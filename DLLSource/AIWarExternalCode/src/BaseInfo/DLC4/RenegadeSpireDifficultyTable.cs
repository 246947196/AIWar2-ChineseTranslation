using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class RenegadeSpireDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<RenegadeSpireDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public string Description = "";

        // Metal income earned per surviving fracture per second
        public int MetalIncomePerFracturePerSecond = 2;

        // Seconds between each fracture marking up one level (applied per-fracture independently)
        public int FractureMarkupIntervalSeconds = 900;

        // Seconds between each ships increasing their mark level/tier
        public int ShipMarkupIntervalSeconds = 600;

        // Seconds between each fracture spawning a ship
        public int ShipSpawnIntervalSeconds = 120;

        // Seconds between relic-spawn attempts (timer resets after each attempt, hit or miss)
        public int RelicSpawnIntervalSeconds = 600;

        // Metal income per fracture per second, plus AIP scaling
        public int BaseMetalIncome = 10;
        public int BonusMetalIncomePer10AIP = 1;

        // Seconds between each fracture marking up one tier
        public int MarkupInterval = 600;

        // Seconds between periodic defiler spawns
        public int DefilerSpawnIntervalSeconds = 300;

        // Starting DefensiveMetalToSpend for each newly spawned defiler, plus AIP scaling
        public int BaseDefilerMetal = 100;
        public int BonusDefilerMetalPer10AIP = 2;

        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private RenegadeSpireDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "RenegadeSpireDifficulty" );
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<RenegadeSpireDifficulty> Pool = new ConcurrentPool<RenegadeSpireDifficulty>( "RenegadeSpireDifficulty", 99999,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new RenegadeSpireDifficulty(); } );

        public static RenegadeSpireDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<RenegadeSpireDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<RenegadeSpireDifficulty>( new RenegadeSpireDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class RenegadeSpireDifficultyTable : ArcenDynamicTable<RenegadeSpireDifficulty>
    {
        public static RenegadeSpireDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater() { }

        public RenegadeSpireDifficultyTable() : base( "RenegadeSpireDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override RenegadeSpireDifficulty GetNewRowFromPool()
        {
            return RenegadeSpireDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, RenegadeSpireDifficulty TypeDataObject )
        {
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "description", ref TypeDataObject.Description, false );
            Data.Fill( "MetalIncomePerFracturePerSecond", ref TypeDataObject.MetalIncomePerFracturePerSecond, false );
            Data.Fill( "FractureMarkupIntervalSeconds", ref TypeDataObject.FractureMarkupIntervalSeconds, false );
            Data.Fill( "ShipSpawnIntervalSeconds", ref TypeDataObject.ShipSpawnIntervalSeconds, false );
            Data.Fill( "RelicSpawnIntervalSeconds", ref TypeDataObject.RelicSpawnIntervalSeconds, false );
            Data.Fill( "BaseMetalIncome", ref TypeDataObject.BaseMetalIncome, false );
            Data.Fill( "BonusMetalIncomePer10AIP", ref TypeDataObject.BonusMetalIncomePer10AIP, false );
            Data.Fill( "MarkupInterval", ref TypeDataObject.MarkupInterval, false );
            Data.Fill( "ShipMarkupIntervalSeconds", ref TypeDataObject.ShipMarkupIntervalSeconds, false );
            Data.Fill( "DefilerSpawnIntervalSeconds", ref TypeDataObject.DefilerSpawnIntervalSeconds, false );
            Data.Fill( "BaseDefilerMetal", ref TypeDataObject.BaseDefilerMetal, false );
            Data.Fill( "BonusDefilerMetalPer10AIP", ref TypeDataObject.BonusDefilerMetalPer10AIP, false );
            return DelReturn.Continue;
        }

        public RenegadeSpireDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionIntensity )
                    return this.Rows[i];
            }
            ArcenDebugging.ArcenDebugLogSingleLine( "RenegadeSpireDifficultyTable: no row for intensity " + factionIntensity + " on faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
            return this.Rows.Count > 0 ? this.Rows[0] : null;
        }
    }
}
