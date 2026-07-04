using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class NeinzulCustodiansDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<NeinzulCustodiansDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public int SecondsBeforeFirstInflux;

        public int SecondsBetweenInfluxPlayer;
        public int SecondsBetweenInfluxAI;
        public int SecondsBetweenInfluxNPC;
        public int SecondsBetweenInfluxIncreasePerExistingEnclave;

        public int MaxExpansionPlanetsPerInflux;
        public int MaxReinforcementsPerInflux;

        public int SecondsPerClanlingConstruction;

        public int EntireFactionMarkUpEveryXSeconds;
        public int IndividualUnitMarkUpAfterAliveEveryXSeconds;

        public int MaximumEnclaves_Base;
        public FInt MaximumEnclaves_IncreasePerHour;
        public FInt MaximumEnclaves_IncreasePer100AIP;

        public int MaximumClanlingsPerEnclave_Base;
        public FInt MaximumClanlingsPerEnclave_IncreasePerHour;
        public FInt MaximumClanlingsPerEnclave_IncreasePer100AIP;

        #region Pooling
        private static ReferenceTracker RefTracker;
        private NeinzulCustodiansDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "NeinzulCustodiansDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<NeinzulCustodiansDifficulty> Pool = new ConcurrentPool<NeinzulCustodiansDifficulty>( "NeinzulCustodiansDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new NeinzulCustodiansDifficulty(); } );

        public static NeinzulCustodiansDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<NeinzulCustodiansDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<NeinzulCustodiansDifficulty>( new NeinzulCustodiansDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class NeinzulCustodiansDifficultyTable : ArcenDynamicTable<NeinzulCustodiansDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static NeinzulCustodiansDifficultyTable Instance;

        public NeinzulCustodiansDifficultyTable() : base( "NeinzulCustodiansDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override NeinzulCustodiansDifficulty GetNewRowFromPool()
        {
            return NeinzulCustodiansDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, NeinzulCustodiansDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBeforeFirstInflux", ref TypeDataObject.SecondsBeforeFirstInflux, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsBetweenInfluxPlayer", ref TypeDataObject.SecondsBetweenInfluxPlayer, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenInfluxAI", ref TypeDataObject.SecondsBetweenInfluxAI, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenInfluxNPC", ref TypeDataObject.SecondsBetweenInfluxNPC, !Data.ReadingPartialRecord );
            Data.Fill( "SecondsBetweenInfluxIncreasePerExistingEnclave", ref TypeDataObject.SecondsBetweenInfluxIncreasePerExistingEnclave, !Data.ReadingPartialRecord );

            Data.Fill( "MaxExpansionPlanetsPerInflux", ref TypeDataObject.MaxExpansionPlanetsPerInflux, !Data.ReadingPartialRecord );
            Data.Fill( "MaxReinforcementsPerInflux", ref TypeDataObject.MaxReinforcementsPerInflux, !Data.ReadingPartialRecord );

            Data.Fill( "SecondsPerClanlingConstruction", ref TypeDataObject.SecondsPerClanlingConstruction, !Data.ReadingPartialRecord );

            Data.Fill( "EntireFactionMarkUpEveryXSeconds", ref TypeDataObject.EntireFactionMarkUpEveryXSeconds, !Data.ReadingPartialRecord );
            Data.Fill( "IndividualUnitMarkUpAfterAliveEveryXSeconds", ref TypeDataObject.IndividualUnitMarkUpAfterAliveEveryXSeconds, !Data.ReadingPartialRecord );

            Data.Fill( "MaximumEnclaves_Base", ref TypeDataObject.MaximumEnclaves_Base, !Data.ReadingPartialRecord );
            Data.Fill( "MaximumEnclaves_IncreasePerHour", ref TypeDataObject.MaximumEnclaves_IncreasePerHour, !Data.ReadingPartialRecord );
            Data.Fill( "MaximumEnclaves_IncreasePer100AIP", ref TypeDataObject.MaximumEnclaves_IncreasePer100AIP, !Data.ReadingPartialRecord );

            Data.Fill( "MaximumClanlingsPerEnclave_Base", ref TypeDataObject.MaximumClanlingsPerEnclave_Base, !Data.ReadingPartialRecord );
            Data.Fill( "MaximumClanlingsPerEnclave_IncreasePerHour", ref TypeDataObject.MaximumClanlingsPerEnclave_IncreasePerHour, !Data.ReadingPartialRecord );
            Data.Fill( "MaximumClanlingsPerEnclave_IncreasePer100AIP", ref TypeDataObject.MaximumClanlingsPerEnclave_IncreasePer100AIP, !Data.ReadingPartialRecord );

            return DelReturn.Continue;
        }

        public NeinzulCustodiansDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            NeinzulCustodiansDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                NeinzulCustodiansDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find NeinzulCustodiansDifficultyDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
