using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TemplarDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<TemplarDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public string Allegiance;
        public bool ShieldMode;

        public int WaveBaseStrength;
        public int CastleSpawnInterval;
        public int MarkupIntervalLowTier;
        public int MarkupIntervalMidTier;
        public int MarkupIntervalHighTier;
        public int CastleIncome;
        public int CastleIncomeIncreasePer50AIP;

        public int BaseStrengthLowTier;
        public int BaseStrengthMidTier;
        public int BaseStrengthHighTier;

        public int CastleConstructionRange = 3;

        public int AIPToStartUsingEliteStructures; //elite units give fewer (or no) resources
        public int AIPToStartUsingEliteStrikecraft; //elite units give fewer (or no) resources
        public int AIPToStartUsingEliteGuardians; //elite units give fewer (or no) resources
        public int AIPToStartUsingEliteWaveLeaders = 150; //elite units give fewer (or no) resources

        //WaveLeaderAIPModifiers; higher tiers ==> higher chances of high-tier wave leaders
        public int AIPForWaveLeaderTierOne = 60;
        public int AIPForWaveLeaderTierTwo = 100;
        public int AIPForWaveLeaderTierThree = 140;
        public int AIPForWaveLeaderTierFour = 180;

        public int MinUnitMarkLevelLowTier = 1;
        public int MaxUnitMarkLevelLowTier = 3;
        public int MinUnitMarkLevelMidTier = 3;
        public int MaxUnitMarkLevelMidTier = 5;
        public int MinUnitMarkLevelHighTier = 3;
        public int MaxUnitMarkLevelHighTier = 7;

        public int AIPPerBaseWaveLeader = 40; //every 40 AIP we have a higher chance of more wave leaders (the number is randomized

        public int HackingPointsSpentPerBonusWaveLeaderFromHack = 100; //every 40 AIP we have a higher chance of more wave leaders (the number is randomized

        public int MinTimeBeforeRebuildingCastle = 1200; //The templar need to let a planet sit for a while before they can rebuild a castle
        public int BaseWaveInterval;
        public short LowCastleRange;
        public short MidCastleRange;
        public short HighCastleRange;

        public bool NoCastles;
        public bool WeakSovereign;
        public int InitialEncampments = 1; //allows for more of the weak/high-income encampments; for lower intensity Templar
        public override string ToString()
        {
            return name + " intensity " + Intensity + " " + Allegiance + " shield mode " + ShieldMode;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private TemplarDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "TemplarDifficulty" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<TemplarDifficulty> Pool = new ConcurrentPool<TemplarDifficulty>( "TemplarDifficulty", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TemplarDifficulty(); } );

        public static TemplarDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<TemplarDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<TemplarDifficulty>( new TemplarDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class TemplarDifficultyTable : ArcenDynamicTable<TemplarDifficulty>
    {
        public static TemplarDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public TemplarDifficultyTable() : base( "TemplarDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override TemplarDifficulty GetNewRowFromPool()
        {
            return TemplarDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, TemplarDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "Allegiance", ref TypeDataObject.Allegiance, !Data.ReadingPartialRecord );
            Data.Fill( "ShieldMode", ref TypeDataObject.ShieldMode, false );

            Data.Fill( "WaveBaseStrength", ref TypeDataObject.WaveBaseStrength, !Data.ReadingPartialRecord );
            Data.Fill( "CastleSpawnInterval", ref TypeDataObject.CastleSpawnInterval, !Data.ReadingPartialRecord );
            Data.Fill( "MarkupIntervalEncampment", ref TypeDataObject.MarkupIntervalLowTier, !Data.ReadingPartialRecord );
            Data.Fill( "MarkupIntervalFastness", ref TypeDataObject.MarkupIntervalMidTier, !Data.ReadingPartialRecord );
            Data.Fill( "MarkupIntervalCastle", ref TypeDataObject.MarkupIntervalHighTier, !Data.ReadingPartialRecord );
            Data.Fill( "DefensiveStructureIncomePerSecond", ref TypeDataObject.CastleIncome, !Data.ReadingPartialRecord );
            Data.Fill( "CastleIncomeIncreasePer50AIP", ref TypeDataObject.CastleIncomeIncreasePer50AIP, !Data.ReadingPartialRecord );

            Data.Fill( "DefenderStrengthEncampment", ref TypeDataObject.BaseStrengthLowTier, !Data.ReadingPartialRecord );
            Data.Fill( "DefenderStrengthFastness", ref TypeDataObject.BaseStrengthMidTier, !Data.ReadingPartialRecord );
            Data.Fill( "DefenderStrengthCastle", ref TypeDataObject.BaseStrengthHighTier, !Data.ReadingPartialRecord );

            Data.Fill( "AIPToStartUsingEliteStructures", ref TypeDataObject.AIPToStartUsingEliteStructures, !Data.ReadingPartialRecord );
            Data.Fill( "AIPToStartUsingEliteStrikecraft", ref TypeDataObject.AIPToStartUsingEliteStrikecraft, !Data.ReadingPartialRecord );
            Data.Fill( "AIPToStartUsingEliteGuardians", ref TypeDataObject.AIPToStartUsingEliteGuardians, !Data.ReadingPartialRecord );
            Data.Fill( "AIPToStartUsingEliteWaveLeaders", ref TypeDataObject.AIPToStartUsingEliteWaveLeaders, false );

            Data.Fill("AIPForWaveLeaderTierOne", ref TypeDataObject.AIPForWaveLeaderTierOne, false );
            Data.Fill("AIPForWaveLeaderTierTwo", ref TypeDataObject.AIPForWaveLeaderTierTwo, false );
            Data.Fill("AIPForWaveLeaderTierThree", ref TypeDataObject.AIPForWaveLeaderTierThree, false );
            Data.Fill("AIPForWaveLeaderTierFour", ref TypeDataObject.AIPForWaveLeaderTierFour, false );
            Data.Fill( "AIPPerBaseWaveLeader", ref TypeDataObject.AIPPerBaseWaveLeader, !Data.ReadingPartialRecord );
            Data.Fill( "HackingPointsSpentPerBonusWaveLeaderFromHack", ref TypeDataObject.HackingPointsSpentPerBonusWaveLeaderFromHack, false );

            Data.Fill( "BaseWaveInterval", ref TypeDataObject.BaseWaveInterval, !Data.ReadingPartialRecord );
            Data.Fill( "LowCastleRange", ref TypeDataObject.LowCastleRange, !Data.ReadingPartialRecord );
            Data.Fill( "MidCastleRange", ref TypeDataObject.MidCastleRange, !Data.ReadingPartialRecord );
            Data.Fill( "HighCastleRange", ref TypeDataObject.HighCastleRange, !Data.ReadingPartialRecord );

            Data.Fill( "NoCastles", ref TypeDataObject.NoCastles, !Data.ReadingPartialRecord );
            Data.Fill( "WeakSovereign", ref TypeDataObject.WeakSovereign, !Data.ReadingPartialRecord );

            Data.Fill( "InitialEncampments", ref TypeDataObject.InitialEncampments, false );

            Data.Fill( "MinUnitMarkLevelLowTier", ref TypeDataObject.MinUnitMarkLevelLowTier, false );
            Data.Fill( "MaxUnitMarkLevelLowTier", ref TypeDataObject.MinUnitMarkLevelLowTier, false );
            Data.Fill( "MinUnitMarkLevelMidTier", ref TypeDataObject.MinUnitMarkLevelMidTier, false );
            Data.Fill( "MaxUnitMarkLevelMidTier", ref TypeDataObject.MinUnitMarkLevelMidTier, false );
            Data.Fill( "MinUnitMarkLevelHighTier", ref TypeDataObject.MinUnitMarkLevelHighTier, false );
            Data.Fill( "MaxUnitMarkLevelHighTier", ref TypeDataObject.MinUnitMarkLevelHighTier, false );

            return DelReturn.Continue;
        }
        public TemplarDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in elderling difficulty", Verbosity.DoNotShow );
                return null;
            }
            string factionAllegiance = faction.BaseInfo.Allegiance;
            bool shieldMode = World_AIW2.Instance.Setup.GetBoolBySetting("ShieldTemplar");
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionIntensity )
                {
                    if ( this.Rows[i].Allegiance == "Player" )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "对玩家友好" ) )
                        {
                            return this.Rows[i];
                        }
                    }
                    else if ( this.Rows[i].Allegiance == "MinorFaction" )
                    {
                        if ( StringExtensions.Contains( factionAllegiance, "MinorFaction", StringComparison.CurrentCultureIgnoreCase ) ||
                            StringExtensions.Contains( factionAllegiance, "Minor Faction", StringComparison.CurrentCultureIgnoreCase ) )
                            return this.Rows[i];
                    }
                    else if ( this.Rows[i].Allegiance == "AI" )
                    {
                        if (ArcenStrings.Equals(factionAllegiance, "对AI友好"))
                        {
                            if ( this.Rows[i].ShieldMode == shieldMode)
                                return this.Rows[i];
                        }
                    }
                }
            }
            throw new Exception( "Could not find the right row for " + faction.GetDisplayName() + " factionIntensity " + factionIntensity + ", factionAllegiance '" + factionAllegiance + "' shieldMode " + shieldMode +"." );
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "We have " + this.Rows.Count + " templar difficulties", Verbosity.DoNotShow );
        }
    }
}
