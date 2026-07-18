using System;

using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AIDifficulty_PraetorianGuard : ArcenDynamicTableRow, IConcurrentPoolable<AIDifficulty_PraetorianGuard>, IProtectedListable
    {
        public int PraetorianStartingBudget;
        public int PraetorianStrengthCap_Base;
        public int PraetorianStrengthCap_PerAIP;
        public int PraetorianStrengthCap_PerAIP_Above200;
        public int PraetorianStrengthCap_PerAIP_Above400;
        public int PraetorianStrengthCap_PerAIP_Above600;
        public Int16 NeverCampCloserThanXHopsToHostileTerritory;
        public FInt OverconfidenceRatio;
        public byte Difficulty;
        public bool CanUseTopTierUnits;
        public int AIPForTopTierUnlock;
        public bool CapScalesWithMarkLevel;

        #region GetStrengthCapAtAIP
        public int GetStrengthCapAtAIP( int AIP )
        {
            return this.PraetorianStrengthCap_Base +
                (PraetorianStrengthCap_PerAIP * AIP) +
                Math.Max( 0, PraetorianStrengthCap_PerAIP_Above200 * (AIP - 200) ) +
                Math.Max( 0, PraetorianStrengthCap_PerAIP_Above400 * (AIP - 400) ) +
                Math.Max( 0, PraetorianStrengthCap_PerAIP_Above600 * (AIP - 600) );
        }
        #endregion

        public override void AddDescription(ArcenCharacterBufferBase Buffer)
        {
            Buffer.Add("初始预算: ").AddNumberMoreReadable(this.PraetorianStartingBudget);
            Buffer.Add("\n100 AIP 时强度上限: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 100 ), true);
            Buffer.Add("\n300 AIP 时强度上限: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 300 ), true);
            Buffer.Add("\n700 AIP 时强度上限: ").AddStrength_MoreReadable(this.GetStrengthCapAtAIP( 700 ), true);
            Buffer.Add("\n<size=75%>(收入基于主AI类型和主AI难度等级)</size>");
            Buffer.Add("\n过度自信比率: ").AddNumberMoreReadable(this.OverconfidenceRatio);
            Buffer.Add("\n顶级单位: ").Add(this.CanUseTopTierUnits ? "可用" : "不可用");

            if ( this.CanUseTopTierUnits ) {
                Buffer.Add(" 在 ").Add(this.AIPForTopTierUnlock).Add(" AIP后");
            }

            Buffer.Add("\n从不扎营近于 ").Add(this.NeverCampCloserThanXHopsToHostileTerritory).Add(" 跳至敌对领土");
            Buffer.Add("\n<size=75%>额外标记等级基于主AI难度</size>");
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AIDifficulty_PraetorianGuard() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIDifficulty_PraetorianGuards" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AIDifficulty_PraetorianGuard> Pool = new ConcurrentPool<AIDifficulty_PraetorianGuard>( "AIDifficulty_PraetorianGuards", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AIDifficulty_PraetorianGuard(); } );

        public static AIDifficulty_PraetorianGuard GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AIDifficulty_PraetorianGuard> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AIDifficulty_PraetorianGuard>( new AIDifficulty_PraetorianGuard() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class AIDifficulty_PraetorianGuardTable : ArcenDynamicTable<AIDifficulty_PraetorianGuard>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AIDifficulty_PraetorianGuardTable Instance;
        public AIDifficulty_PraetorianGuardTable() : base( "AIDifficulty_PraetorianGuard", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AIDifficulty_PraetorianGuard GetNewRowFromPool()
        {
            return AIDifficulty_PraetorianGuard.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AIDifficulty_PraetorianGuard TypeDataObject )
        {
            Data.Fill( "praetorian_guard_starting_budget", ref TypeDataObject.PraetorianStartingBudget, !Data.ReadingPartialRecord );
            Data.Fill( "praetorian_guard_base_strength_cap", ref TypeDataObject.PraetorianStrengthCap_Base, false );
            Data.Fill( "praetorian_guard_strength_cap_per_aip", ref TypeDataObject.PraetorianStrengthCap_PerAIP, false );
            Data.Fill( "praetorian_guard_strength_cap_per_aip_above_200", ref TypeDataObject.PraetorianStrengthCap_PerAIP_Above200, false );
            Data.Fill( "praetorian_guard_strength_cap_per_aip_above_400", ref TypeDataObject.PraetorianStrengthCap_PerAIP_Above400, false );
            Data.Fill( "praetorian_guard_strength_cap_per_aip_above_600", ref TypeDataObject.PraetorianStrengthCap_PerAIP_Above600, false );
            Data.Fill( "praetorian_guard_never_camp_closer_than_x_hops_to_hostile_territory", ref TypeDataObject.NeverCampCloserThanXHopsToHostileTerritory, false );
            Data.Fill( "overconfidence_ratio", ref TypeDataObject.OverconfidenceRatio, false );
            Data.Fill( "difficulty", ref TypeDataObject.Difficulty, false );
            Data.Fill( "can_use_top_tier_units", ref TypeDataObject.CanUseTopTierUnits, false );
            Data.Fill( "aip_for_top_tier_unlock", ref TypeDataObject.AIPForTopTierUnlock, false );
            Data.Fill( "cap_scales_with_mark_level", ref TypeDataObject.CapScalesWithMarkLevel, false );
            return DelReturn.Continue;
        }
    }
}
