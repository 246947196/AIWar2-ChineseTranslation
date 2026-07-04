using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithMinersDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<ZenithMinersDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public int TimeForFirstProbe;
        public int ProbeInterval; //how long between probes
        public int ProbeDuration; //how long after probe arrival does miner appear
        public int MinerDuration; //how after miner does it eat the planet
        public FInt BaseHackingResponse;

        public byte PercentChanceTwoPlanets;
        public byte PercentChanceThreePlanets;
        public byte PercentChanceFourPlanets;
        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ZenithMinersDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ZenithMinersDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ZenithMinersDifficulty> Pool = new ConcurrentPool<ZenithMinersDifficulty>( "ZenithMinersDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ZenithMinersDifficulty(); } );

        public static ZenithMinersDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ZenithMinersDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ZenithMinersDifficulty>( new ZenithMinersDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class ZenithMinersDifficultyTable : ArcenDynamicTable<ZenithMinersDifficulty>
    {
        public static ZenithMinersDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ZenithMinersDifficultyTable() : base( "ZenithMinersDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override ZenithMinersDifficulty GetNewRowFromPool()
        {
            return ZenithMinersDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, ZenithMinersDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "ProbeInterval", ref TypeDataObject.ProbeInterval, !Data.ReadingPartialRecord );
            Data.Fill( "TimeForFirstProbe", ref TypeDataObject.TimeForFirstProbe, !Data.ReadingPartialRecord );

            Data.Fill( "ProbeDuration", ref TypeDataObject.ProbeDuration, !Data.ReadingPartialRecord );
            Data.Fill( "MinerDuration", ref TypeDataObject.MinerDuration, !Data.ReadingPartialRecord );
            Data.Fill( "BaseHackingResponse", ref TypeDataObject.BaseHackingResponse, !Data.ReadingPartialRecord );
            Data.Fill( "PercentChanceTwoPlanets", ref TypeDataObject.PercentChanceTwoPlanets, !Data.ReadingPartialRecord );
            Data.Fill( "PercentChanceThreePlanets", ref TypeDataObject.PercentChanceThreePlanets, false );
            Data.Fill( "PercentChanceFourPlanets", ref TypeDataObject.PercentChanceFourPlanets, false );
            return DelReturn.Continue;
        }
        public ZenithMinersDifficulty GetRowByIntensity( int factionItensity )
        {
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionItensity )
                    return this.Rows[i];
            }
            return null;
        }
    }
}
