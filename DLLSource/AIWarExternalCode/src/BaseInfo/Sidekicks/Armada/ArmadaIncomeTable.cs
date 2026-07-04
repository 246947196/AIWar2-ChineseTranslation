using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaIncome : ArcenDynamicTableRow, IConcurrentPoolable<ArmadaIncome>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public int IneligibleMiningInterval = 190; //how long do you need to wait before you can rebuild a mine on some planet
        public int MetalMineTime = 100;
        public int HaPMineTime = 130;
        public int ScienceMineTime = 150;

        public int BaseMetalProducedFromMine = 200000;
        public int MetalIncomeIncreaseFromMarkLevel = 40000;
        public int BaseScienceProducedFromMine = 1500;
        public int ScienceIncomeIncreaseFromMarkLevel = 125;
        public int BaseHackingProducedFromMine = 10;
        public int HackingIncomeIncreaseFromMarkLevel = 8;
        public int BaseTiberiumProducedFromMine = 100;
        public int TiberiumIncomeIncreaseFromMarkLevel = 20;

        public int RangerIncomePerSecond = 20;
        public int DireRangerIncomePerSecond = 100;
        public int OutpostForRangerCap = 8;
        public int OutpostForDireRangerCap = 5;

        public int LocustsSummoned = 7;

        public FInt MiningAmountDropoffPercentPerMine = FInt.FromParts(7, 000);
        public FInt MinimumMinePercent = FInt.FromParts(15, 000);
        public int MiningDepthIncreaseRate = 3;
        public const int PercentMineSpeedIncreasePerTier = 10;

        public int ProducerInterval = 600;

        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }
        #region Pooling
        private static ReferenceTracker RefTracker;
        private ArmadaIncome() : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("ArmadaIncome");
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<ArmadaIncome> Pool = new ConcurrentPool<ArmadaIncome>("ArmadaIncome", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ArmadaIncome(); });

        public static ArmadaIncome GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool(this);
        }
        private static ArcenTypeAnalyzer<ArmadaIncome> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if (typeAnalyzer == null)
                typeAnalyzer = new ArcenTypeAnalyzer<ArmadaIncome>(new ArmadaIncome());
            typeAnalyzer.ApplyDefaults(this);
        }
        #endregion
    }
    public class ArmadaIncomeTable : ArcenDynamicTable<ArmadaIncome>
    {
        public static ArmadaIncomeTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ArmadaIncomeTable() : base("ArmadaIncome", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread)
        {
            Instance = this;
        }

        public override ArmadaIncome GetNewRowFromPool()
        {
            return ArmadaIncome.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor(ArcenXMLElement Data, ArmadaIncome TypeDataObject)
        {
            //bool debug = false;
            Data.Fill("name", ref TypeDataObject.name, !Data.ReadingPartialRecord);
            Data.Fill("Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord);
            Data.Fill("IneligibleMiningInterval", ref TypeDataObject.IneligibleMiningInterval, !Data.ReadingPartialRecord);
            Data.Fill("MetalMineTime", ref TypeDataObject.MetalMineTime, !Data.ReadingPartialRecord);
            Data.Fill("HaPMineTime", ref TypeDataObject.HaPMineTime, !Data.ReadingPartialRecord);
            Data.Fill("ScienceMineTime", ref TypeDataObject.ScienceMineTime, !Data.ReadingPartialRecord);
            Data.Fill("BaseMetalProducedFromMine", ref TypeDataObject.BaseMetalProducedFromMine, !Data.ReadingPartialRecord);
            Data.Fill("BaseScienceProducedFromMine", ref TypeDataObject.BaseScienceProducedFromMine, !Data.ReadingPartialRecord);
            Data.Fill("BaseHackingProducedFromMine", ref TypeDataObject.BaseHackingProducedFromMine, !Data.ReadingPartialRecord);

            Data.Fill("MiningAmountDropoffPercentPerMine", ref TypeDataObject.MiningAmountDropoffPercentPerMine, !Data.ReadingPartialRecord);
            Data.Fill("MiningDepthIncreaseRate", ref TypeDataObject.MiningDepthIncreaseRate, !Data.ReadingPartialRecord);

            return DelReturn.Continue;

        }
        public ArmadaIncome GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in armada income", Verbosity.DoNotShow );
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
