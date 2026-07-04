using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithArchitraveDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<ZenithArchitraveDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public int ExcessSpawnersToTriggerOtherArchitravesToAttackMe;
        public int MaxMetalReserves;
        public int MaxGolemMetalReserves;
        public int StrengthPerSpawner;
        public int SpawnerStrengthIncreasePerMark;
        public int AttritionPercent;
        public int SecondsToUpgrade;
        public int TimeBetweenTerritoryIncrease;
        public int BaseMetalIncome;
        public int BaseDefensiveMetalIncome;
        public int BaseMetalIncomeCivilWar;
        public int PeaceTimeBeforePioneers;
        public int DefensiveStructuresPerMarkLevel;
        public int UtilityStructuresPerMarkLevel;
        public int WarIntervalForStrengthIncrease;
        public int MaxSpawnerMarkLevel;
        public FInt AllowedStrengthIncreaseMultiplierPerInterval;
        public int BaseSpawnerWarpInTime;
        public int SpawnerWarpInTimeVariance;
        public readonly List<ArchitraveIncomeModifier> IncomeModifiers = List<ArchitraveIncomeModifier>.Create_WillNeverBeGCed( 300, "ZenithArchitraveDifficulty-IncomeModifiers" );
        //Add strength numbers for spawners
        public override string ToString()
        {
            return name + " intensity " + Intensity + " excessspawners to trigger " + ExcessSpawnersToTriggerOtherArchitravesToAttackMe + " max metal reserves " + MaxMetalReserves + " StrengthPerSpawner " + StrengthPerSpawner + " SpawnerStrengthIncreasePerMark " + SpawnerStrengthIncreasePerMark + " attrition percent " + AttritionPercent + " TimeBetweenTerritoryIncrease " + TimeBetweenTerritoryIncrease + " SecondsToUpgrade " + SecondsToUpgrade;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ZenithArchitraveDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ZenithArchitraveDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ZenithArchitraveDifficulty> Pool = new ConcurrentPool<ZenithArchitraveDifficulty>( "ZenithArchitraveDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ZenithArchitraveDifficulty(); } );

        public static ZenithArchitraveDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ZenithArchitraveDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ZenithArchitraveDifficulty>( new ZenithArchitraveDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class ZenithArchitraveDifficultyTable : ArcenDynamicTable<ZenithArchitraveDifficulty>
    {
        public static ZenithArchitraveDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ZenithArchitraveDifficultyTable() : base( "ZenithArchitraveDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override ZenithArchitraveDifficulty GetNewRowFromPool()
        {
            return ZenithArchitraveDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, ZenithArchitraveDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "ExcessSpawnersToTriggerOtherArchitravesToAttackMe", ref TypeDataObject.ExcessSpawnersToTriggerOtherArchitravesToAttackMe, !Data.ReadingPartialRecord );
            Data.Fill( "MaxMetalReserves", ref TypeDataObject.MaxMetalReserves, !Data.ReadingPartialRecord );
            Data.Fill( "MaxGolemMetalReserves", ref TypeDataObject.MaxGolemMetalReserves, !Data.ReadingPartialRecord );
            Data.Fill( "StrengthPerSpawner", ref TypeDataObject.StrengthPerSpawner, !Data.ReadingPartialRecord );
            Data.Fill( "SpawnerStrengthIncreasePerMark", ref TypeDataObject.SpawnerStrengthIncreasePerMark, !Data.ReadingPartialRecord );
            Data.Fill( "AttritionPercent", ref TypeDataObject.AttritionPercent, !Data.ReadingPartialRecord );
            Data.Fill( "DefensiveStructuresPerMarkLevel", ref TypeDataObject.DefensiveStructuresPerMarkLevel, !Data.ReadingPartialRecord );
            Data.Fill( "UtilityStructuresPerMarkLevel", ref TypeDataObject.UtilityStructuresPerMarkLevel, !Data.ReadingPartialRecord );
            Data.Fill( "TimeBetweenTerritoryIncrease", ref TypeDataObject.TimeBetweenTerritoryIncrease, !Data.ReadingPartialRecord );
            Data.Fill( "PeaceTimeBeforePioneers", ref TypeDataObject.PeaceTimeBeforePioneers, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsToUpgradeSpawner", ref TypeDataObject.SecondsToUpgrade, !Data.ReadingPartialRecord );
            Data.Fill( "BaseMetalIncome", ref TypeDataObject.BaseMetalIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseDefensiveMetalIncome", ref TypeDataObject.BaseDefensiveMetalIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseMetalIncomeCivilWar", ref TypeDataObject.BaseMetalIncomeCivilWar, !Data.ReadingPartialRecord );
            Data.Fill( "WarIntervalForStrengthIncrease", ref TypeDataObject.WarIntervalForStrengthIncrease, !Data.ReadingPartialRecord );
            Data.Fill( "MaxSpawnerMarkLevel", ref TypeDataObject.MaxSpawnerMarkLevel, !Data.ReadingPartialRecord );
            Data.Fill( "AllowedStrengthIncreaseMultiplierPerInterval", ref TypeDataObject.AllowedStrengthIncreaseMultiplierPerInterval, !Data.ReadingPartialRecord );
            Data.Fill( "BaseSpawnerWarpInTime", ref TypeDataObject.BaseSpawnerWarpInTime, !Data.ReadingPartialRecord );
            Data.Fill( "SpawnerWarpInTimeVariance", ref TypeDataObject.SpawnerWarpInTimeVariance, !Data.ReadingPartialRecord );
            foreach ( ArcenXMLElement node in Data.ChildrenOfType_AndParentsAndPartials( "IncomeModifier" ) )
            {
                try
                {
                    ArchitraveIncomeModifier increase = new ArchitraveIncomeModifier();
                    node.Fill( "Name", ref increase.Name, !node.ReadingPartialRecord );
                    node.Fill( "TimeInterval", ref increase.TimeInterval, !node.ReadingPartialRecord );
                    node.Fill( "CivilWarOffensiveOnly", ref increase.CivilWarOffensiveOnly, false );
                    node.Fill( "CivilWarDefensive", ref increase.CivilWarDefensive, false );
                    node.Fill( "FallenSpireCities", ref increase.FallenSpireCities, false );
                    node.Fill( "ControlsLessThanXPlanets", ref increase.ControlsLessThanXPlanets, false );
                    node.Fill( "WithoutFullTerritory", ref increase.WithoutFullTerritory, false );
                    node.Fill( "AdditiveIncrease", ref increase.AdditiveIncrease, false );
                    node.Fill( "MultiplicativeIncrease", ref increase.MultiplicativeIncrease, false );
                    node.Fill( "Multiplier", ref increase.Multiplier, false );
                    node.Fill( "UnitTag", ref increase.UnitTag, false );

                    TypeDataObject.IncomeModifiers.Add( increase );
                }
                catch ( Exception e )
                {
                    throw new ArcenDataReadException( "IncomeModifier: " + node.LimitedOuterXml + Environment.NewLine + e );
                }
            }

            return DelReturn.Continue;
        }
        public ZenithArchitraveDifficulty GetRowByIntensity( int factionItensity )
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
