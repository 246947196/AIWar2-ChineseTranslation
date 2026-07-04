using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    /* These classes are for the XML data. We read the data from the XML, put it in this table,
    then look at it in the Scenario where we figure out how much energy to give the Dark Spire */
    public class DarkSpireHarvestData : ArcenDynamicTableRow, IConcurrentPoolable<DarkSpireHarvestData>, IProtectedListable
    {
        public string FactionName;
        public FInt RatioWhenKilling;
        public FInt RatioWhenDying;
        public FInt RatioForStructuresDying = FInt.One;

        #region Pooling
        private static ReferenceTracker RefTracker;
        private DarkSpireHarvestData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "DarkSpireHarvestDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<DarkSpireHarvestData> Pool = new ConcurrentPool<DarkSpireHarvestData>( "DarkSpireHarvestDatas", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DarkSpireHarvestData(); } );

        public static DarkSpireHarvestData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<DarkSpireHarvestData> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<DarkSpireHarvestData>( new DarkSpireHarvestData() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class DarkSpireHarvestDataTable : ArcenDynamicTable<DarkSpireHarvestData>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static DarkSpireHarvestDataTable Instance;
        public readonly Dictionary<string, DarkSpireHarvestData> RowsByFaction = Dictionary<string, DarkSpireHarvestData>.Create_WillNeverBeGCed( 200, "DarkSpireHarvestDataTable-RowsByFaction" );

        public DarkSpireHarvestDataTable() : base( "DarkSpire_HarvestRatio", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow )
        {
            Instance = this;
        }

        public override DarkSpireHarvestData GetNewRowFromPool()
        {
            return DarkSpireHarvestData.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override void DoPreInitializationLogic() //Look at the Astro Trains for a fancier example
        { }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            bool debug = false;
            RowsByFaction.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                DarkSpireHarvestData row = this.Rows[i];
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( i + ": " + row.FactionName, Verbosity.DoNotShow );
                RowsByFaction[row.FactionName] = row;
            }
        }

        public DarkSpireHarvestData GetRowByFaction( Faction faction )
        {
            bool debug = false;
            //picks a random row allowed for this intensity
            DarkSpireHarvestData row = null;
            string factionName = "Default";
            if ( faction != null )
                factionName = faction.SpecialFactionData.InternalName;
            row = RowsByFaction[factionName];
            if ( row == null )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GetRowByFaction: no definition for " + factionName + " so use the default", Verbosity.DoNotShow );
                row = RowsByFaction["Default"];
                if ( row == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no Default harvest ratio defined in the XML", Verbosity.DoNotShow );
                    return null;
                }
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "We requested the row for " + factionName + " and got the row for " + row.FactionName + " has ratios " + row.RatioWhenDying + " and " + row.RatioWhenKilling, Verbosity.DoNotShow );
            return row;
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, DarkSpireHarvestData TypeDataObject )
        {
            bool debug = false;
            //Each entry is added to the XML, then code parsing that XML entry goes here,
            //and the variable the entry is stored in goes into the AstroTrainBehaviorType object

            //passing in "false" means that the field can be empty or 0. Sometimes we will want some fields to be 0
            Data.Fill( "faction_name", ref TypeDataObject.FactionName, !Data.ReadingPartialRecord );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Parsed harvest ratio " + TypeDataObject.FactionName, Verbosity.DoNotShow );
            Data.Fill( "harvest_ratio_when_dying", ref TypeDataObject.RatioWhenDying, false );
            Data.Fill( "harvest_ratio_when_killing", ref TypeDataObject.RatioWhenKilling, !Data.ReadingPartialRecord );
            Data.Fill( "harvest_ratio_for_structures_dying", ref TypeDataObject.RatioForStructuresDying, false );
            return DelReturn.Continue;
        }
    }
}
