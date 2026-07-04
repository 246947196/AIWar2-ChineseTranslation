using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class InstigatorEffectData : ArcenDynamicTableRow, IConcurrentPoolable<InstigatorEffectData>, IProtectedListable
    {
        public string name;
        public FInt BaseStrengthPerInterval;
        public int IntervalInSeconds;
        public AIBudgetType BudgetToBoost;
        public FInt EffectMultiplierLowDifficulty;
        public FInt EffectMultiplierMediumDifficulty;
        public FInt EffectMultiplierHighDifficulty;
        public FInt BonusStrengthPerAIP;
        public string UnitTagsToSpawn;
        public FInt BaseStrengthInUnitsPerInterval;
        public FInt AIPIncrease;
        public int id;
        
        public string GetHoverText( Faction faction )
        {
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            FInt strength = BaseStrengthInUnitsPerInterval + BonusStrengthPerAIP * AIP;
            
            var sentinelsBase = faction.TryGetAISentinelsCoreData();
            if ( sentinelsBase == null )
                return string.Empty;
            
            string output = "This is a string version of Instigator Effect Data object " + this.id;
            
            if ( AIPIncrease > 0 )
            {
                output = "This structure boosts AIP by " + AIPIncrease.IntValue + " every " + IntervalInSeconds + " seconds.";
            }
            
            if ( this.BudgetToBoost != AIBudgetType.None )
            {
                int totalBudget = sentinelsBase.GetSpecificBudgetThreshold( AIBudgetType.Wave );
                /* budget = the in-game (NOT in-code) name of the budget type, as evaluated with the folowing switch statement.*/
                string budget;
                switch ( this.BudgetToBoost )
                {
                    case AIBudgetType.HunterFleet:
                        budget = "Hunter Fleet";
                        break;
                    case AIBudgetType.WormholeInvasion:
                        budget = "Wormhole Invasion";
                        break;
                    case AIBudgetType.Warden:
                        budget = "Warden Fleet";
                        break;
                    default:
                        budget = this.BudgetToBoost.ToString();
                        break;
                }
                
                string prefix = "This structure temporarily boosts the AI <color=#ff0000>" + budget + "</color> budget";
                if ( this.BudgetToBoost == AIBudgetType.Wave )
                    prefix = "This structures increases the power of the next <color=#ff0000>Wave</color>";
                
                if ( totalBudget <= 0 )
                {
                    output = prefix + " by " + strength.IntValue.ToString( "#,##0" ) + " every " + IntervalInSeconds + " seconds.";
                }
                else
                {
                    FInt Percent = (strength / totalBudget) * 100;
                    output = prefix + " by <color=#a1ffa1>" + Percent.ToFloatNonSim().ToString( "#,##0.0" ) + "%</color> of a wave every <color=#a1ffa1>" + IntervalInSeconds + "</color> seconds.";
                }
            }
            
            if ( !string.IsNullOrEmpty( UnitTagsToSpawn ) )
            {
                int totalWaveBudget = sentinelsBase.GetSpecificBudgetThreshold( AIBudgetType.Wave );
                if ( totalWaveBudget <= 0 )
                {
                    strength /= 1000;
                    output = "This structure spawns ships with Strength <color=#a1ffa1>" + strength.IntValue.ToString( "#,##0" ) + "</color> every <color=#a1ffa1>" + IntervalInSeconds + "</color> seconds.";
                }
                else
                {
                    FInt wavePercent = (strength / totalWaveBudget) * 100;
                    output = "This structure spawns ships which are <color=#a1ffa1>" + wavePercent.ToFloatNonSim().ToString( "#,##0.0" ) + "%</color> of a wave's strength every <color=#a1ffa1>" + IntervalInSeconds + "</color> seconds.";
                }
            }
            
            return output;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private InstigatorEffectData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "InstigatorEffectDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<InstigatorEffectData> Pool = new ConcurrentPool<InstigatorEffectData>( "InstigatorEffectDatas", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new InstigatorEffectData(); } );

        public static InstigatorEffectData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<InstigatorEffectData> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<InstigatorEffectData>( new InstigatorEffectData() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class InstigatorDataTable : ArcenDynamicTable<InstigatorEffectData>
    {
        //this is okay, this is a data lookup
        public static InstigatorDataTable Instance;
        public readonly Dictionary<int, InstigatorEffectData> RowById = Dictionary<int, InstigatorEffectData>.Create_WillNeverBeGCed( 40, "InstigatorDataTable-RowById" );
        public readonly List<InstigatorEffectData> AllEffects = List<InstigatorEffectData>.Create_WillNeverBeGCed( 200, "InstigatorDataTable-AllEffects" );
        public InstigatorDataTable() : base( "InstigatorData", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        public override InstigatorEffectData GetNewRowFromPool()
        {
            return InstigatorEffectData.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            RowById.Clear();
            AllEffects.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                InstigatorEffectData row = this.Rows[i];
                RowById[row.id] = row;
                AllEffects.Add( row );
            }
        }
        public InstigatorEffectData GetRowById( int id )
        {
            return RowById[id];
        }
        public InstigatorEffectData GetRandomRow( ArcenSimContextAnyStatus Context )
        {
            return AllEffects[Context.RandomToUse.Next( 0, AllEffects.Count )];
        }
        public InstigatorEffectData GetRandomRow( ArcenSimContextAnyStatus Context, byte diffOfStrongestAIFaction )
        {
            InstigatorEffectData data = null;
            int retries = 10;
            do
            {
                data = AllEffects[Context.RandomToUse.Next( 0, AllEffects.Count )];
                if ( data.BudgetToBoost == AIBudgetType.WormholeInvasion &&
                     (FactionUtilityMethods.Instance.GetCurrentAIP() < 300 ||
                       diffOfStrongestAIFaction < (byte)7) )
                    data = null;
            } while ( data == null && retries-- > 0 );
            return data;
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, InstigatorEffectData TypeDataObject )
        {
            bool debug = false;
            //Each entry is added to the XML, then code parsing that XML entry goes here,
            //and the variable the entry is stored in goes into the AstroTrainBehaviorType object

            //passing in "false" means that the field can be empty
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Parsed Instigator effect " + TypeDataObject.name, Verbosity.DoNotShow );
            Data.Fill( "base_strength_per_interval", ref TypeDataObject.BaseStrengthPerInterval, false );
            Data.FillEnum( "budget_to_boost", ref TypeDataObject.BudgetToBoost, false );
            Data.Fill( "interval_in_seconds", ref TypeDataObject.IntervalInSeconds, !Data.ReadingPartialRecord );
            Data.Fill( "bonus_strength_per_aip", ref TypeDataObject.BonusStrengthPerAIP, false );
            Data.Fill( "unit_tags_to_spawn", ref TypeDataObject.UnitTagsToSpawn, false );
            Data.Fill( "aip_increase", ref TypeDataObject.AIPIncrease, false );
            Data.Fill( "effect_multiplier_for_low_difficulty", ref TypeDataObject.EffectMultiplierLowDifficulty, false );
            Data.Fill( "effect_multiplier_for_medium_difficulty", ref TypeDataObject.EffectMultiplierMediumDifficulty, false );
            Data.Fill( "effect_multiplier_for_high_difficulty", ref TypeDataObject.EffectMultiplierHighDifficulty, false );

            Data.Fill( "id", ref TypeDataObject.id, !Data.ReadingPartialRecord );
            return DelReturn.Continue;
        }
    }
}
