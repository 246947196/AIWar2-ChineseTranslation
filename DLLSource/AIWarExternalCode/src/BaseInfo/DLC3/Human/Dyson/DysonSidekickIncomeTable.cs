using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickIncome : ArcenDynamicTableRow, IConcurrentPoolable<DysonSidekickIncome>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public FInt BaseScienceIncomePerSecond;
        public FInt BaseMetalIncomePerSecond;
        public FInt BaseResourceOneIncomePerSecond;
        public FInt BaseHackingIncomePerSecond;
        public FInt ScienceIncomePerGeneratorPerSecond;
        public FInt MetalIncomePerGeneratorPerSecond;
        public FInt ResourceOneIncomePerGeneratorPerSecond;
        public FInt HackingIncomePerGeneratorPerSecond;

        public FInt ScienceIncomePerGeneratorPerSecondIncreasePerMarkLevel;
        public FInt MetalIncomePerGeneratorPerSecondIncreasePerMarkLevel;
        public FInt ResourceOneIncomePerGeneratorPerSecondIncreasePerMarkLevel;
        public FInt HackingIncomePerGeneratorPerSecondIncreasePerMarkLevel;

        public int StartingCuendillarForAsteroid = 5;
        public int CuendillarAsteroidVariance = 8;

        public int CuendillarPlanetaryVariance = 105;
        public int CuendillarMinForNonHomeworld = 50;
        public int CuendillarMinForHomeworldOrBastion = 90;

        public int PlanetoidSpawnInterval = 2000;
        public int StartingCuendillarForPlanetoid = 3;
        public int CuendillarPlanetoidVariance = 4;

        public int GuardianIncomePerSecond = 26;
        public int DireGuardianIncomePerSecond = 111;

        public int BoosterForGuardianCap = 8;
        public int BoosterForDireGuardianCap = 5;

        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }
        #region Pooling
        private static ReferenceTracker RefTracker;
        private DysonSidekickIncome() : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("DysonSidekickIncome");
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<DysonSidekickIncome> Pool = new ConcurrentPool<DysonSidekickIncome>("DysonSidekickIncome", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DysonSidekickIncome(); });

        public static DysonSidekickIncome GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool(this);
        }
        private static ArcenTypeAnalyzer<DysonSidekickIncome> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if (typeAnalyzer == null)
                typeAnalyzer = new ArcenTypeAnalyzer<DysonSidekickIncome>(new DysonSidekickIncome());
            typeAnalyzer.ApplyDefaults(this);
        }
        #endregion
    }
    public class DysonSidekickIncomeTable : ArcenDynamicTable<DysonSidekickIncome>
    {
        public static DysonSidekickIncomeTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public DysonSidekickIncomeTable() : base("Income", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread)
        {
            Instance = this;
        }

        public override DysonSidekickIncome GetNewRowFromPool()
        {
            return DysonSidekickIncome.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor(ArcenXMLElement Data, DysonSidekickIncome TypeDataObject)
        {
            //bool debug = false;
            Data.Fill("name", ref TypeDataObject.name, !Data.ReadingPartialRecord);
            Data.Fill("Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord);
            Data.Fill("BaseScienceIncomePerSecond", ref TypeDataObject.BaseScienceIncomePerSecond, !Data.ReadingPartialRecord);
            Data.Fill("BaseMetalIncomePerSecond", ref TypeDataObject.BaseMetalIncomePerSecond, !Data.ReadingPartialRecord);
            Data.Fill("BaseResourceOneIncomePerSecond", ref TypeDataObject.BaseResourceOneIncomePerSecond, !Data.ReadingPartialRecord);
            Data.Fill("BaseHackingIncomePerSecond", ref TypeDataObject.BaseHackingIncomePerSecond, !Data.ReadingPartialRecord);
            Data.Fill("ScienceIncomePerGeneratorPerSecond", ref TypeDataObject.ScienceIncomePerGeneratorPerSecond, !Data.ReadingPartialRecord);
            Data.Fill("MetalIncomePerGeneratorPerSecond", ref TypeDataObject.MetalIncomePerGeneratorPerSecond, !Data.ReadingPartialRecord);
            Data.Fill("ResourceOneIncomePerGeneratorPerSecond", ref TypeDataObject.ResourceOneIncomePerGeneratorPerSecond, !Data.ReadingPartialRecord);
            Data.Fill("HackingIncomePerGeneratorPerSecond", ref TypeDataObject.HackingIncomePerGeneratorPerSecond, !Data.ReadingPartialRecord);
            
            Data.Fill("ScienceIncomePerGeneratorPerSecondIncreasePerMarkLevel", ref TypeDataObject.ScienceIncomePerGeneratorPerSecondIncreasePerMarkLevel, !Data.ReadingPartialRecord);
            Data.Fill("MetalIncomePerGeneratorPerSecondIncreasePerMarkLevel", ref TypeDataObject.MetalIncomePerGeneratorPerSecondIncreasePerMarkLevel, !Data.ReadingPartialRecord);
            Data.Fill("ResourceOneIncomePerGeneratorPerSecondIncreasePerMarkLevel", ref TypeDataObject.ResourceOneIncomePerGeneratorPerSecondIncreasePerMarkLevel, !Data.ReadingPartialRecord);
            Data.Fill("HackingIncomePerGeneratorPerSecondIncreasePerMarkLevel", ref TypeDataObject.HackingIncomePerGeneratorPerSecondIncreasePerMarkLevel, !Data.ReadingPartialRecord);

            Data.Fill("StartingCuendillarForAsteroid", ref TypeDataObject.StartingCuendillarForAsteroid, false);
            Data.Fill("CuendillarAsteroidVariance", ref TypeDataObject.CuendillarAsteroidVariance, false);
            Data.Fill("PlanetoidSpawnInterval", ref TypeDataObject.PlanetoidSpawnInterval, false);
            Data.Fill("StartingCuendillarForPlanetoid", ref TypeDataObject.StartingCuendillarForPlanetoid, false);
            Data.Fill("CuendillarPlanetoidVariance", ref TypeDataObject.CuendillarPlanetoidVariance, false);
                        

            return DelReturn.Continue;

        }
        public DysonSidekickIncome GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in dyson sidekick income", Verbosity.DoNotShow );
                return null;
            }
            string factionAllegiance = faction.BaseInfo.Allegiance;
            for (int i = 0; i < this.Rows.Count; i++)
            {
                if (this.Rows[i].Intensity == factionIntensity)
                    return this.Rows[i];
            }
            throw new Exception( "Could not find the right row for " + faction.GetDisplayName() + " idx " + faction.FactionIndex + " factionIntensity " + factionIntensity + ", factionAllegiance '" + factionAllegiance + "'." + " We saw " + this.Rows.Count + " rows and had no matches on intensity." );
        }

    }

}
