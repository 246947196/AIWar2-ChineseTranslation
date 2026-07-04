using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ElderlingsDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<ElderlingsDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public string Allegiance;

        public int BaseEggSpawningInterval;
        public int BaseEggHatchingInterval;
        public int BaseStartingEggs;
        public int ExperienceRequiredToLevelUpLowTier;
        public int ExperienceRequiredToLevelUpLowTierIncreasePerLevel = 100;
        public int ExperienceRequiredToLevelUpMedTier;
        public int ExperienceRequiredToLevelUpMedTierIncreasePerLevel = 100;
        public int ExperienceRequiredToLevelUpHighTier;
        public int ExperienceRequiredToLevelUpHighTierIncreasePerLevel = 100;
        public FInt ExperienceMultiplerForChallengerPlus = FInt.One;

        public int AIPForMedTier;
        public int AIPForHighTier;
        public int MaxTerritorySizeLowTier = 4;
        public int MaxTerritorySizeMedTier = 6;
        public int MaxTerritorySizeHighTier = 8;
        public int PercentMultiSpawn;
        public int ElderlingsToTriggerSanityLoss = 3; //if there are this many elderlings then they will start taking Sanity damage due to overcrowding. This drives them mad
        public int MaxElderlingsPerPlanet = 3; //don't ever make this 1, otherwise no eggs will ever be laid
        public int BaseElderlingTerritorySize = 3; //for similar reasons to ElderlingsPerPlanet, this should probably not be smaller then 3

        public int MaddenedEggInterval = 1800; //If there are necromancers, we spawn extra maddened elderlings to serve as a source of Essence to the player
        public int StartingSanity = 1200;

        public int AdditionalFeebleElderlings = 0;
        public override string ToString()
        {
            return name + " intensity " + Intensity + " " + Allegiance;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ElderlingsDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ElderlingsDifficulty" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ElderlingsDifficulty> Pool = new ConcurrentPool<ElderlingsDifficulty>( "ElderlingsDifficulty", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ElderlingsDifficulty(); } );

        public static ElderlingsDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ElderlingsDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ElderlingsDifficulty>( new ElderlingsDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class ElderlingsDifficultyTable : ArcenDynamicTable<ElderlingsDifficulty>
    {
        public static ElderlingsDifficultyTable Instance;

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public ElderlingsDifficultyTable() : base( "ElderlingsDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override ElderlingsDifficulty GetNewRowFromPool()
        {
            return ElderlingsDifficulty.GetFromPoolOrCreate();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, ElderlingsDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "Intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "Allegiance", ref TypeDataObject.Allegiance, !Data.ReadingPartialRecord );

            Data.Fill( "BaseEggSpawningInterval", ref TypeDataObject.BaseEggSpawningInterval, !Data.ReadingPartialRecord );
            Data.Fill( "BaseEggHatchingInterval", ref TypeDataObject.BaseEggHatchingInterval, !Data.ReadingPartialRecord );
            Data.Fill( "BaseStartingEggs", ref TypeDataObject.BaseStartingEggs, !Data.ReadingPartialRecord );
            Data.Fill( "AIPForMedTier", ref TypeDataObject.AIPForMedTier, !Data.ReadingPartialRecord );
            Data.Fill( "AIPForHighTier", ref TypeDataObject.AIPForHighTier, !Data.ReadingPartialRecord );

            Data.Fill( "MaxTerritorySizeLowTier", ref TypeDataObject.MaxTerritorySizeLowTier, false );
            Data.Fill( "MaxTerritorySizeMedTier", ref TypeDataObject.MaxTerritorySizeMedTier, false );
            Data.Fill( "MaxTerritorySizeHighTier", ref TypeDataObject.MaxTerritorySizeHighTier, false );
            
            Data.Fill( "ExperienceRequiredToLevelUpLowTier", ref TypeDataObject.ExperienceRequiredToLevelUpLowTier, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceRequiredToLevelUpLowTierIncreasePerLevel", ref TypeDataObject.ExperienceRequiredToLevelUpLowTierIncreasePerLevel, false ); //not required
            Data.Fill( "ExperienceRequiredToLevelUpMedTier", ref TypeDataObject.ExperienceRequiredToLevelUpMedTier, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceRequiredToLevelUpMedTierIncreasePerLevel", ref TypeDataObject.ExperienceRequiredToLevelUpMedTierIncreasePerLevel, false ); //not required
            Data.Fill( "ExperienceRequiredToLevelUpHighTier", ref TypeDataObject.ExperienceRequiredToLevelUpHighTier, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceRequiredToLevelUpHighTierIncreasePerLevel", ref TypeDataObject.ExperienceRequiredToLevelUpHighTierIncreasePerLevel, false ); //not required
            Data.Fill( "ExperienceMultiplerForChallengerPlus", ref TypeDataObject.ExperienceMultiplerForChallengerPlus, false ); //not required

            Data.Fill( "PercentMultiSpawn", ref TypeDataObject.PercentMultiSpawn, !Data.ReadingPartialRecord );
            Data.Fill( "StartingSanity", ref TypeDataObject.StartingSanity, false );
            Data.Fill( "AdditionalFeebleElderlings", ref TypeDataObject.AdditionalFeebleElderlings, false );
            Data.Fill( "MaddenedEggInterval", ref TypeDataObject.MaddenedEggInterval, false );
            Data.Fill( "MaxElderlingsPerPlanet", ref TypeDataObject.MaxElderlingsPerPlanet, false ); //defaults to 3, but can be overridden. Don't ever make this 1
            Data.Fill( "ElderlingsToTriggerSanityLoss", ref TypeDataObject.ElderlingsToTriggerSanityLoss, false ); //defaults to 3, but can be overridden. Don't ever make this 1
            
            return DelReturn.Continue;
        }
        public ElderlingsDifficulty GetRowByIntensity( int factionIntensity, Faction faction )
        {
            if ( this.Rows.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "no rows in elderling difficulty", Verbosity.DoNotShow );
                return null;
            }
            string factionAllegiance = faction.BaseInfo.Allegiance;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionIntensity )
                {
                    if ( this.Rows[i].Allegiance == "Player" )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "Friendly To Players" ) )
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
                        if ( ArcenStrings.Equals( factionAllegiance, "Allied To AI" ) )
                            return this.Rows[i];
                    }
                }
            }
            throw new Exception( "Could not find the right row for " + faction.GetDisplayName() + " idx " + faction.FactionIndex + " factionIntensity " + factionIntensity + ", factionAllegiance '" + factionAllegiance + "'." );
        }
        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "We have " + this.Rows.Count + " elderlings difficulties", Verbosity.DoNotShow );
        }
    }
}
