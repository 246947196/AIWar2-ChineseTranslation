using System;

using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AIDifficulty_WardenFleet : ArcenDynamicTableRow, IConcurrentPoolable<AIDifficulty_WardenFleet>, IProtectedListable
    {
        public int WardenStartingBudget;
        public int WardenStrengthCap_Base;
        public int WardenStrengthCap_PerAIP;
        public int WardenStrengthCap_PerAIP_Above200;
        public int WardenStrengthCap_PerAIP_Above400;
        public int WardenStrengthCap_PerAIP_Above600;
        public Int16 NeverCampCloserThanXHopsToHostileTerritory;
        public FInt OverconfidenceRatio;
        public byte Difficulty;
        public bool CanUseTopTierUnits;
        public int AIPForTopTierUnlock;
        public bool CanWaitForReinforcements;
        public bool CapScalesWithMarkLevel;


        #region GetStrengthCapAtAIP
        public int GetStrengthCapAtAIP( int AIP )
        {
            return this.WardenStrengthCap_Base +
                (WardenStrengthCap_PerAIP * AIP) +
                Math.Max( 0, WardenStrengthCap_PerAIP_Above200 * (AIP - 200) ) +
                Math.Max( 0, WardenStrengthCap_PerAIP_Above400 * (AIP - 400) ) +
                Math.Max( 0, WardenStrengthCap_PerAIP_Above600 * (AIP - 600) );
        }
        #endregion

        public override void AddDescription(ArcenCharacterBufferBase Buffer)
        {
            Buffer.Add("Starting Budget: ").AddNumberMoreReadable(this.WardenStartingBudget);
            Buffer.Add("\nStrength Cap At 100 AIP: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 100 ), true);
            Buffer.Add("\nStrength Cap At 300 AIP: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 300 ), true);
            Buffer.Add("\nStrength Cap At 700 AIP: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 700 ), true);
            Buffer.Add("\n<size=75%>(Income Is Based Off Main AI Type And Main AI Difficulty Level)</size>");
            Buffer.Add("\nOverconfidence Ratio: ").AddNumberMoreReadable(this.OverconfidenceRatio);
            Buffer.Add("\nTop-Tier Units: ").Add(this.CanUseTopTierUnits ? "Available" : "Unavailable");

            if ( this.CanUseTopTierUnits ) {
                Buffer.Add(" After ").Add(this.AIPForTopTierUnlock).Add(" AIP");
            }

            Buffer.Add("\nWaits For Reinforcements: ").Add(this.CanWaitForReinforcements ? "They Can" : "They Cannot");

            Buffer.Add("\nNever Camp Closer Than ").Add(this.NeverCampCloserThanXHopsToHostileTerritory).Add(" Hops To Hostile Territory");
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AIDifficulty_WardenFleet() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIDifficulty_WardenFleets" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AIDifficulty_WardenFleet> Pool = new ConcurrentPool<AIDifficulty_WardenFleet>( "AIDifficulty_WardenFleets", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AIDifficulty_WardenFleet(); } );

        public static AIDifficulty_WardenFleet GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AIDifficulty_WardenFleet> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AIDifficulty_WardenFleet>( new AIDifficulty_WardenFleet() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class AIDifficulty_WardenFleetTable : ArcenDynamicTable<AIDifficulty_WardenFleet>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AIDifficulty_WardenFleetTable Instance;
        public AIDifficulty_WardenFleetTable() : base( "AIDifficulty_WardenFleet", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AIDifficulty_WardenFleet GetNewRowFromPool()
        {
            return AIDifficulty_WardenFleet.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AIDifficulty_WardenFleet TypeDataObject )
        {
            Data.Fill( "warden_fleet_starting_budget", ref TypeDataObject.WardenStartingBudget, !Data.ReadingPartialRecord );
            Data.Fill( "warden_base_strength_cap", ref TypeDataObject.WardenStrengthCap_Base, false );
            Data.Fill( "warden_strength_cap_per_aip", ref TypeDataObject.WardenStrengthCap_PerAIP, false );
            Data.Fill( "warden_strength_cap_per_aip_above_200", ref TypeDataObject.WardenStrengthCap_PerAIP_Above200, false );
            Data.Fill( "warden_strength_cap_per_aip_above_400", ref TypeDataObject.WardenStrengthCap_PerAIP_Above400, false );
            Data.Fill( "warden_strength_cap_per_aip_above_600", ref TypeDataObject.WardenStrengthCap_PerAIP_Above600, false );
            Data.Fill( "warden_fleet_never_camp_closer_than_x_hops_to_hostile_territory", ref TypeDataObject.NeverCampCloserThanXHopsToHostileTerritory, false );
            Data.Fill( "overconfidence_ratio", ref TypeDataObject.OverconfidenceRatio, false );
            Data.Fill( "difficulty", ref TypeDataObject.Difficulty, false );
            Data.Fill( "can_use_top_tier_units", ref TypeDataObject.CanUseTopTierUnits, false );
            Data.Fill( "aip_for_top_tier_unlock", ref TypeDataObject.AIPForTopTierUnlock, false );
            Data.Fill( "can_wait_for_reinforcements", ref TypeDataObject.CanWaitForReinforcements, false );
            Data.Fill( "cap_scales_with_mark_level", ref TypeDataObject.CapScalesWithMarkLevel, false );
            return DelReturn.Continue;
        }
    }
}
