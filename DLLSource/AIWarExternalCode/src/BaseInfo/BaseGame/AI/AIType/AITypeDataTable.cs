using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //Type Difficulty is defined in Universal's Achievement code
    public static class TypeDifficulty_Extensions
    {
        public static string GetHexColor( this TypeDifficulty Type )
        {
            switch ( Type )
            {
                case TypeDifficulty.Any:
                    return "#df09ff"; //darker purple
                case TypeDifficulty.Unset:
                    break;
                case TypeDifficulty.Easier:
                    return "#66ffce"; //light cyan-ish green
                case TypeDifficulty.Moderate:
                    return "#ffe066"; //golden yellow
                case TypeDifficulty.Hard:
                    return "#ff3232"; //angry red
                case TypeDifficulty.Brutal:
                    return "#ff3fd9"; //purple
                case TypeDifficulty.SuperCat:
                    return "#ff0066"; //bright red / fuschia-ish
            }
            return "#ffffff";
        }
    }

    public class AITypeData : ArcenDynamicTableRow, IConcurrentPoolable<AITypeData>, IProtectedListable, IOption
    {
        public string Abbreviation = string.Empty;
        public readonly EnumIndexedArray<AIBudgetType,AIBudgetItem> BudgetItems = EnumIndexedArray<AIBudgetType,AIBudgetItem>.Create_WillNeverBeGCed( false, null, "AITypeData-BudgetItems" );
        public FInt RatioOfSuperiorityAtWhichThreatOverruns;
        public FInt RatioOfSuperiorityAtWhichThreatActuallyAttacks;
        public FInt RatioOfInferiorityAtWhichThreatRetreats;
        public FInt RatioOfInferiorityAtWhichDefenseHunkersDown;
        public FInt RatioOfInferiorityAtWhichGuardShipsGoThreat;
        public FInt RatioOfInferiorityAtWhichAllGuardPostsDeploy = FInt.FromParts(2,500);
        public FInt RatioOfFearOfRemoteEnemies;
        public FInt RatioForPrefeferringOverridingHighPriorityTargets;
        public FInt RatioForPrefeferringHighPriorityTargets;
        public FInt InitialAIDefensesGuardiansBudgetPerMark3Planet;
        public FInt InitialAIDefensesStrikecraftBudgetPerMark3Planet;
        public FInt InitialAIDefensesTurretBudgetPerMark3Planet;
        public FInt InitialAIDefensesNonTurretDefenseBudgetPerMark3Planet;
        public FInt MultiplierForStartingGuardianBudgetOnReconquest;
        public FInt MultiplierForGameStartingGuardiansFromTotalBudget;
        public FInt MultiplierForGameStartingTurretsFromTotalBudget;
        public FInt MultiplierForGameStartingNonTurretDefensesFromTotalBudget;
        public FInt MultiplierForGameStartingStrikecraftFromTotalBudget;
        public int PercentageOutOf100OfPlanetsToHaveHalfCapTurretsAndNonTurretDefenses;
        public int PercentageOutOf100OfPlanetsToHaveFullCapTurretsAndNonTurretDefenses;
        public int ModulusForTurretsAtReinforcementLocationLowerIsMoreFrequent;
        public int ModulusForNonTurretDefensesAtReinforcementLocationLowerIsMoreFrequent;
        public int DeepstrikeDistanceModifier = 0;
        public bool OverlordInDeepstrike = false;
        public byte NumberOfDirePostsOnHomeworld;
        public byte NumberOfDirePostsOnBastionWorlds;
        public byte NumberOfBastionWorlds;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk1;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk2;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk3;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk4;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk5;
        public byte ChanceOutOf100ForArmedGuardPostPlanetMk6; //mk7 and home planets  are always fully armed guard posts
        public bool IgnoreGuardianRestrictionsInWaves;
        public FInt CounterAttackBudgetMultiplier;
        public FInt MultiplierForBudget_Reinforcement = FInt.One;
        public FInt MultiplierForBudget_Wave = FInt.One;
        public FInt MultiplierForBudget_CPA = FInt.One;
        public FInt MultiplierForBudget_WardenFleet = FInt.One;
        public FInt MultiplierForBudget_Reconquest = FInt.One;
        public FInt MultiplierForBudget_HunterFleet = FInt.One;
        public FInt MultiplierForBudget_PraetorianGuard = FInt.One;
        public FInt MultiplierForBudget_WormholeInvasion = FInt.One;
        public FInt MultiplierForBudget_BorderAggression = FInt.One;

        public string BigGunNastyPickTag = string.Empty;
        public string EyeNastyPickTag = string.Empty;
        public string SupportStructureNastyPickTag = string.Empty;
        public string WildCardNastyPickTag = string.Empty;
        public string CommandStationPickTag = string.Empty;
        public string SpawnOneOfTagInWaves = string.Empty;

        public string DllName = string.Empty;
        public string TypeName = string.Empty;
        [NotForDumping]
        public IAITypeImplementation Implementation;
        [NotForDumping]
        public ArcenCachedExternalType CachedTypeForImplementation;

        public string description = string.Empty;
        public TypeDifficulty Difficulty = TypeDifficulty.Unset;
        public TypeDifficulty RandomDifficulty = TypeDifficulty.Unset;
        public TypeDifficulty AdaptiveDifficulty = TypeDifficulty.Unset;
        public int PraetorianRange = 3;
        public FInt PraetorianPopulationCapMultiplier = FInt.One;
        public bool GenerateExoOnImportantStructureDeath;

        public override void AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( this.description );
        }

        private string displayNameAndColor = null;
        public override string GetDisplayName()
        {
            if ( this.displayNameAndColor == null )
            {
                if ( this.AdaptiveDifficulty != TypeDifficulty.Unset )
                    this.displayNameAndColor = "Adaptive <color=" + this.AdaptiveDifficulty.GetHexColor() + ">" + this.AdaptiveDifficulty + "</color>";
                else if ( this.RandomDifficulty != TypeDifficulty.Unset )
                    this.displayNameAndColor = "Random <color=" + this.RandomDifficulty.GetHexColor() + ">" + this.RandomDifficulty + "</color>";
                else if ( this.Difficulty == TypeDifficulty.Unset )
                    this.displayNameAndColor = this.DisplayName;
                else
                    this.displayNameAndColor = "<color=" + this.Difficulty.GetHexColor() + ">" + this.Difficulty + "</color>  " + this.DisplayName;
            }
            return this.displayNameAndColor;
        }

        public override string GetShortDisplayName()
        {
            return this.Abbreviation;
        }

        string IOption.GetShortDisplayName()
        {
            // our regular names fit and look better
            // our short name is for really short other places in the game
            return this.GetDisplayName();
        }

        string IOption.GetDisplayName()
        {
            // our regular names fit and look better
            // our short name is for really short other places in the game
            return this.GetDisplayName();
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AITypeData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AITypeDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AITypeData> Pool = new ConcurrentPool<AITypeData>( "AITypeDatas", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AITypeData(); } );

        public static AITypeData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AITypeData> typeAnalyzer;

        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AITypeData>( new AITypeData() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class AITypeDataTable : ArcenDynamicTable<AITypeData>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AITypeDataTable Instance;

        public override AITypeData GetNewRowFromPool()
        {
            return AITypeData.GetFromPoolOrCreate();
        }

        public readonly DictionaryOfLists<TypeDifficulty, AITypeData> TypeByDifficulty = DictionaryOfLists<TypeDifficulty, AITypeData>.Create_WillNeverBeGCed( 100, 80, "AITypeData-TypeByDifficulty" );
        public readonly List<AITypeData> SortedTypes = List<AITypeData>.Create_WillNeverBeGCed( 100, "AITypeData-SortedTypes" );
        public readonly List<ArcenDynamicTableRow> SortedTypesGeneric = List<ArcenDynamicTableRow>.Create_WillNeverBeGCed( 100, "AITypeData-SortedTypesGeneric" );

        public AITypeDataTable() : base( "AIType", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Index, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override List<ArcenDynamicTableRow> GetSortedTypes()
        {
            return this.SortedTypesGeneric;
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AITypeData TypeDataObject )
        {
            Data.Fill( "abbreviation", ref TypeDataObject.Abbreviation, !Data.ReadingPartialRecord );
            Data.Fill( "dll_name", ref TypeDataObject.DllName, !Data.ReadingPartialRecord );
            Data.Fill( "type_name", ref TypeDataObject.TypeName, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_superiority_at_which_threat_overruns", ref TypeDataObject.RatioOfSuperiorityAtWhichThreatOverruns, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_superiority_at_which_threat_actually_attacks", ref TypeDataObject.RatioOfSuperiorityAtWhichThreatActuallyAttacks, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_inferiority_at_which_threat_retreats", ref TypeDataObject.RatioOfInferiorityAtWhichThreatRetreats, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_inferiority_at_which_defense_hunkers_down", ref TypeDataObject.RatioOfInferiorityAtWhichDefenseHunkersDown, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_inferiority_at_which_guard_ships_go_threat", ref TypeDataObject.RatioOfInferiorityAtWhichGuardShipsGoThreat, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_of_inferiority_at_which_all_guard_posts_deploy", ref TypeDataObject.RatioOfInferiorityAtWhichAllGuardPostsDeploy, false );
            Data.Fill( "ratio_of_fear_of_remote_enemies", ref TypeDataObject.RatioOfFearOfRemoteEnemies, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_for_preferring_overriding_high_priority_targets", ref TypeDataObject.RatioForPrefeferringOverridingHighPriorityTargets, !Data.ReadingPartialRecord );
            Data.Fill( "ratio_for_preferring_high_priority_targets", ref TypeDataObject.RatioForPrefeferringHighPriorityTargets, !Data.ReadingPartialRecord );
            Data.Fill( "initial_ai_defenses_guardians_budget_per_mark_3_planet", ref TypeDataObject.InitialAIDefensesGuardiansBudgetPerMark3Planet, !Data.ReadingPartialRecord );
            Data.Fill( "initial_ai_defenses_strikecraft_budget_per_mark_3_planet", ref TypeDataObject.InitialAIDefensesStrikecraftBudgetPerMark3Planet, !Data.ReadingPartialRecord );
            Data.Fill( "initial_ai_defenses_turret_budget_per_mark_3_planet", ref TypeDataObject.InitialAIDefensesTurretBudgetPerMark3Planet, !Data.ReadingPartialRecord );
            Data.Fill( "initial_ai_defenses_non_turret_defense_budget_per_mark_3_planet", ref TypeDataObject.InitialAIDefensesNonTurretDefenseBudgetPerMark3Planet, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_starting_guardian_budget_on_reconquest", ref TypeDataObject.MultiplierForStartingGuardianBudgetOnReconquest, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_game_starting_guardians_from_total_budget", ref TypeDataObject.MultiplierForGameStartingGuardiansFromTotalBudget, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_game_starting_turrets_from_total_budget", ref TypeDataObject.MultiplierForGameStartingTurretsFromTotalBudget, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_game_starting_non_turret_defenses_from_total_budget", ref TypeDataObject.MultiplierForGameStartingNonTurretDefensesFromTotalBudget, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_game_starting_strikecraft_from_total_budget", ref TypeDataObject.MultiplierForGameStartingStrikecraftFromTotalBudget, !Data.ReadingPartialRecord );
            Data.Fill( "percentage_out_of_100_of_planets_to_have_half_cap_turrets_and_non_turret_defenses", ref TypeDataObject.PercentageOutOf100OfPlanetsToHaveHalfCapTurretsAndNonTurretDefenses, !Data.ReadingPartialRecord );
            Data.Fill( "percentage_out_of_100_of_planets_to_have_full_cap_turrets_and_non_turret_defenses", ref TypeDataObject.PercentageOutOf100OfPlanetsToHaveFullCapTurretsAndNonTurretDefenses, !Data.ReadingPartialRecord );
            Data.Fill( "modulus_for_turrets_at_reinforcement_location_lower_is_more_frequent", ref TypeDataObject.ModulusForTurretsAtReinforcementLocationLowerIsMoreFrequent, !Data.ReadingPartialRecord );
            Data.Fill( "modulus_for_non_turret_defenses_at_reinforcement_location_lower_is_more_frequent", ref TypeDataObject.ModulusForNonTurretDefensesAtReinforcementLocationLowerIsMoreFrequent, !Data.ReadingPartialRecord );
            Data.Fill( "number_of_dire_posts_on_homeworld", ref TypeDataObject.NumberOfDirePostsOnHomeworld, !Data.ReadingPartialRecord );
            Data.Fill( "number_of_dire_posts_on_bastion_worlds", ref TypeDataObject.NumberOfDirePostsOnBastionWorlds, !Data.ReadingPartialRecord );
            Data.Fill( "number_of_bastion_worlds", ref TypeDataObject.NumberOfBastionWorlds, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk1", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk1, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk2", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk2, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk3", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk3, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk4", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk4, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk5", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk5, !Data.ReadingPartialRecord );
            Data.Fill( "chance_out_of_100_for_armed_guard_post_planet_mk6", ref TypeDataObject.ChanceOutOf100ForArmedGuardPostPlanetMk6, !Data.ReadingPartialRecord );
            Data.Fill( "ignore_guardian_restrictions_in_waves", ref TypeDataObject.IgnoreGuardianRestrictionsInWaves, false );
            Data.Fill( "description", ref TypeDataObject.description, !Data.ReadingPartialRecord );
            Data.Fill( "big_gun_nasty_pick_tag", ref TypeDataObject.BigGunNastyPickTag, !Data.ReadingPartialRecord );
            Data.Fill( "eye_nasty_pick_tag", ref TypeDataObject.EyeNastyPickTag, !Data.ReadingPartialRecord );
            Data.Fill( "support_structure_nasty_pick_tag", ref TypeDataObject.SupportStructureNastyPickTag, !Data.ReadingPartialRecord );
            Data.Fill( "wildcard_nasty_pick_tag", ref TypeDataObject.WildCardNastyPickTag, !Data.ReadingPartialRecord );            
            Data.Fill( "command_station_pick_tag", ref TypeDataObject.CommandStationPickTag, false );
            Data.Fill( "spawn_one_of_ship_tag_in_waves", ref TypeDataObject.SpawnOneOfTagInWaves, false );
            Data.Fill( "deepstrike_distance_modifier", ref TypeDataObject.DeepstrikeDistanceModifier, false );
            Data.Fill( "overlord_always_in_deepstrike", ref TypeDataObject.OverlordInDeepstrike, false );
            Data.Fill( "counterattack_multiplier", ref TypeDataObject.CounterAttackBudgetMultiplier, false );
            Data.Fill( "multiplier_for_budget_reinforcement", ref TypeDataObject.MultiplierForBudget_Reinforcement, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_wave", ref TypeDataObject.MultiplierForBudget_Wave, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_cpa", ref TypeDataObject.MultiplierForBudget_CPA, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_warden_fleet", ref TypeDataObject.MultiplierForBudget_WardenFleet, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_reconquest", ref TypeDataObject.MultiplierForBudget_Reconquest, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_hunter_fleet", ref TypeDataObject.MultiplierForBudget_HunterFleet, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_praetorian_guard", ref TypeDataObject.MultiplierForBudget_PraetorianGuard, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_wormhole_invasion", ref TypeDataObject.MultiplierForBudget_WormholeInvasion, !Data.ReadingPartialRecord );
            Data.Fill( "multiplier_for_budget_border_aggression", ref TypeDataObject.MultiplierForBudget_BorderAggression, !Data.ReadingPartialRecord );
            Data.Fill( "generate_exo_on_important_structure_death", ref TypeDataObject.GenerateExoOnImportantStructureDeath, false ); //only used for Vengeful AI Type
            Data.FillEnum( "type_difficulty", ref TypeDataObject.Difficulty, false );
            Data.FillEnum( "random_difficulty", ref TypeDataObject.RandomDifficulty, false );
            Data.FillEnum( "adaptive_difficulty", ref TypeDataObject.AdaptiveDifficulty, false );
            Data.Fill( "praetorian_range", ref TypeDataObject.PraetorianRange, false );
            Data.Fill( "praetorian_population_cap_multiplier", ref TypeDataObject.PraetorianPopulationCapMultiplier, false );

            //make sure that if something sets a random difficulty, we are considering it to be that difficulty
            if ( TypeDataObject.RandomDifficulty > TypeDataObject.Difficulty )
                TypeDataObject.Difficulty = TypeDataObject.RandomDifficulty;
            //make sure that if something sets an adaptive difficulty, we are considering it to be that difficulty
            if ( TypeDataObject.AdaptiveDifficulty > TypeDataObject.Difficulty )
                TypeDataObject.Difficulty = TypeDataObject.AdaptiveDifficulty;

            if ( !Data.ReadingPartialRecord )
            {
                for ( AIBudgetType i = AIBudgetType.None; i < AIBudgetType.Length; i++ )
                {
                    if ( TypeDataObject.BudgetItems[i] == null )
                        TypeDataObject.BudgetItems[i] = AIBudgetItem.CreateNotFromPool();
                    else
                        TypeDataObject.BudgetItems[i].Clear();
                }
            }
            foreach ( ArcenXMLElement child in Data.ChildrenOfType_AndParentsAndPartials( "budget_item" ) )
            {
                AIBudgetType type = AIBudgetType.None;
                child.FillEnum( "type", ref type, !Data.Direct_IsPartialRecord );
                try
                {
                    AIBudgetItem item = TypeDataObject.BudgetItems[type];
                    bool allowEmpties = Data.ReadingPartialRecord || TypeDataObject.CopiedFrom != TypeDataObject;
                    child.Fill( "seconds_between_attempts_to_spend", ref item.SecondsBetweenAttemptsToSpend, !allowEmpties );
                    child.Fill( "spend_on_threshold", ref item.SpendOnThreshold, !allowEmpties );
                    child.Fill( "double_wave_interval_each_time", ref item.DoubleWaveIntervalEachTime, false ); //only for Waves
                    child.Fill( "normal_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.NormalAIShipGroup, !allowEmpties );
                    child.Fill( "guard_post_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.GuardPostAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "dire_guard_post_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.DireGuardPostAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "unarmed_guard_post_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.UnarmedGuardPostAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "turret_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.TurretAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "non_turret_defense_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.NonTurretDefenseAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "wormhole_sentinel_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.WormholeSentinelAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "guardian_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.GuardianAIShipGroup, !allowEmpties );
                    child.Fill( "forcefield_guardian_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.ForcefieldGuardianAIShipGroup, !allowEmpties );
                    child.Fill( "decloaker_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.DecloakerAIShipGroup, !allowEmpties );
                    child.Fill( "dire_guardian_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.DireGuardianAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "singular_freaky_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.RegularSingularFreakySurprisesAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                    child.Fill( "dire_singular_freaky_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.DireSingularFreakySurprisesAIShipGroup, false ); //always allow empties
                    child.Fill( "exo_leader_ship_group_cat", AIShipGroupCategoryTable.Instance, ref item.ExoLeaderAIShipGroup, !allowEmpties && type == AIBudgetType.Reinforcement );
                }
                catch ( Exception e )
                {
                    throw new ArcenDataReadException( "budget_item:" + type + ": " + child.LimitedOuterXml + Environment.NewLine + e );
                }
            }

            for ( AIBudgetType i = AIBudgetType.None + 1; i < AIBudgetType.Length; i++ )
            {
                AIBudgetItem item = TypeDataObject.BudgetItems[i];
                CheckForEmptyAIShipGroup( TypeDataObject, item, item.NormalAIShipGroup, "normal_ship_group_cat" );
                if ( item.Type == AIBudgetType.Reinforcement )
                {
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.GuardPostAIShipGroup, "guard_post_ship_group_cat" );
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.DireGuardPostAIShipGroup, "dire_guard_post_ship_group_cat" );
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.UnarmedGuardPostAIShipGroup, "unarmed_guard_post_ship_group_cat" );
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.TurretAIShipGroup, "turret_ship_group_cat" );
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.NonTurretDefenseAIShipGroup, "non_turret_defense_ship_group_cat" );
                    CheckForEmptyAIShipGroup( TypeDataObject, item, item.RegularSingularFreakySurprisesAIShipGroup, "singular_freaky_ship_group_cat" );
                    //we don't care if DireSingularFreakySurprisesAIShipGroup is null, it's valid to be null
                    //CheckForEmptyAIShipGroup( TypeDataObject, item, item.WormholeSentinelAIShipGroup, "sentinel_ship_group_cat" );
                }
                CheckForEmptyAIShipGroup( TypeDataObject, item, item.GuardianAIShipGroup, "guardian_ship_group_cat" );
                //CheckForEmptyAIShipGroup( TypeDataObject, item, item.ForcefieldGuardianAIShipGroup, "forcefield_guardian_ship_group_cat" );
                CheckForEmptyAIShipGroup( TypeDataObject, item, item.DireGuardianAIShipGroup, "dire_guardian_ship_group_cat" );
                //if ( item.Type == AIBudgetType.Reinforcement )
                //    CheckForEmptyAIShipGroup( TypeDataObject, item, item.ShieldAIShipGroup, "shield_ship_group_cat" );
            }

            return DelReturn.Continue;
        }

        public override void DoPostInitializationAndSortingLogic_MainThread()
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                AITypeData row = this.Rows[i];
                row.CachedTypeForImplementation = ArcenExternalTypeManager.LoadCachedTypeFromExternalAssembly(
                    row.DllName, string.Empty, row.TypeName, row.OriginalXmlData.CachedDoc );
                row.Implementation = row.CachedTypeForImplementation.GetOrCreateFirstAvailableInstanceOfType_AnyThread<IAITypeImplementation>( row.InternalName );
            }
        }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            TypeByDifficulty.Clear();
            SortedTypes.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                AITypeData row = this.Rows[i];
                //add to the sorted types
                SortedTypes.Add( row );

                if ( row.RandomDifficulty != TypeDifficulty.Unset || row.AdaptiveDifficulty != TypeDifficulty.Unset )
                    continue; //make sure not to include any of the random difficulties in the selection by difficulty.

                if ( row.OriginalXmlData.GetBool( "custom_neverRandom", false, false ) )
                    continue;
                
                TypeByDifficulty[row.Difficulty].Add( row );
            }

            SortedTypes.Sort( static delegate ( AITypeData Left, AITypeData Right )
            {
                int val;

                //first sort by difficulty asc
                val = Left.Difficulty.CompareTo( Right.Difficulty );
                if ( val != 0 ) return val;

                //then desc sort by if it's the random option or not
                val = Right.RandomDifficulty.CompareTo( Left.RandomDifficulty );
                if ( val != 0 ) return val;

                val = Right.AdaptiveDifficulty.CompareTo( Left.AdaptiveDifficulty );
                if ( val != 0 ) return val;

                //then sort by isdefault desc
                val = Right.IsDefault.CompareTo( Left.IsDefault );
                if ( val != 0 ) return val;

                //then sort by name
                val = Right.DisplayName.CompareTo( Left.DisplayName );
                if ( val != 0 ) return val;

                //then sortorder, I guess.  Probably not needed
                return Left.SortOrder.CompareTo( Right.SortOrder );
            } );

            this.SortedTypesGeneric.Clear();
            //now store a generic version we can pull out later
            for ( int i = 0; i < this.SortedTypes.Count; i++ )
                this.SortedTypesGeneric.Add( (ArcenDynamicTableRow)this.SortedTypes[i] );
        }
        public AITypeData GetRandomTypeData()
        {
            AITypeData row = null;
            int attemptsLeft = 100;

            while (attemptsLeft > 0)
            {
                row = this.Rows[Engine_Universal.PermanentQualityRandom.Next( 0, this.Rows.Count )];
                attemptsLeft--;

                if ( row.Difficulty == TypeDifficulty.Unset || //skip tutorial
                     row.AdaptiveDifficulty != TypeDifficulty.Unset || //not adaptive
                     row.RandomDifficulty != TypeDifficulty.Unset ) //not random
                {
                    continue;
                }

                if ( row.OriginalXmlData.GetBool( "custom_neverRandom", false, false ) )
                    continue;
            }

            return row;
        }
        public AITypeData GetRandomTypeByDifficulty( TypeDifficulty difficulty )
        {
            if ( difficulty == TypeDifficulty.Any )
            {
                return GetRandomTypeData();
            }

            List<AITypeData> list = TypeByDifficulty[difficulty];
            return list[Engine_Universal.PermanentQualityRandom.Next( 0, list.Count )];
        }

        #region CheckForEmptyAIShipGroup
        private void CheckForEmptyAIShipGroup( AITypeData TypeDataObject, AIBudgetItem item, AIShipGroupCategory AIShipGroup, string AIShipGroupName )
        {
            if ( AIShipGroup == null )
            {
                ArcenDebugging.ArcenDebugLog( "Error in AI type " + TypeDataObject.InternalName + " budget item " + item.Type + ": no AIShipGroup assigned for " + AIShipGroupName, Verbosity.ShowAsError );
                return;
            }
            if ( AIShipGroup.DrawBag.InternalListSize <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "Error in AI type " + TypeDataObject.InternalName + " budget item " + item.Type + ": empty AIShipGroup assigned for " + AIShipGroupName + " (" + AIShipGroup.InternalName + ")", Verbosity.ShowAsError );
                return;
            }
        }
        #endregion
    }

    public interface IAITypeImplementation : IArcenExternalClassNothingSpecialToManageSingleton
    {
        void SetSpendingRatios( Faction Faction, AIBudgetCurrentConfiguration Budget, FInt AtAip );
        int GetRaidDesirability( Faction Faction, Planet planet );
        int GetRaidTraversalDifficulty( Faction Faction, Planet planet );
        AIDefensePlacer GetAIDefensePlacerForPlanet( Faction faction, Planet planet, ArcenHostOnlySimContext Context );
        void AssignDefenseValuesTo_HostOnly( List<Planet> planets, ArcenHostOnlySimContext Context );
        void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context );
        void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly(GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context);
        void CalculateCoreImportance( ref float importanceWithAdjustment, GameEntity_Squad attackerEntity, EntitySystem attackerSystem, EntitySystemTypeData attackerSystemTypeData, GameEntity_Squad defenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer );
        void AdjustWaveOptions( Faction attachedFaction, ArcenHostOnlySimContext context, ref int numUnitTypes, ref int maxGuardianTypes, PlannedWaveOptions options, Planet planetToUseForSpawningTypes );
    }
}
