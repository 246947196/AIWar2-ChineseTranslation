using System;

using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AIDifficulty_HunterFleet : ArcenDynamicTableRow, IConcurrentPoolable<AIDifficulty_HunterFleet>, IProtectedListable
    {
        public byte Difficulty;
        public int HunterStartingBudget;
        public int HunterBonusIncomePerMinute;
        public FInt OverconfidenceRatio;
        public bool CanUseDireGuardians;
        public int AIPForDireUnlock;
        public bool CanWaitForReinforcements;
        //Notes on how to use this. If you are adding a new anti tagged unit income then you need to add the GeneratesBonusHunterShips
        //tag to the units in question for performance reasons. We shouldn't tag too many of these for performance reasons
        public readonly List<TaggedUnitIncomeData> AntiTaggedUnitIncome = List<TaggedUnitIncomeData>.Create_WillNeverBeGCed( 500, "AIDifficulty_HunterFleet-TaggedUnitIncomeData" );

        public override void AddDescription(ArcenCharacterBufferBase Buffer)
        {
            Buffer.Add("初始预算: ").AddNumberMoreReadable(this.HunterStartingBudget);
            Buffer.Add("\n每分钟额外预算: ").AddNumberMoreReadable(this.HunterBonusIncomePerMinute);
            Buffer.Add("\n<size=75%>(所有其他收入来自哨兵或守卫的被动捐赠)</size>");
            Buffer.Add("\n过度自信比率: ").AddNumberMoreReadable(this.OverconfidenceRatio);
            Buffer.Add("\n恐怖守卫: ").Add(this.CanUseDireGuardians ? "可用" : "不可用");

            if ( this.CanUseDireGuardians ) {
                Buffer.Add(" 在 ").Add(this.AIPForDireUnlock).Add(" AIP后");
            }

            Buffer.Add("\n等待增援: ").Add(this.CanWaitForReinforcements ? "会等待" : "不会等待");
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AIDifficulty_HunterFleet() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIDifficulty_HunterFleets" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AIDifficulty_HunterFleet> Pool = new ConcurrentPool<AIDifficulty_HunterFleet>( "AIDifficulty_HunterFleets", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AIDifficulty_HunterFleet(); } );

        public static AIDifficulty_HunterFleet GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AIDifficulty_HunterFleet> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AIDifficulty_HunterFleet>( new AIDifficulty_HunterFleet() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public struct TaggedUnitIncomeData : IDumpDetailsNoMatterWhat
    {
        public string Tag;
        public FInt IncomePerInterval;
        public int IntervalInSeconds;
        public FInt LinearIncomeIncreasePer10AIP;
        public bool OnlyPlayerOwned;
        public bool PlayerOrPlayerAllyOwned;
        public bool IncomeIncreasedWithMultipleTargets;

        public TaggedUnitIncomeData( string tag, FInt income, FInt linearIncrease, int interval, bool playerowned, bool allyowned, bool increasedIncomeMultiTargets )
        {
            this.Tag = tag;
            this.IncomePerInterval = income;
            this.IntervalInSeconds = interval;
            this.LinearIncomeIncreasePer10AIP = linearIncrease;
            this.OnlyPlayerOwned = playerowned;
            this.PlayerOrPlayerAllyOwned = allyowned;
            this.IncomeIncreasedWithMultipleTargets = increasedIncomeMultiTargets;
        }
    }
    public class AIDifficulty_HunterFleetTable : ArcenDynamicTable<AIDifficulty_HunterFleet>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AIDifficulty_HunterFleetTable Instance;
        public AIDifficulty_HunterFleetTable() : base( "AIDifficulty_HunterFleet", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AIDifficulty_HunterFleet GetNewRowFromPool()
        {
            return AIDifficulty_HunterFleet.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AIDifficulty_HunterFleet TypeDataObject )
        {
            Data.Fill( "hunter_fleet_starting_budget", ref TypeDataObject.HunterStartingBudget, false );
            Data.Fill( "hunter_bonus_income_per_minute", ref TypeDataObject.HunterBonusIncomePerMinute, false );
            Data.Fill( "overconfidence_ratio", ref TypeDataObject.OverconfidenceRatio, false );
            Data.Fill( "difficulty", ref TypeDataObject.Difficulty, false );
            Data.Fill( "can_use_dire_guardians", ref TypeDataObject.CanUseDireGuardians, false );
            Data.Fill( "aip_for_dire_unlock", ref TypeDataObject.AIPForDireUnlock, false );
            Data.Fill( "can_wait_for_reinforcements", ref TypeDataObject.CanWaitForReinforcements, false );

            foreach ( ArcenXMLElement node in Data.ChildrenOfType_AndParentsAndPartials( "BonusAntiTaggedUnitIncome" ) )
            {
                try
                {
                    string tag = string.Empty;
                    FInt income = FInt.Zero;
                    int interval = 0;
                    bool playerowned = false;
                    bool allyowned = false;
                    FInt aipIncrease = FInt.Zero;
                    bool increasedIncome = false;
                    node.Fill( "tag", ref tag, true );
                    node.Fill( "income_per_interval", ref income, true );
                    node.Fill( "interval_in_seconds", ref interval, true );
                    node.Fill( "only_player_owned", ref playerowned, false );//not a required argument
                    node.Fill( "only_player_or_ally_owned", ref allyowned, false ); //not a required argument
                    node.Fill( "linear_income_increase_per_10_aip", ref aipIncrease, true );
                    node.Fill( "income_increased_with_multiple_targets", ref increasedIncome, true );
                    TypeDataObject.AntiTaggedUnitIncome.Add( new TaggedUnitIncomeData( tag, income, aipIncrease, interval, playerowned, allyowned, increasedIncome ) );

                    if ( playerowned && allyowned )
                    {
                        throw new Exception( "Invalid anti-tag income for hunter fleet. for tag " + tag + " both player only and both ally and player are set." );
                    }
                }
                catch ( Exception e )
                {
                    throw new ArcenDataReadException( "BonusAntiTaggedUnitIncome: " + node.LimitedOuterXml + Environment.NewLine + e );
                }
            }
            return DelReturn.Continue;
        }
    }
}
