using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WildHivesDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<WildHivesDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public short perSecondHive;
        public short perSecondWorker;

        public short workerCostBase;
        public short workerCostIncreasePerAlreadyBuilt;

        public short claimMineCost;
        public short claimCenterpieceCost;
        public short claimOtherNeutralCost;

        public short soldierCostBase;
        public short soldierCostIncreasePerAlreadyBuilt;

        public short secondsBetweenFriendlySoldierSpawns;

        public short maxSoldiersStoredPerHiveAndWorker;
        public short maxSoldiersStoredPerAdvancedHive;

        public int GetPerSecondBudgetForHive( GameEntity_Squad hive ) => hive.TypeData.GetHasTag( "AdvancedWildHive" ) ? perSecondHive * 2 : perSecondHive;
        public int GetMaxSoldiersToSpawn( GameEntity_Squad entity ) => entity.TypeData.GetHasTag( "AdvancedWildHive" ) ? maxSoldiersStoredPerAdvancedHive : maxSoldiersStoredPerHiveAndWorker;

        //public override string ToString()
        //{
        //    //For debug purposes
        //    string output = "WildHivesDifficulty " + name + ", Intensity" + Intensity + ", ";
        //
        //
        //
        //    output += "SecondsBetweenMigration_Large_New " + SecondsBetweenMigration_Large_New + "\n";
        //    output += "SecondsBetweenMigration_Large_Saved " + SecondsBetweenMigration_Large_Saved + "\n";
        //
        //    output += "SecondsBetweenMigration_Medium_New " + SecondsBetweenMigration_Medium_New + "\n";
        //    output += "SecondsBetweenMigration_Medium_Saved " + SecondsBetweenMigration_Medium_Saved + "\n";
        //
        //    output += "SecondsBetweenMigration_Small_New " + SecondsBetweenMigration_Small_New + "\n";
        //    output += "SecondsBetweenMigration_Small_Saved " + SecondsBetweenMigration_Small_Saved + "\n";
        //
        //    output += "MigrantsPerMigration_Large " + MigrantsPerMigration_Large + "\n";
        //    output += "MigrantsPerMigration_Medium " + MigrantsPerMigration_Medium + "\n";
        //    output += "MigrantsPerMigration_Small " + MigrantsPerMigration_Small + "\n";
        //
        //    output += "ClanlingsPerChamber_Base " + ClanlingsPerChamber_Base + "\n";
        //    output += "ClanlingsPerChamber_IncreasePer_Large" + ClanlingsPerChamber_IncreasePer_Large + "\n";
        //    output += "ClanlingsPerChamber_IncreasePer_Medium " + ClanlingsPerChamber_IncreasePer_Medium + "\n";
        //    output += "ClanlingsPerChamber_IncreasePer_Small " + ClanlingsPerChamber_IncreasePer_Small + "\n";
        //
        //    output += "MarkIncreasePerMigration_Large" + MarkIncreasePerMigration_Large + "\n";
        //    output += "MarkIncreasePerMigration_Medium" + MarkIncreasePerMigration_Medium + "\n";
        //    output += "MarkIncreasePerMigration_Small" + MarkIncreasePerMigration_Small + "\n";
        //    return output;
        //}

        #region Pooling
        private static ReferenceTracker RefTracker;
        private WildHivesDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "WildHivesDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<WildHivesDifficulty> Pool = new ConcurrentPool<WildHivesDifficulty>( "WildHivesDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new WildHivesDifficulty(); } );

        public static WildHivesDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<WildHivesDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<WildHivesDifficulty>( new WildHivesDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class WildHivesDifficultyTable : ArcenDynamicTable<WildHivesDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static WildHivesDifficultyTable Instance;

        public WildHivesDifficultyTable() : base( "WildHivesDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override WildHivesDifficulty GetNewRowFromPool()
        {
            return WildHivesDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, WildHivesDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "BudgetPerSecond_Hive", ref TypeDataObject.perSecondHive, !Data.ReadingPartialRecord );
            Data.Fill( "BudgetPerSecond_Worker", ref TypeDataObject.perSecondWorker, !Data.ReadingPartialRecord );

            Data.Fill( "WorkerCost_Base", ref TypeDataObject.workerCostBase, !Data.ReadingPartialRecord );
            Data.Fill( "WorkerCost_IncreasePerAlreadyBuilt", ref TypeDataObject.workerCostIncreasePerAlreadyBuilt, !Data.ReadingPartialRecord );

            Data.Fill( "ClaimMetalGeneratorCost", ref TypeDataObject.claimMineCost, !Data.ReadingPartialRecord );
            Data.Fill( "ClaimFleetCenterpieceCost", ref TypeDataObject.claimCenterpieceCost, !Data.ReadingPartialRecord );
            Data.Fill( "ClaimFleetOtherCost", ref TypeDataObject.claimOtherNeutralCost, !Data.ReadingPartialRecord );

            Data.Fill( "SoldierCost_Base", ref TypeDataObject.soldierCostBase, !Data.ReadingPartialRecord );
            Data.Fill( "SoldierCost_IncreasePerAlreadyBuilt", ref TypeDataObject.soldierCostIncreasePerAlreadyBuilt, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBetweenFriendlySoldierSpawns", ref TypeDataObject.secondsBetweenFriendlySoldierSpawns, !Data.ReadingPartialRecord );

            Data.Fill( "MaxSoldiersPer_HivesAndWorkers", ref TypeDataObject.maxSoldiersStoredPerHiveAndWorker, !Data.ReadingPartialRecord );
            Data.Fill( "MaxSoldiersPer_AdvancedHives", ref TypeDataObject.maxSoldiersStoredPerAdvancedHive, !Data.ReadingPartialRecord );

            return DelReturn.Continue;
        }

        public WildHivesDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            WildHivesDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                WildHivesDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find WildHivesDifficultyDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
        public WildHivesDifficulty GetRowForIntensity( int factionIntensity )
        {
            WildHivesDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                WildHivesDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find WildHivesDifficultyDifficulty for Intensity " + factionIntensity.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetRowForIntensity: " + outputRow.name + ", as difficulty for " + factionIntensity.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
