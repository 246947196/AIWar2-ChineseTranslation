using System;

using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AIDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<AIDifficulty>, IProtectedListable
    {
        public float Budget_BaseIncome;
        public float Budget_IncomeMultiplierForAIPOver10;
        public float Budget_IncomeMultiplierForAIPOver50;
        public float Budget_IncomeMultiplierForAIPOver100;
        public float Budget_IncomeMultiplierForAIPOver200;
        public float Budget_IncomeMultiplierForAIPOver400;
        public float Budget_IncomeMultiplierForAIPOver600;
        public FInt Budget_IncomeMultiplierForBorderAggression;
        public int Budget_BudgetScaleVisualOnly;

        public FInt SharkB2_BaseStrength;
        public FInt SharkB2_BonusStrengthPerAIP;

        public int MinimumSecondsOnPlanetBeforeRetreat;
        public int SecondsThreatWaitsBeforeJoiningHunterFleet;
        public int SecondsThreatExistsAsThreatBeforeJoiningHunterFleet;
        public int GolemsToSeed;
        public int PercentBigGunNastyPick;
        public int PercentEyeNastyPick;
        public int PercentSupportStructureNastyPick;
        public int PercentWildCardNastyPick;
        public int AIReservesIncomeIncreaseInterval;
        public int AIReservesInitialWormholeSpawnDelay;
        public short AIReservesWormholeDespawnTime;
        public int AIReservesWormholeSpawnInterval;
        public int AIPUnlockReconquestWave;
        public int AIPUnlockCounterattacks;
        public byte Difficulty;
        public FInt BaseCounterattackMultiplier;
        public int AIPUnlockWormholeInvasion;
        public int CounterattackMinStrength;
        public FInt PercentOfWaveBudgetThatCanBeGuardian;
        public int BaseHackingWaveSize;
        public int AttackPercentFindWeakerTarget;
        public int AttackPercentCommandStation;
        public int AttackPercentMetalGenerator;
        public byte AddedPraetorianMarkLevelsAboveAmbient;
        public readonly List<int> AIPForMarkLevel = List<int>.Create_WillNeverBeGCed( 12, "AIDifficulty-AIPForMarkLevel" );
        public readonly List<int> HackingDifficultyLevel = List<int>.Create_WillNeverBeGCed( 12, "AIDifficulty-HackingDifficultyLevel" );
        public readonly List<FInt> HackingDifficultyMultiplier = List<FInt>.Create_WillNeverBeGCed( 12, "AIDifficulty-HackingDifficultyMultiplier" );
        public FInt WaveBudgetMultiplier = FInt.One;
        public FInt HackingAipMultiplier = FInt.One;
        public FInt OverconfidenceRatio = FInt.One; //multiply your forces by this for all AI calculations. Intended to make the AI willing to attack when they shouldn't on lower difficulties
        public int MinWormholeSentimelBudget;
        public int MaxWormholeSentimelBudget;
        public int GuardFreeDistanceFromKing;
        public bool AllowedToGoForPlayerHomeworld;
        public int NumberOfTenMinuteIncrementsBeforeHomeworldIsFullyDefensible = 0;
        public FInt DefensiveCapMultiplier_Initial = FInt.One;
        public FInt DefensiveCapMultiplier_Ongoing = FInt.One;
        public FInt DefensiveCapIncreasePerAIP = FInt.Zero;
        public FInt DefensiveCapIncreasePerTenMinutes = FInt.Zero;
        public FInt MultiplierToGlobalHackingEventIntervals = FInt.One; //the lowest of these is what is actually used in the game.
        public int PercentFortifiedDataCenters = 0;
        public int AIPFloorMultiplierPercent;
        public int AIPAbsoluteFloor;
        public int AIPNeverReducesBelow;
        public int AIPUnlockWormholeBorer = -1;
        public FInt WormholeBorerIncomePerMinute;
        public FInt WormholeBorerCompletionTime;

        public int BaseBunkerSpawnInterval = 1800;
        public FInt BaseBunkerStrengthMultiplier = FInt.One;

        //Tunables for Extragalactic War response
        public FInt EnemyOverallPowerMultiplier; //not currently used
        public int MaxExtragalacticWarTier;
        public FInt ExtragalacticIncomePer10AIP;
        public readonly List<FInt> ExtragalacticWarIncomeByTier = List<FInt>.Create_WillNeverBeGCed( 12, "AIDifficulty-ExtragalacticWarIncomeByTier" );

        public string Description = string.Empty;

        public override void AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add(this.Description).NewLine();
            Buffer.Add("\nBudget Scale: ").AddNumberMoreReadable(this.Budget_BudgetScaleVisualOnly);
            Buffer.Add("\nAI Reserves Interval (Lower Is Harder): ").AddNumberMoreReadable(this.AIReservesIncomeIncreaseInterval);
            Buffer.Add("\nDefense General Scale: ").AddNumberMoreReadable(this.DefensiveCapMultiplier_Ongoing);
            Buffer.Add("\nMultiplier To Your Hacking Event Intervals: ").AddNumberMoreReadable(this.MultiplierToGlobalHackingEventIntervals);
            Buffer.Add("\nAIP Floor Multiplier: ").AddNumberMoreReadable(this.AIPFloorMultiplierPercent).Add("%");
            Buffer.Add("\nAIP Never Reduces Below: ").AddNumberMoreReadable(this.AIPNeverReducesBelow);
            Buffer.Add("\nExtragalactic Income Per 10 AIP: ").AddNumberMoreReadable(this.ExtragalacticIncomePer10AIP);
            Buffer.Add("\nAdded Praetorian Mark Levels Above Ambient: ").AddNumberMoreReadable(this.AddedPraetorianMarkLevelsAboveAmbient);
            Buffer.Add("\nScale Of Strength Sent After Players Lose Command Stations: ").AddNumberMoreReadable(this.SharkB2_BaseStrength.GetNearestIntPreferringHigher()).Add(" / ").AddNumberMoreReadable(this.SharkB2_BonusStrengthPerAIP.GetNearestIntPreferringHigher());
            Buffer.Add("\nOverconfidence Ratio: ").Add(this.OverconfidenceRatio);
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AIDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIDifficulties" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AIDifficulty> Pool = new ConcurrentPool<AIDifficulty>( "AIDifficulties", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AIDifficulty(); } );

        public static AIDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AIDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AIDifficulty>( new AIDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class AIDifficultyTable : ArcenDynamicTable<AIDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AIDifficultyTable Instance;
        public AIDifficultyTable() : base( "AIDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AIDifficulty GetNewRowFromPool()
        {
            return AIDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public AIDifficulty GetRowByOrdinal( byte difficulty )
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Difficulty == difficulty )
                    return this.Rows[i];
            }
            throw new Exception( "Could not find AI difficulty " + difficulty );
        }
        public override DelReturn NodeProcessor( ArcenXMLElement Data, AIDifficulty TypeDataObject )
        {
            Data.Fill( "budget_base_income", ref TypeDataObject.Budget_BaseIncome, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_10", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver10, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_50", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver50, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_100", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver100, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_200", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver200, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_400", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver400, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_AIP_Over_600", ref TypeDataObject.Budget_IncomeMultiplierForAIPOver600, !Data.ReadingPartialRecord );
            Data.Fill( "budget_income_multiplier_for_border_aggression", ref TypeDataObject.Budget_IncomeMultiplierForBorderAggression, false );
            Data.Fill( "budget_budget_scale_visual_only", ref TypeDataObject.Budget_BudgetScaleVisualOnly, !Data.ReadingPartialRecord );

            Data.Fill( "shark_b2_base_strength_per_command_station", ref TypeDataObject.SharkB2_BaseStrength, false );
            Data.Fill( "shark_b2_bonus_strength_per_aip", ref TypeDataObject.SharkB2_BonusStrengthPerAIP, false );

            Data.Fill( "minimum_seconds_on_planet_before_retreat", ref TypeDataObject.MinimumSecondsOnPlanetBeforeRetreat, !Data.ReadingPartialRecord );
            Data.Fill( "seconds_threat_waits_before_joining_hunter_fleet", ref TypeDataObject.SecondsThreatWaitsBeforeJoiningHunterFleet, !Data.ReadingPartialRecord );
            Data.Fill( "seconds_threat_exists_as_threat_before_joining_hunter_fleet", ref TypeDataObject.SecondsThreatExistsAsThreatBeforeJoiningHunterFleet, !Data.ReadingPartialRecord );
            Data.Fill( "percent_big_gun_nasty_pick", ref TypeDataObject.PercentBigGunNastyPick, !Data.ReadingPartialRecord );
            Data.Fill( "percent_eye_nasty_pick", ref TypeDataObject.PercentEyeNastyPick, !Data.ReadingPartialRecord );
            Data.Fill( "percent_support_structure_nasty_pick", ref TypeDataObject.PercentSupportStructureNastyPick, !Data.ReadingPartialRecord );
            Data.Fill( "percent_wild_card_nasty_pick", ref TypeDataObject.PercentWildCardNastyPick, !Data.ReadingPartialRecord );
            Data.Fill( "ai_reserves_wormhole_spawn_interval", ref TypeDataObject.AIReservesWormholeSpawnInterval, !Data.ReadingPartialRecord );
            Data.Fill( "ai_reserves_wormhole_despawn_time", ref TypeDataObject.AIReservesWormholeDespawnTime, !Data.ReadingPartialRecord );
            Data.Fill( "ai_reserves_initial_wormhole_spawn_delay", ref TypeDataObject.AIReservesInitialWormholeSpawnDelay, !Data.ReadingPartialRecord );
            Data.Fill( "ai_reserves_income_increase_interval", ref TypeDataObject.AIReservesIncomeIncreaseInterval, !Data.ReadingPartialRecord );
            Data.Fill( "number_golems_to_seed", ref TypeDataObject.GolemsToSeed, !Data.ReadingPartialRecord );
            Data.Fill( "aip_unlock_counterattacks", ref TypeDataObject.AIPUnlockCounterattacks, !Data.ReadingPartialRecord );
            Data.Fill( "aip_unlock_reconquestwave", ref TypeDataObject.AIPUnlockReconquestWave, !Data.ReadingPartialRecord );
            Data.Fill( "difficulty", ref TypeDataObject.Difficulty, !Data.ReadingPartialRecord );
            Data.Fill( "counterattack_base_multiplier", ref TypeDataObject.BaseCounterattackMultiplier, !Data.ReadingPartialRecord );
            Data.Fill( "counterattack_min_strength", ref TypeDataObject.CounterattackMinStrength, !Data.ReadingPartialRecord );
            Data.Fill( "aip_unlock_wormhole_invasion", ref TypeDataObject.AIPUnlockWormholeInvasion, !Data.ReadingPartialRecord );
            Data.Fill( "percent_wave_budget_that_can_be_guardians", ref TypeDataObject.PercentOfWaveBudgetThatCanBeGuardian, false ); //this can be 0 for lower difficulties
            Data.Fill( "overconfidence_ratio", ref TypeDataObject.OverconfidenceRatio, false ); //this can be 0 for lower difficulties
            Data.Fill( "hacking_aip_multiplier", ref TypeDataObject.HackingAipMultiplier, false ); //this can be 0 for lower difficulties
            Data.Fill( "base_hacking_wave_size", ref TypeDataObject.BaseHackingWaveSize, false ); //this can be 0 for lower difficulties
            Data.Fill( "attack_percent_find_weaker_target", ref TypeDataObject.AttackPercentFindWeakerTarget, false );
            Data.Fill( "attack_percent_command_station", ref TypeDataObject.AttackPercentCommandStation, false );
            Data.Fill( "attack_percent_metal_generator", ref TypeDataObject.AttackPercentMetalGenerator, false );
            Data.Fill( "min_wormhole_sentinel_budget", ref TypeDataObject.MinWormholeSentimelBudget, false ); //this is false because the Min budget can be 0
            Data.Fill( "max_wormhole_sentinel_budget", ref TypeDataObject.MaxWormholeSentimelBudget, !Data.ReadingPartialRecord );
            Data.Fill( "guard_free_distance_from_king", ref TypeDataObject.GuardFreeDistanceFromKing, false ); //false, this can be 0 for lower difficulties
            Data.Fill( "allowed_to_go_for_player_homeworld", ref TypeDataObject.AllowedToGoForPlayerHomeworld, false ); //how much strength relative to the defenses should the wave have
            Data.Fill( "number_of_ten_minute_increments_before_homeworld_is_fully_defensible", ref TypeDataObject.NumberOfTenMinuteIncrementsBeforeHomeworldIsFullyDefensible, !Data.ReadingPartialRecord ); //the full cap of a homeworld can't be used by this AI until it reaches how many hours' worth of 10 minute increments?
            Data.Fill( "defensive_cap_multiplier_initial", ref TypeDataObject.DefensiveCapMultiplier_Initial, false ); //different AIP levels get different amounts of defenses
            Data.Fill( "defensive_cap_multiplier_ongoing", ref TypeDataObject.DefensiveCapMultiplier_Ongoing, false ); //different AIP levels get different amounts of defenses
            Data.Fill( "defensive_cap_increase_per_aip", ref TypeDataObject.DefensiveCapIncreasePerAIP, false ); //how much the defensive cap should increase with aip
            Data.Fill( "defensive_cap_increase_per_ten_minutes", ref TypeDataObject.DefensiveCapIncreasePerTenMinutes, false ); //how much the defensive cap should increase with time spent.  Small but should happen.            
            Data.Fill( "percent_fortified_data_centers", ref TypeDataObject.PercentFortifiedDataCenters, false ); //what percentage of Data Centers on the map shoudl be Fortified
            Data.Fill( "multiplier_to_global_hacking_event_intervals", ref TypeDataObject.MultiplierToGlobalHackingEventIntervals, false ); //what percentage of Data Centers on the map shoudl be Fortified
            Data.Fill( "WormholeBorerIncomePerMinute", ref TypeDataObject.WormholeBorerIncomePerMinute, false ); //how quickly wormhole borers are built
            Data.Fill( "WormholeBorerCompletionTime", ref TypeDataObject.WormholeBorerCompletionTime, false ); //how quickly wormhole borers create the wormhole
            Data.Fill( "AIPUnlockWormholeBorer", ref TypeDataObject.AIPUnlockWormholeBorer, false ); //how quickly wormhole borers are built
            Data.Fill( "BaseBunkerSpawnInterval", ref TypeDataObject.BaseBunkerSpawnInterval, false ); //how quickly wormhole borers are built
            Data.Fill( "BaseBunkerStrengthMultiplier", ref TypeDataObject.BaseBunkerStrengthMultiplier, false ); //how quickly wormhole borers are built
            Data.Fill( "wave_budget_multiplier", ref TypeDataObject.WaveBudgetMultiplier, false ); //how much more frequently waves should grow
            Data.Fill( "added_praetorian_mark_levels_above_ambient", ref TypeDataObject.AddedPraetorianMarkLevelsAboveAmbient, !Data.ReadingPartialRecord );

            Data.Fill( "description", ref TypeDataObject.Description, false );
            Data.Fill( "aip_floor_multiplier_percent", ref TypeDataObject.AIPFloorMultiplierPercent, false );
            Data.Fill( "aip_absolute_floor", ref TypeDataObject.AIPAbsoluteFloor, false );
            Data.Fill( "aip_never_reduces_below", ref TypeDataObject.AIPNeverReducesBelow, false );

            Data.Fill( "enemy_overall_power_multiplier", ref TypeDataObject.EnemyOverallPowerMultiplier, false );
            Data.Fill( "max_extragalactic_war_tier", ref TypeDataObject.MaxExtragalacticWarTier, false );
            Data.Fill( "extragalactic_income_per_10AIP", ref TypeDataObject.ExtragalacticIncomePer10AIP, false );

            //need to make sure that the list has enough entries
            while ( TypeDataObject.ExtragalacticWarIncomeByTier.Count < 5 )
                TypeDataObject.ExtragalacticWarIncomeByTier.Add( FInt.Zero );

            //fill the entries, with array indices 0-indexed but the xml 1-indexed
            for ( int i = 1; i <= 5; i++ )
                Data.FillArrayIndex( "income_for_extragalactic_war_tier" + i, TypeDataObject.ExtragalacticWarIncomeByTier, i - 1, false );

            //need to make sure that the list has enough entries
            while ( TypeDataObject.HackingDifficultyMultiplier.Count < 8 )
                TypeDataObject.HackingDifficultyMultiplier.Add( FInt.Zero );

            //fill the entries, with array indices 0-indexed but the xml 1-indexed
            for ( int i = 1; i <= 8; i++ )
                Data.FillArrayIndex( "hacking_wave_multiplier_" + i, TypeDataObject.HackingDifficultyMultiplier, i - 1, false );

            //need to make sure that the list has enough entries
            while ( TypeDataObject.HackingDifficultyLevel.Count < 8 )
                TypeDataObject.HackingDifficultyLevel.Add( 0 );

            //fill the entries, with array indices 0-indexed but the xml 1-indexed
            for ( int i = 1; i <= 8; i++ )
                Data.FillArrayIndex( "hacking_level_" + i, TypeDataObject.HackingDifficultyLevel, i - 1, false );

            //need to make sure that the list has enough entries
            while ( TypeDataObject.AIPForMarkLevel.Count < 8 )
                TypeDataObject.AIPForMarkLevel.Add( 0 );

            //fill the entries, with array indices 0-indexed and the xml 0-indexed
            for ( int i = 0; i <= 7; i++ )
                Data.FillArrayIndex( "aip_for_mark_" + i, TypeDataObject.AIPForMarkLevel, i, false );

            if ( TypeDataObject.AIPForMarkLevel.Count < 7 )
                ArcenDebugging.ArcenDebugLog( "Too few AIPForMarkLevel on AI difficulty " + TypeDataObject.InternalName, Verbosity.ShowAsError );

            bool foundErrors = false;
            for ( int i = 0; i < 7; i++ )
            {
                if ( TypeDataObject.HackingDifficultyMultiplier[i] == FInt.Zero )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hacking difficulty multiplier for difficulty " + TypeDataObject.Difficulty + " level " + i + " is " + TypeDataObject.HackingDifficultyMultiplier[i], Verbosity.DoNotShow );
                    foundErrors = true;
                }
            }
            if ( foundErrors )
                throw new Exception( "Could not parse the hacking difficulty\n" );

            return DelReturn.Continue;
        }

    }
}
