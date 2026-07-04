using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SplinteringSpireDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<SplinteringSpireDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public FInt BudgetPerSecond_Base;
        public FInt BudgetPerSecond_IncreasePerHour;
        public FInt BudgetPerSecond_IncreasePer100AIP;

        public FInt MaxStrength_Base;
        public FInt MaxStrength_IncreasePerHour;
        public FInt MaxStrength_IncreasePer100AIP;

        public FInt RewardMultiplier;
        public FInt DarkSpireDismantleIntervalResponseMultiplier;
        public FInt DarkSpireDismantleFinalResponseMultiplier;

        public int SecondsToDismantleDarkSpireVG;
        public int DurationOfDerelicts;
        public int DarkSpireResponseIntervalWhenBeingDismantled;

        #region Pooling
        private static ReferenceTracker RefTracker;
        private SplinteringSpireDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "SplinteringSpireDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<SplinteringSpireDifficulty> Pool = new ConcurrentPool<SplinteringSpireDifficulty>( "SplinteringSpireDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SplinteringSpireDifficulty(); } );

        public static SplinteringSpireDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<SplinteringSpireDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<SplinteringSpireDifficulty>( new SplinteringSpireDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class SplinteringSpireDifficultyTable : ArcenDynamicTable<SplinteringSpireDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static SplinteringSpireDifficultyTable Instance;
        public SplinteringSpireDifficultyTable() : base( "SplinteringSpireDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override SplinteringSpireDifficulty GetNewRowFromPool()
        {
            return SplinteringSpireDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, SplinteringSpireDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "BudgetPerSecond_Base", ref TypeDataObject.BudgetPerSecond_Base, true);
            Data.Fill( "BudgetPerSecond_IncreasePerHour", ref TypeDataObject.BudgetPerSecond_IncreasePerHour, true);
            Data.Fill( "BudgetPerSecond_IncreasePer100AIP", ref TypeDataObject.BudgetPerSecond_IncreasePer100AIP, true);

            Data.Fill( "MaxStrength_Base", ref TypeDataObject.MaxStrength_Base, true);
            Data.Fill( "MaxStrength_IncreasePerHour", ref TypeDataObject.MaxStrength_IncreasePerHour, true);
            Data.Fill( "MaxStrength_IncreasePer100AIP", ref TypeDataObject.MaxStrength_IncreasePer100AIP, true);

            Data.Fill( "RewardMultiplier", ref TypeDataObject.RewardMultiplier, true );
            Data.Fill( "DarkSpireDismantleIntervalResponseMultiplier", ref TypeDataObject.DarkSpireDismantleIntervalResponseMultiplier, true );
            Data.Fill( "DarkSpireDismantleFinalResponseMultiplier", ref TypeDataObject.DarkSpireDismantleFinalResponseMultiplier, true );

            Data.Fill( "SecondsToDismantleDarkSpireVG", ref TypeDataObject.SecondsToDismantleDarkSpireVG, true );
            Data.Fill( "DurationOfDerelicts", ref TypeDataObject.DurationOfDerelicts, true );
            Data.Fill( "DarkSpireResponseIntervalWhenBeingDismantled", ref TypeDataObject.DarkSpireResponseIntervalWhenBeingDismantled, true );

            if ( TypeDataObject.Intensity < 0 || TypeDataObject.Intensity > 10 )
                throw new Exception( "Unknown intensity for SplinteringSpire difficulty " + TypeDataObject.name + ", intensity " + TypeDataObject.Intensity );

            return DelReturn.Continue;
        }

        public SplinteringSpireDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            SplinteringSpireDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                SplinteringSpireDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find SplinteringSpireDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
