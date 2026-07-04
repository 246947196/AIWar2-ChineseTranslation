using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HunterFleetType : ArcenDynamicTableRow, IConcurrentPoolable<HunterFleetType>, IProtectedListable
    {
        public string Abbreviation = string.Empty;
        public string Description = string.Empty;
        public readonly AIBudgetType BudgetType = AIBudgetType.HunterFleet;

        public bool AllowEntryIntoHostileTerritory = true;
        public bool ShouldCountAsThreat = true;
        public FInt HostileRatio_TooDangerousToStay = FInt.FromParts( 2, 000 );
        public FInt HostileRatio_TooDangerousToTarget = FInt.FromParts( 0, 750 );
        public FInt HostileRatio_TooDangerousToGoThrough = FInt.FromParts( 0, 600 );
        public FInt MyStrengthMultiplier = FInt.FromParts( 1, 000 );
        public FInt EnemyStrengthMultiplier = FInt.FromParts( 1, 500 );
        public bool ShouldHuntEnemyKingUnits = true;
        public bool AutoKite = false;
        public bool UseFireteams = true;
        public int NeverCampCloserThanXHopsToHostileTerritory = 0;

        public override void AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( this.Description );
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private HunterFleetType() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "HunterFleetTypes" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<HunterFleetType> Pool = new ConcurrentPool<HunterFleetType>( "HunterFleetTypes", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new HunterFleetType(); } );

        public static HunterFleetType GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<HunterFleetType> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<HunterFleetType>( new HunterFleetType() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class HunterFleetTypeTable : ArcenDynamicTable<HunterFleetType>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static HunterFleetTypeTable Instance;
        public HunterFleetTypeTable() : base( "HunterFleetType", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        public override HunterFleetType GetNewRowFromPool()
        {
            return HunterFleetType.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, HunterFleetType TypeDataObject )
        {
            Data.Fill( "abbreviation", ref TypeDataObject.Abbreviation, !Data.ReadingPartialRecord );
            Data.Fill( "description", ref TypeDataObject.Description, !Data.ReadingPartialRecord );
            //BudgetType is not read from xml

            Data.Fill( "AllowEntryIntoHostileTerritory", ref TypeDataObject.AllowEntryIntoHostileTerritory, false );
            Data.Fill( "ShouldCountAsThreat", ref TypeDataObject.ShouldCountAsThreat, false );
            Data.Fill( "HostileRatio_TooDangerousToStay", ref TypeDataObject.HostileRatio_TooDangerousToStay, !Data.ReadingPartialRecord );
            Data.Fill( "HostileRatio_TooDangerousToTarget", ref TypeDataObject.HostileRatio_TooDangerousToTarget, !Data.ReadingPartialRecord );
            Data.Fill( "HostileRatio_TooDangerousToGoThrough", ref TypeDataObject.HostileRatio_TooDangerousToGoThrough, !Data.ReadingPartialRecord );
            Data.Fill( "MyStrengthMultiplier", ref TypeDataObject.MyStrengthMultiplier, !Data.ReadingPartialRecord );
            Data.Fill( "EnemyStrengthMultiplier", ref TypeDataObject.EnemyStrengthMultiplier, !Data.ReadingPartialRecord );
            Data.Fill( "ShouldHuntEnemyKingUnits", ref TypeDataObject.ShouldHuntEnemyKingUnits, false );
            Data.Fill( "AutoKite", ref TypeDataObject.AutoKite, false );
            Data.Fill( "UseFireteams", ref TypeDataObject.UseFireteams, false );
            Data.Fill( "NeverCampCloserThanXHopsToHostileTerritory", ref TypeDataObject.NeverCampCloserThanXHopsToHostileTerritory, false );

            return DelReturn.Continue;
        }
    }
}
