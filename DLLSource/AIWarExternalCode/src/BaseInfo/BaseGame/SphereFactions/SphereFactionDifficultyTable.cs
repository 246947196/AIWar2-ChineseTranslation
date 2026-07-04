using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SphereFactionDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<SphereFactionDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public FInt BudgetPerSecond_Base;
        public FInt BudgetPerSecond_IncreasePerHour;
        public FInt BudgetPerSecond_IncreasePer100AIP;
        public FInt BudgetPerSecond_MultiplierPerHack;

        public FInt MaxStrength_Base;
        public FInt MaxStrength_IncreasePerHour;
        public FInt MaxStrength_IncreasePer100AIP;
        public FInt MaxStrength_MultiplierPerHack;

        public FInt BudgetMultiplierWhenBelow10PercentCapacity;

        public int DurationOfAngerFromHack_Gray;
        public int DurationOfAngerFromHack_Chromatic;
        public int DurationOfAngerFromHack_Imperial;
        public int DurationOfAngerFromHack_Dark;
        public int DurationOfAngerFromHack_Zenith;

        public bool CanAntagonizerSpawn => CooldownBetweenAntagonizerSpawns > 0;
        public int CooldownBetweenAntagonizerSpawns;
        public int SecondsForAntagonizerToActivate_Base;
        public int SecondsForAntagonizerToActivate_IncreasePerHop;

        #region Pooling
        private static ReferenceTracker RefTracker;
        private SphereFactionDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "SphereFactionDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<SphereFactionDifficulty> Pool = new ConcurrentPool<SphereFactionDifficulty>( "SphereFactionDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SphereFactionDifficulty(); } );

        public static SphereFactionDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<SphereFactionDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<SphereFactionDifficulty>( new SphereFactionDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class SphereFactionDifficultyTable : ArcenDynamicTable<SphereFactionDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static SphereFactionDifficultyTable Instance;
        public SphereFactionDifficultyTable() : base( "SphereFactionDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override SphereFactionDifficulty GetNewRowFromPool()
        {
            return SphereFactionDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, SphereFactionDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "BudgetPerSecond_Base", ref TypeDataObject.BudgetPerSecond_Base, true);
            Data.Fill( "BudgetPerSecond_IncreasePerHour", ref TypeDataObject.BudgetPerSecond_IncreasePerHour, true);
            Data.Fill( "BudgetPerSecond_IncreasePer100AIP", ref TypeDataObject.BudgetPerSecond_IncreasePer100AIP, true);
            Data.Fill( "BudgetPerSecond_MultiplierPerHack", ref TypeDataObject.BudgetPerSecond_MultiplierPerHack, true);

            Data.Fill( "MaxStrength_Base", ref TypeDataObject.MaxStrength_Base, true);
            Data.Fill( "MaxStrength_IncreasePerHour", ref TypeDataObject.MaxStrength_IncreasePerHour, true);
            Data.Fill( "MaxStrength_IncreasePer100AIP", ref TypeDataObject.MaxStrength_IncreasePer100AIP, true);
            Data.Fill( "MaxStrength_MultiplierPerHack", ref TypeDataObject.MaxStrength_MultiplierPerHack, true);

            Data.Fill( "BudgetMultiplierWhenBelow10PercentCapacity", ref TypeDataObject.BudgetMultiplierWhenBelow10PercentCapacity, true );

            Data.Fill( "DurationOfAngerFromHack_Gray", ref TypeDataObject.DurationOfAngerFromHack_Gray, true );
            Data.Fill( "DurationOfAngerFromHack_Chromatic", ref TypeDataObject.DurationOfAngerFromHack_Chromatic, true );
            Data.Fill( "DurationOfAngerFromHack_Imperial", ref TypeDataObject.DurationOfAngerFromHack_Imperial, true );
            Data.Fill( "DurationOfAngerFromHack_Dark", ref TypeDataObject.DurationOfAngerFromHack_Dark, true );
            Data.Fill( "DurationOfAngerFromHack_Zenith", ref TypeDataObject.DurationOfAngerFromHack_Zenith, true );

            Data.Fill( "CooldownBetweenAntagonizerSpawns", ref TypeDataObject.CooldownBetweenAntagonizerSpawns, false );
            Data.Fill( "SecondsForAntagonizerToActivate_Base", ref TypeDataObject.SecondsForAntagonizerToActivate_Base, false );
            Data.Fill( "SecondsForAntagonizerToActivate_IncreasePerHop", ref TypeDataObject.SecondsForAntagonizerToActivate_IncreasePerHop, false );

            if ( TypeDataObject.Intensity < 0 || TypeDataObject.Intensity > 10 )
                throw new Exception( "Unknown intensity for SphereFaction difficulty " + TypeDataObject.name + ", intensity " + TypeDataObject.Intensity );

            return DelReturn.Continue;
        }

        public SphereFactionDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            SphereFactionDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                SphereFactionDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find SphereFactionDifficultyDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
