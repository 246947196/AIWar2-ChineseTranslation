using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PraetorianGuardTypeData : ArcenDynamicTableRow, IConcurrentPoolable<PraetorianGuardTypeData>, IProtectedListable
    {
        public string Abbreviation = string.Empty;
        public string Description = string.Empty;
        public readonly AIBudgetType BudgetType = AIBudgetType.PraetorianGuard;

        public bool AllowEntryIntoHostileTerritory = true;
        public bool ShouldCountAsThreat = false;
        public FInt HostileRatio_TooDangerousToStay = FInt.FromParts( 2, 000 );
        public FInt HostileRatio_TooDangerousToTarget = FInt.FromParts( 2, 000 );
        public FInt HostileRatio_TooDangerousToGoThrough = FInt.FromParts( 1, 000 );
        public bool ShouldHuntEnemyKingUnits = false;
        public bool AutoKite = false;
        public bool DistanceFromKingRestricted = true;

        public override void AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( this.Description );
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PraetorianGuardTypeData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PraetorianGuardTypeDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<PraetorianGuardTypeData> Pool = new ConcurrentPool<PraetorianGuardTypeData>( "PraetorianGuardTypeDatas", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PraetorianGuardTypeData(); } );

        public static PraetorianGuardTypeData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<PraetorianGuardTypeData> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<PraetorianGuardTypeData>( new PraetorianGuardTypeData() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class PraetorianGuardTypeDataTable : ArcenDynamicTable<PraetorianGuardTypeData>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static PraetorianGuardTypeDataTable Instance;
        public PraetorianGuardTypeDataTable() : base( "PraetorianGuardType", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        public override PraetorianGuardTypeData GetNewRowFromPool()
        {
            return PraetorianGuardTypeData.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, PraetorianGuardTypeData TypeDataObject )
        {
            Data.Fill( "abbreviation", ref TypeDataObject.Abbreviation, !Data.ReadingPartialRecord );
            Data.Fill( "description", ref TypeDataObject.Description, !Data.ReadingPartialRecord );
            //BudgetType is not read from xml

            Data.Fill( "AllowEntryIntoHostileTerritory", ref TypeDataObject.AllowEntryIntoHostileTerritory, false );
            Data.Fill( "ShouldCountAsThreat", ref TypeDataObject.ShouldCountAsThreat, false );
            Data.Fill( "HostileRatio_TooDangerousToStay", ref TypeDataObject.HostileRatio_TooDangerousToStay, !Data.ReadingPartialRecord );
            Data.Fill( "HostileRatio_TooDangerousToTarget", ref TypeDataObject.HostileRatio_TooDangerousToTarget, !Data.ReadingPartialRecord );
            Data.Fill( "HostileRatio_TooDangerousToGoThrough", ref TypeDataObject.HostileRatio_TooDangerousToGoThrough, !Data.ReadingPartialRecord );
            Data.Fill( "ShouldHuntEnemyKingUnits", ref TypeDataObject.ShouldHuntEnemyKingUnits, false );
            Data.Fill( "AutoKite", ref TypeDataObject.AutoKite, false );
            Data.Fill( "DistanceFromKingRestricted", ref TypeDataObject.DistanceFromKingRestricted, false );

            return DelReturn.Continue;
        }
    }
}
