using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SappersDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<SappersDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public string Allegiance;
        public int SapperMetalPerSecond = 1;
        public int HabitatMetalPerSecond = 5;
        public int MaxHabitats = 5;
        public int TimeBetweenHabitatRespawns = 600;
        public int CostForSapper = 200;
        public int ResourceFromHarvestingCrystal = 50;
        public int TimeForCrystalToFlower = 100;
        public int BaseCrystalCost = 300;
        public int CrystalsPerPlanet = 5;

        public int SapperUpgradesAfterSpendingThisManyResources = 1600;
        public int SecondsForBasicToMarkUp = 600;
        public int SecondsForAdvancedToMarkUp = 600;
        public int SecondsForBeachheaderToMarkUp = 600;

        public int AIPIntervalForCalculatingBonusStructures = 50;
        public int BaseBasicStructuresPerPlanet = 5; //this can't be >= 2, since we require at least two basic structures to build a watchtower or beachheader
        public int BasicStructuresPerPlanetIncreasePerAIPInterval = 1;
        public int BasicStructuresPerPlanetIncreasePerSpireCity = 1;
        public int BaseAdvancedStructuresPerPlanet = 1;
        public int AdvancedStructuresPerPlanetIncreasePerAIPInterval = 0;
        public int AdvancedStructuresPerPlanetIncreasePerSpireCity = 1;
        public int BaseBeachheaderStructuresPerPlanet = 1;
        public int BeachheaderStructuresPerPlanetIncreasePerAIPInterval = 0;
        public int BeachheaderStructuresPerPlanetIncreasePerSpireCity = 1;

        public int BaseWatchtowerMaxStrength = 5 * 1000;
        public int WatchtowerStrengthIncreasePerAIPInterval = 6 * 1000;
        public int WatchtowerStrengthIncreasePerSpireCity = 10 * 1000;
        public int WatchtowerIncome = 10;

        public int SapperMarkLevelForAdvanced = 2;
        public int SapperMarkLevelForBeachheader = 3;

        public int BeachheadTurretInterval = 30;
        public int WatchtowerRange = 2;
        public override string ToString()
        {
            return name + " intensity " + Intensity + " " + Allegiance;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private SappersDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "SappersDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<SappersDifficulty> Pool = new ConcurrentPool<SappersDifficulty>( "SappersDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SappersDifficulty(); } );

        public static SappersDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<SappersDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<SappersDifficulty>( new SappersDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class SappersDifficultyTable : ArcenDynamicTable<SappersDifficulty>
    {
        public static SappersDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public SappersDifficultyTable() : base( "SapperDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override SappersDifficulty GetNewRowFromPool()
        {
            return SappersDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, SappersDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "Allegiance", ref TypeDataObject.Allegiance, !Data.ReadingPartialRecord );
            Data.Fill( "SapperMetalPerSecond", ref TypeDataObject.SapperMetalPerSecond, !Data.ReadingPartialRecord );
            Data.Fill( "HabitatMetalPerSecond", ref TypeDataObject.HabitatMetalPerSecond, !Data.ReadingPartialRecord );
            Data.Fill( "MaxHabitats", ref TypeDataObject.MaxHabitats, !Data.ReadingPartialRecord );
            Data.Fill( "TimeBetweenHabitatRespawns", ref TypeDataObject.TimeBetweenHabitatRespawns, !Data.ReadingPartialRecord );
            Data.Fill( "CostForSapper", ref TypeDataObject.CostForSapper, !Data.ReadingPartialRecord );
            Data.Fill( "ResourceFromHarvestingCrystal", ref TypeDataObject.ResourceFromHarvestingCrystal, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsForBasicToMarkUp", ref TypeDataObject.SecondsForBasicToMarkUp, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsForAdvancedToMarkUp", ref TypeDataObject.SecondsForAdvancedToMarkUp, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsForBeachheaderToMarkUp", ref TypeDataObject.SecondsForBeachheaderToMarkUp, !Data.ReadingPartialRecord );
            Data.Fill( "TimeForCrystalToFlower", ref TypeDataObject.TimeForCrystalToFlower, !Data.ReadingPartialRecord );
            Data.Fill( "BaseCrystalCost", ref TypeDataObject.BaseCrystalCost, !Data.ReadingPartialRecord );
            Data.Fill( "CrystalsPerPlanet", ref TypeDataObject.CrystalsPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "SapperUpgradesAfterSpendingThisManyResources", ref TypeDataObject.SapperUpgradesAfterSpendingThisManyResources, !Data.ReadingPartialRecord );

            Data.Fill( "BaseBasicStructuresPerPlanet", ref TypeDataObject.BaseBasicStructuresPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "BasicStructuresPerPlanetIncreasePerAIPInterval", ref TypeDataObject.BasicStructuresPerPlanetIncreasePerAIPInterval, !Data.ReadingPartialRecord );
            Data.Fill( "BasicStructuresPerPlanetIncreasePerSpireCity", ref TypeDataObject.BasicStructuresPerPlanetIncreasePerSpireCity, !Data.ReadingPartialRecord );
            Data.Fill( "BaseAdvancedStructuresPerPlanet", ref TypeDataObject.BaseAdvancedStructuresPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "AdvancedStructuresPerPlanetIncreasePerAIPInterval", ref TypeDataObject.AdvancedStructuresPerPlanetIncreasePerAIPInterval, !Data.ReadingPartialRecord );
            Data.Fill( "AdvancedStructuresPerPlanetIncreasePerSpireCity", ref TypeDataObject.AdvancedStructuresPerPlanetIncreasePerSpireCity, !Data.ReadingPartialRecord );

            Data.Fill( "BaseBeachheaderStructuresPerPlanet", ref TypeDataObject.BaseBeachheaderStructuresPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "BeachheaderStructuresPerPlanetIncreasePerAIPInterval", ref TypeDataObject.BeachheaderStructuresPerPlanetIncreasePerAIPInterval, !Data.ReadingPartialRecord );
            Data.Fill( "BeachheaderStructuresPerPlanetIncreasePerSpireCity", ref TypeDataObject.BeachheaderStructuresPerPlanetIncreasePerSpireCity, !Data.ReadingPartialRecord );

            Data.Fill( "BaseWatchtowerMaxStrength", ref TypeDataObject.BaseWatchtowerMaxStrength, !Data.ReadingPartialRecord );
            Data.Fill( "WatchtowerStrengthIncreasePerAIPInterval", ref TypeDataObject.WatchtowerStrengthIncreasePerAIPInterval, !Data.ReadingPartialRecord );
            Data.Fill( "WatchtowerStrengthIncreasePerSpireCity", ref TypeDataObject.WatchtowerStrengthIncreasePerSpireCity, !Data.ReadingPartialRecord );
            Data.Fill( "WatchtowerIncome", ref TypeDataObject.WatchtowerIncome, !Data.ReadingPartialRecord );

            Data.Fill( "SapperMarkLevelForAdvanced", ref TypeDataObject.SapperMarkLevelForAdvanced, !Data.ReadingPartialRecord );
            Data.Fill( "SapperMarkLevelForBeachheader", ref TypeDataObject.SapperMarkLevelForBeachheader, !Data.ReadingPartialRecord );

            Data.Fill( "BeachheadTurretInterval", ref TypeDataObject.BeachheadTurretInterval, !Data.ReadingPartialRecord );
            Data.Fill( "WatchtowerRange", ref TypeDataObject.WatchtowerRange, !Data.ReadingPartialRecord );

            return DelReturn.Continue;
        }
        public SappersDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in sapper difficulty", Verbosity.DoNotShow );
                return null;
            }
            string factionAllegiance = faction.BaseInfo.Allegiance;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionIntensity )
                {
                    if ( this.Rows[i].Allegiance == "Player" )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "Friendly To Players" ) )
                        {
                            return this.Rows[i];
                        }
                    }
                    else if ( this.Rows[i].Allegiance == "MinorFaction" )
                    {
                        if ( StringExtensions.Contains( factionAllegiance, "MinorFaction", StringComparison.CurrentCultureIgnoreCase ) ||
                            StringExtensions.Contains( factionAllegiance, "Minor Faction", StringComparison.CurrentCultureIgnoreCase ) )
                            return this.Rows[i];
                    }
                    else if ( this.Rows[i].Allegiance == "AI" )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "Allied To AI" ) )
                            return this.Rows[i];
                    }
                }
            }
            throw new Exception( "Could not find the right row for " + faction.GetDisplayName() + " factionIntensity " + factionIntensity + ", factionAllegiance '" + factionAllegiance + "'." );
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "We have " + this.Rows.Count + " sapper difficulties", Verbosity.DoNotShow );
        }
    }
}
