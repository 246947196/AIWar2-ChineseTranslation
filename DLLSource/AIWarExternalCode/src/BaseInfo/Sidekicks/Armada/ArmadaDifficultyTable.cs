
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<ArmadaDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public int MaxRangersPerMajor = 12;
        public int MaxDireRangersPerMajor = 5;
        
        public int MaxRangersPerMinor = 7;
        public int MaxDireRangersPerMinor = 1;

        public int SwarmSpawnInterval = 80;

        public int AIResponseInterval = 3000;

        public FInt EnemyIncomePerSecond = FInt.FromParts(1, 500);
        public FInt EnemyPerSecondPer10AIP = FInt.FromParts(3, 000);
        public int BonusEnemyResponsePerMine = 2000;
        public int BonusEnemyResponsePerVeinEmpower = 4000;
        public int ScalingEnemyResponseForCumulativeVeinEmpowering = 500;
        public int AIPForHarderCPAs = 120;

        public int EnemyResponsePerVeinPerMarkLevel = 300;

        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }
        #region Pooling
        private static ReferenceTracker RefTracker;
        private ArmadaDifficulty() : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("ArmadaDifficulty");
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<ArmadaDifficulty> Pool = new ConcurrentPool<ArmadaDifficulty>("ArmadaDifficulty", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ArmadaDifficulty(); });

        public static ArmadaDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool(this);
        }
        private static ArcenTypeAnalyzer<ArmadaDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if (typeAnalyzer == null)
                typeAnalyzer = new ArcenTypeAnalyzer<ArmadaDifficulty>(new ArmadaDifficulty());
            typeAnalyzer.ApplyDefaults(this);
        }
        #endregion
    }
    public class ArmadaDifficultyTable : ArcenDynamicTable<ArmadaDifficulty>
    {
        public static ArmadaDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ArmadaDifficultyTable() : base("ArmadaDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread)
        {
            Instance = this;
        }

        public override ArmadaDifficulty GetNewRowFromPool()
        {
            return ArmadaDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor(ArcenXMLElement Data, ArmadaDifficulty TypeDataObject)
        {
            //bool debug = false;
            Data.Fill("name", ref TypeDataObject.name, !Data.ReadingPartialRecord);
            Data.Fill("Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord);
            Data.Fill("MaxRangersPerMinor", ref TypeDataObject.MaxRangersPerMinor, false);
            Data.Fill("MaxRangersPerMajor", ref TypeDataObject.MaxRangersPerMajor, false);

            Data.Fill("MaxDireRangersPerMinor", ref TypeDataObject.MaxRangersPerMinor, false);
            Data.Fill("MaxDireRangersPerMajor", ref TypeDataObject.MaxRangersPerMajor, false);

            Data.Fill("EnemyIncomePerSecond", ref TypeDataObject.EnemyIncomePerSecond, false);
            Data.Fill("EnemyIncomePerSecondPer10AIP", ref TypeDataObject.EnemyPerSecondPer10AIP, false);
            Data.Fill("BonusEnemyResponsePerMine", ref TypeDataObject.BonusEnemyResponsePerMine, false);


            return DelReturn.Continue;
        }
        public ArmadaDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in armada difficulty", Verbosity.DoNotShow );
                return null;
            }
            for (int i = 0; i < this.Rows.Count; i++)
            {
                if (this.Rows[i].Intensity == factionIntensity)
                    return this.Rows[i];
            }
            throw new Exception( "Could not find the right row for " + faction.GetDisplayName() + " idx " + faction.FactionIndex + " factionIntensity " + factionIntensity +"." );
        }
    }
}
