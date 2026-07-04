
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<DysonSidekickDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        //These are for Ravager Assaults
        public FInt RavagerAssaultAIPThreshold = FInt.FromParts(70, 000);
        public FInt BaseStrPerSecond;
        public FInt IncreasedStrPerSecondPer10AIP;
        public FInt IncreasedStrPerSecondPerScienceGenerator;
        public FInt IncreasedStrPerSecondPerMetalGenerator;
        public FInt IncreasedStrPerSecondPerResourceOneGenerator;
        public FInt IncreasedStrPerSecondPerHackingGenerator;
        public FInt IncreasedStrPerSecondPerRavagedPlanet;
        public int MinAttackStrength = 10 * 1000;
        public int MinAttackInterval = 660;
        public int MaxAttackInterval = 1600;

        public int AIPForReaperTierOne = 120;
        public int AIPForReaperTierTwo = 200; 
        public int AIPForReaperTierThree = 300;

        public int ImmobileRavagerTimeToRavage = 700;
        public int ImmobileRavagerTroopSpawnInterval = 90;
        public int ImmobileRavagerTroopSpawnStrengthPerAIP = 1 * 80;

        public int GatewayMarkupInterval = 600;
        public int GatewayLarvaInterval = 800;
        public int GatewayTroopSpawnInterval = 45;
        public int GatewayTroopSpawnStrengthPerAIP = 1 * 200;

        public int LarvaSpawnInterval = 600; //
        public int LunarInvasionInterval = 3600;

        public int AsteroidTransportSpawnInterval = 30;
        public int ChrysalisTransportSpawnInterval = 35;
        public int CuendillarMinedPerTransport = 6;

        //For the Dyson Sphere win condition
        public int TimeForSphereWin;
        public int SphereWinResponsePerAIPForReapers;
        public int SphereWinAttackIntervalForReapers;
        public int SphereWinResponsePerAIPForAI;
        public int SphereWinAttackIntervalForAI;

        //Overloading and Drilling work similarly
        public int AIPForPlanetDrilling = 3;
        public int DrillResponsePerAIP = 500; //so 100 AIP ==> 50str response
        public int DrillResponseIncreasePerTransport = 2000; 
        public int DrillAttackInterval = 50;
        public int PlanetsDrilledForFlagshipTierTwo = 2;
        public int PlanetsDrilledForFlagshipTierThree = 4;
        public int PlanetTransportSpawnInterval = 30;
        public int AIPForReaperMarkLevel = 100;

        public int AIPForPlanetOverloading = 40; //the AI gets very worried about destroying planets
        public int PlanetOverloadTime = 600;
        public int OverloadResponsePerAIP = 2000; //
        public int OverloadAttackInterval = 15;


        //For Chrysalises
        public int ChrysalisHatchTime = 700;
        public int BonusChrysalisChancePerRavagedPlanet = 35;
        public int ChrysalisSpawnInterval = 2000;
        public int ChrysalisCuendillar = 30;
        public int ChrysalisCuendillarVariance = 10;
        public int ChrysalisHatchBaseStrength = 50000;
        public int ChrysalisHatchSovereignsToSpawn = 2;
        public int ChrysalisHatchStrengthIncreasePerCuendillarRemaining = 35 * 1000;
        public int ChrysalisHatchAIPPerMarkLevel =  80; //allows ships to spawn at higher mark levels. Each X unlocks the next mark level
        public int ChrysalisHatchRavagerStrength = 20000;

        //For Lunar Invasions
        public int LunarInvasionSovereignsToSpawn = 5;
        public int LunarInvasionStrengthPerAIP = 2000;
        public int LunarInvasionStrengthPerStronghold = 10000;
        public int LunarInvasionExoStrengthPerAIP = 1500;
        //For planetoids
        public int AIPlanetoidDrillSpawnInterval = 2400;

        public int MaxGuardiansPerMinor = 7;
        public int MaxDireGuardiansPerMinor = 1;

        public int MaxGuardiansPerMajor = 12;
        public int MaxDireGuardiansPerMajor = 5;
        public override string ToString()
        {
            return name + " intensity " + Intensity;
        }
        #region Pooling
        private static ReferenceTracker RefTracker;
        private DysonSidekickDifficulty() : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("DysonSidekickDifficulty");
            RefTracker.IncrementObjectCount();
        }
        private static ConcurrentPool<DysonSidekickDifficulty> Pool = new ConcurrentPool<DysonSidekickDifficulty>("DysonSidekickDifficulty", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DysonSidekickDifficulty(); });

        public static DysonSidekickDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool(this);
        }
        private static ArcenTypeAnalyzer<DysonSidekickDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if (typeAnalyzer == null)
                typeAnalyzer = new ArcenTypeAnalyzer<DysonSidekickDifficulty>(new DysonSidekickDifficulty());
            typeAnalyzer.ApplyDefaults(this);
        }
        #endregion
    }
    public class DysonSidekickDifficultyTable : ArcenDynamicTable<DysonSidekickDifficulty>
    {
        public static DysonSidekickDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public DysonSidekickDifficultyTable() : base("Difficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread)
        {
            Instance = this;
        }

        public override DysonSidekickDifficulty GetNewRowFromPool()
        {
            return DysonSidekickDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor(ArcenXMLElement Data, DysonSidekickDifficulty TypeDataObject)
        {
            //bool debug = false;
            Data.Fill("name", ref TypeDataObject.name, !Data.ReadingPartialRecord);
            Data.Fill("Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord);
            Data.Fill("BaseStrPerSecond", ref TypeDataObject.BaseStrPerSecond, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPer10AIP", ref TypeDataObject.IncreasedStrPerSecondPer10AIP, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPerScienceGenerator", ref TypeDataObject.IncreasedStrPerSecondPerScienceGenerator, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPerMetalGenerator", ref TypeDataObject.IncreasedStrPerSecondPerMetalGenerator, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPerResourceOneGenerator", ref TypeDataObject.IncreasedStrPerSecondPerResourceOneGenerator, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPerHackingGenerator", ref TypeDataObject.IncreasedStrPerSecondPerHackingGenerator, !Data.ReadingPartialRecord);
            Data.Fill("IncreasedStrPerSecondPerRavagedPlanet", ref TypeDataObject.IncreasedStrPerSecondPerRavagedPlanet, !Data.ReadingPartialRecord);
            Data.Fill("MinAttackStrength", ref TypeDataObject.MinAttackStrength, false);
            Data.Fill("MinAttackInterval", ref TypeDataObject.MinAttackInterval, false);
            Data.Fill("MaxAttackInterval", ref TypeDataObject.MaxAttackInterval, false);

            Data.Fill("TimeForSphereWin", ref TypeDataObject.TimeForSphereWin, false);
            Data.Fill("SphereWinResponsePerAIPForReapers", ref TypeDataObject.SphereWinResponsePerAIPForReapers, false);
            Data.Fill("SphereWinAttackIntervalForReapers", ref TypeDataObject.SphereWinAttackIntervalForReapers, false);
            Data.Fill("SphereWinResponsePerAIPForAI", ref TypeDataObject.SphereWinResponsePerAIPForAI, false);
            Data.Fill("SphereWinAttackIntervalForAI", ref TypeDataObject.SphereWinAttackIntervalForAI, false);

            Data.Fill("AIPForPlanetDrilling", ref TypeDataObject.AIPForPlanetDrilling, false);
            Data.Fill("DrillResponsePerAIP", ref TypeDataObject.DrillResponsePerAIP, false);
            Data.Fill("DrillResponseIncreasePerTransport", ref TypeDataObject.DrillResponseIncreasePerTransport, false);
            Data.Fill("DrillAttackInterval", ref TypeDataObject.DrillAttackInterval, false);
            Data.Fill("AIPForReaperMarkLevel", ref TypeDataObject.AIPForReaperMarkLevel, false);

            Data.Fill("AIPForPlanetOverloading", ref TypeDataObject.AIPForPlanetOverloading, false);
            Data.Fill("PlanetOverloadTime", ref TypeDataObject.PlanetOverloadTime, false);
            Data.Fill("OverloadResponsePerAIP", ref TypeDataObject.OverloadResponsePerAIP, false);
            Data.Fill("OverloadAttackInterval", ref TypeDataObject.OverloadAttackInterval, false);

            // Data.Fill("AIPForPlanetCuendillarDrill", ref TypeDataObject.AIPForPlanetCuendillarDrill, false);
            // Data.Fill("PlanetCuendillarDrillTime", ref TypeDataObject.PlanetCuendillarDrillTime, false);
            // Data.Fill("CuendillarDrillResponsePerAIP", ref TypeDataObject.CuendillarDrillAttackInterval, false);
            // Data.Fill("CuendillarDrillAttackInterval", ref TypeDataObject.CuendillarDrillAttackInterval, false);
            // Data.Fill("CuendillarGeneratedFromDrilling", ref TypeDataObject.CuendillarGeneratedFromDrilling, false);

            Data.Fill("PlanetsDrilledForFlagshipTierTwo", ref TypeDataObject.PlanetsDrilledForFlagshipTierTwo, false);
            Data.Fill("PlanetsDrilledForFlagshipTierThree", ref TypeDataObject.PlanetsDrilledForFlagshipTierThree, false);

            Data.Fill("AIPForReaperTierOne", ref TypeDataObject.AIPForReaperTierOne, false);
            Data.Fill("AIPForReaperTierTwo", ref TypeDataObject.AIPForReaperTierTwo, false);
            Data.Fill("AIPForReaperTierThree", ref TypeDataObject.AIPForReaperTierThree, false);

            Data.Fill("ImmobileRavagerTimeToRavage", ref TypeDataObject.ImmobileRavagerTimeToRavage, false);
            Data.Fill("ImmobileRavagerTroopSpawnInterval", ref TypeDataObject.ImmobileRavagerTroopSpawnInterval, false);
            Data.Fill("ImmobileRavagerTroopSpawnStrengthPerAIP", ref TypeDataObject.ImmobileRavagerTroopSpawnStrengthPerAIP, false);

            Data.Fill("ChrysalisHatchTime", ref TypeDataObject.ChrysalisHatchTime, false);
            Data.Fill("ChrysalisSpawnInterval", ref TypeDataObject.ChrysalisSpawnInterval, false);
            Data.Fill("ChrysalisHatchBaseStrength", ref TypeDataObject.ChrysalisHatchBaseStrength, false);
            Data.Fill("ChrysalisHatchSovereignsToSpawn", ref TypeDataObject.ChrysalisHatchSovereignsToSpawn, false);
            Data.Fill("ChrysalisHatchRavagerStrength", ref TypeDataObject.ChrysalisHatchRavagerStrength, false);

            Data.Fill("LarvaSpawnInterval", ref TypeDataObject.LarvaSpawnInterval, false);

            Data.Fill("GatewayMarkupInterval", ref TypeDataObject.GatewayMarkupInterval, false);
            Data.Fill("GatewayLarvaInterval", ref TypeDataObject.GatewayLarvaInterval, false);
            Data.Fill("GatewayTroopSpawnInterval", ref TypeDataObject.GatewayTroopSpawnInterval, false);
            Data.Fill("GatewayTroopSpawnStrengthPerAIP", ref TypeDataObject.GatewayTroopSpawnStrengthPerAIP, false);

            Data.Fill("LunarInvasionInterval", ref TypeDataObject.LunarInvasionInterval, false);
            Data.Fill("LunarInvasionStrengthPerAIP", ref TypeDataObject.LunarInvasionStrengthPerAIP, false);
            Data.Fill("LunarInvasionStrengthPerStronghold", ref TypeDataObject.LunarInvasionStrengthPerStronghold, false);
            Data.Fill("LunarInvasionExoStrengthPerAIP", ref TypeDataObject.LunarInvasionExoStrengthPerAIP, false);
            Data.Fill("LunarInvasionSovereignsToSpawn", ref TypeDataObject.LunarInvasionSovereignsToSpawn, false);

            return DelReturn.Continue;
        }
        public DysonSidekickDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in dyson sidekick difficulty", Verbosity.DoNotShow );
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
