
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TiberiumDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<TiberiumDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public int VeinMarkupCost = 500;
        public int NewVeinCost = 1200;
        public int NewDefenseCost = 300;
        public int DropshipCost = 50; //these spawn locusts
        public int NewTierOneShipCost = 500;
        public int NewTierTwoShipCost = 900;
        public int NewDireCost = 1200;
        public int PerSecondVeinIncome = 1;
        public int BonusIntervalIncome = 1;
        public int BonusInterval = 3;
        public int BaseSummoners = 1;
        public int BoostCPACost = 1000;
        public int CPABoostAmount = 500;
        public int SummonerCost = 3000;
        public int SummonTime = 1800;

        public int PerSecondDropshipIncome = 2;

        public short LocustRange = (short)4;
        public short MarkLevelForVeinNeighborsForDefense = (short)5;
        
        public FInt AdditionalSummonersPer50AIP = FInt.FromParts(0, 750);

        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }
        #region Pooling
        private static ReferenceTracker RefTracker;
        private TiberiumDifficulty() : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("TiberiumDifficulty");
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<TiberiumDifficulty> Pool = new ConcurrentPool<TiberiumDifficulty>("TiberiumDifficulty", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TiberiumDifficulty(); });

        public static TiberiumDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool(this);
        }
        private static ArcenTypeAnalyzer<TiberiumDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if (typeAnalyzer == null)
                typeAnalyzer = new ArcenTypeAnalyzer<TiberiumDifficulty>(new TiberiumDifficulty());
            typeAnalyzer.ApplyDefaults(this);
        }
        #endregion
    }
    public class TiberiumDifficultyTable : ArcenDynamicTable<TiberiumDifficulty>
    {
        public static TiberiumDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public TiberiumDifficultyTable() : base("TiberiumDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread)
        {
            Instance = this;
        }

        public override TiberiumDifficulty GetNewRowFromPool()
        {
            return TiberiumDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor(ArcenXMLElement Data, TiberiumDifficulty TypeDataObject)
        {
            //bool debug = false;
            Data.Fill("name", ref TypeDataObject.name, !Data.ReadingPartialRecord);
            Data.Fill("Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord);
            Data.Fill("VeinMarkupCost", ref TypeDataObject.VeinMarkupCost, !Data.ReadingPartialRecord);
            Data.Fill("NewVeinCost", ref TypeDataObject.NewVeinCost, !Data.ReadingPartialRecord);
            Data.Fill("NewDefenseCost", ref TypeDataObject.NewDefenseCost, !Data.ReadingPartialRecord);
            Data.Fill("NewTierOneShipCost", ref TypeDataObject.NewTierOneShipCost, !Data.ReadingPartialRecord);
            Data.Fill("NewTierTwoShipCost", ref TypeDataObject.NewTierTwoShipCost, !Data.ReadingPartialRecord);
            Data.Fill("NewDireCost", ref TypeDataObject.NewDireCost, !Data.ReadingPartialRecord);

            Data.Fill("PerSecondVeinIncome", ref TypeDataObject.PerSecondVeinIncome, !Data.ReadingPartialRecord);
            Data.Fill("BonusIntervalIncome", ref TypeDataObject.BonusIntervalIncome, !Data.ReadingPartialRecord);
            Data.Fill("BonusInterval", ref TypeDataObject.BonusInterval, !Data.ReadingPartialRecord);

            Data.Fill("DropshipCost", ref TypeDataObject.DropshipCost, false);
            Data.Fill("BaseSummoners", ref TypeDataObject.BaseSummoners, false);
            Data.Fill("BoostCPACost", ref TypeDataObject.BoostCPACost, false);
            Data.Fill("CPABoostAmount", ref TypeDataObject.CPABoostAmount, false);
            Data.Fill("SummonerCost", ref TypeDataObject.SummonerCost, false);
            Data.Fill("SummonTime", ref TypeDataObject.SummonTime, false);
            Data.Fill("PerSecondDropshipIncome", ref TypeDataObject.PerSecondDropshipIncome, false);
            Data.Fill("AdditionalSummonersPer50AIP", ref TypeDataObject.AdditionalSummonersPer50AIP, false);

            int locustRange = TypeDataObject.LocustRange;
            Data.Fill("LocustRange", ref locustRange, false);
            TypeDataObject.LocustRange = (short)locustRange;

            int markLevelForVeinNeighborsForDefense = TypeDataObject.MarkLevelForVeinNeighborsForDefense;
            Data.Fill("MarkLevelForVeinNeighborsForDefense", ref markLevelForVeinNeighborsForDefense, false);
            TypeDataObject.MarkLevelForVeinNeighborsForDefense = (short)markLevelForVeinNeighborsForDefense;

            return DelReturn.Continue;
        }
        public TiberiumDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in tiberium difficulty", Verbosity.DoNotShow );
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
