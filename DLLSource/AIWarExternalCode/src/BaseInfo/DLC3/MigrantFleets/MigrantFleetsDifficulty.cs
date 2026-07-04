using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MigrantFleetsDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<MigrantFleetsDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public short SecondsBetweenMigration_Large_New;
        public short SecondsBetweenMigration_Large_Saved;

        public short SecondsBetweenMigration_Medium_New;
        public short SecondsBetweenMigration_Medium_Saved;

        public short SecondsBetweenMigration_Small_New;
        public short SecondsBetweenMigration_Small_Saved;

        public short MigrantsPerMigration_Large;
        public short MigrantsPerMigration_Medium;
        public short MigrantsPerMigration_Small;

        public short ChambersPerEnclave_Base;
        public FInt ChambersPerEnclave_IncreasePer_Large;
        public FInt ChambersPerEnclave_IncreasePer_Medium;
        public FInt ChambersPerEnclave_IncreasePer_Small;

        public FInt MarkIncreasePerMigration_Large;
        public FInt MarkIncreasePerMigration_Medium;
        public FInt MarkIncreasePerMigration_Small;

        public short GetChambersPerEnclave( MigrantFleetsFactionBaseInfo data )
        {
            short baseValue = ChambersPerEnclave_Base;
            FInt fromLarge = data.TimesTriggered( MigrantFleetEscortSize.Large ) * ChambersPerEnclave_IncreasePer_Large;
            FInt fromMedium = data.TimesTriggered( MigrantFleetEscortSize.Medium ) * ChambersPerEnclave_IncreasePer_Medium;
            FInt fromSmall = data.TimesTriggered( MigrantFleetEscortSize.Small ) * ChambersPerEnclave_IncreasePer_Small;
            return (short)(baseValue + (fromLarge + fromMedium + fromSmall).GetNearestIntPreferringLower());
        }
        public byte GetMarkLevel( MigrantFleetsFactionBaseInfo data )
        {
            FInt fromLarge = data.TimesTriggered( MigrantFleetEscortSize.Large ) * MarkIncreasePerMigration_Large;
            FInt fromMedium = data.TimesTriggered( MigrantFleetEscortSize.Medium ) * MarkIncreasePerMigration_Medium;
            FInt fromSmall = data.TimesTriggered( MigrantFleetEscortSize.Small ) * MarkIncreasePerMigration_Small;
            return (byte)Math.Min( 7, 1 + (fromLarge + fromMedium + fromSmall).GetNearestIntPreferringLower() );
        }

        public override string ToString()
        {
            //For debug purposes
            string output = "MigrantFleetsDifficulty " + name + ", Intensity" + Intensity + ", ";
            output += "SecondsBetweenMigration_Large_New " + SecondsBetweenMigration_Large_New + "\n";
            output += "SecondsBetweenMigration_Large_Saved " + SecondsBetweenMigration_Large_Saved + "\n";

            output += "SecondsBetweenMigration_Medium_New " + SecondsBetweenMigration_Medium_New + "\n";
            output += "SecondsBetweenMigration_Medium_Saved " + SecondsBetweenMigration_Medium_Saved + "\n";

            output += "SecondsBetweenMigration_Small_New " + SecondsBetweenMigration_Small_New + "\n";
            output += "SecondsBetweenMigration_Small_Saved " + SecondsBetweenMigration_Small_Saved + "\n";

            output += "MigrantsPerMigration_Large " + MigrantsPerMigration_Large + "\n";
            output += "MigrantsPerMigration_Medium " + MigrantsPerMigration_Medium + "\n";
            output += "MigrantsPerMigration_Small " + MigrantsPerMigration_Small + "\n";

            output += "ChambersPerEnclave_Base " + ChambersPerEnclave_Base + "\n";
            output += "ChambersPerEnclave_IncreasePer_Large" + ChambersPerEnclave_IncreasePer_Large + "\n";
            output += "ChambersPerEnclave_IncreasePer_Medium " + ChambersPerEnclave_IncreasePer_Medium + "\n";
            output += "ChambersPerEnclave_IncreasePer_Small " + ChambersPerEnclave_IncreasePer_Small + "\n";

            output += "MarkIncreasePerMigration_Large" + MarkIncreasePerMigration_Large + "\n";
            output += "MarkIncreasePerMigration_Medium" + MarkIncreasePerMigration_Medium + "\n";
            output += "MarkIncreasePerMigration_Small" + MarkIncreasePerMigration_Small + "\n";
            return output;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private MigrantFleetsDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "MigrantFleetsDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<MigrantFleetsDifficulty> Pool = new ConcurrentPool<MigrantFleetsDifficulty>( "MigrantFleetsDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MigrantFleetsDifficulty(); } );

        public static MigrantFleetsDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<MigrantFleetsDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<MigrantFleetsDifficulty>( new MigrantFleetsDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class MigrantFleetsDifficultyTable : ArcenDynamicTable<MigrantFleetsDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static MigrantFleetsDifficultyTable Instance;

        public MigrantFleetsDifficultyTable() : base( "MigrantFleetsDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override MigrantFleetsDifficulty GetNewRowFromPool()
        {
            return MigrantFleetsDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, MigrantFleetsDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBetweenMigration_Large_New", ref TypeDataObject.SecondsBetweenMigration_Large_New, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenMigration_Large_Saved", ref TypeDataObject.SecondsBetweenMigration_Large_Saved, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBetweenMigration_Medium_New", ref TypeDataObject.SecondsBetweenMigration_Medium_New, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenMigration_Medium_Saved", ref TypeDataObject.SecondsBetweenMigration_Medium_Saved, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBetweenMigration_Small_New", ref TypeDataObject.SecondsBetweenMigration_Small_New, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenMigration_Small_Saved", ref TypeDataObject.SecondsBetweenMigration_Small_Saved, !Data.ReadingPartialRecord );

            Data.Fill( "MigrantsPerMigration_Large", ref TypeDataObject.MigrantsPerMigration_Large, !Data.ReadingPartialRecord );
            Data.Fill( "MigrantsPerMigration_Medium", ref TypeDataObject.MigrantsPerMigration_Medium, !Data.ReadingPartialRecord );
            Data.Fill( "MigrantsPerMigration_Small", ref TypeDataObject.MigrantsPerMigration_Small, !Data.ReadingPartialRecord );

            Data.Fill( "ChambersPerEnclave_Base", ref TypeDataObject.ChambersPerEnclave_Base, !Data.ReadingPartialRecord );
            Data.Fill( "ChambersPerEnclave_IncreasePer_Large", ref TypeDataObject.ChambersPerEnclave_IncreasePer_Large, !Data.ReadingPartialRecord );
            Data.Fill( "ChambersPerEnclave_IncreasePer_Medium", ref TypeDataObject.ChambersPerEnclave_IncreasePer_Medium, !Data.ReadingPartialRecord );
            Data.Fill( "ChambersPerEnclave_IncreasePer_Small", ref TypeDataObject.ChambersPerEnclave_IncreasePer_Small, !Data.ReadingPartialRecord );

            Data.Fill( "MarkIncreasePerMigration_Large", ref TypeDataObject.MarkIncreasePerMigration_Large, !Data.ReadingPartialRecord );
            Data.Fill( "MarkIncreasePerMigration_Medium", ref TypeDataObject.MarkIncreasePerMigration_Medium, !Data.ReadingPartialRecord );
            Data.Fill( "MarkIncreasePerMigration_Small", ref TypeDataObject.MarkIncreasePerMigration_Small, !Data.ReadingPartialRecord );
            return DelReturn.Continue;
        }

        public MigrantFleetsDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            MigrantFleetsDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                MigrantFleetsDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find MigrantFleetsDifficultyDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
