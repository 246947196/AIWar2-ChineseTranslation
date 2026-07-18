using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgeDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<ScourgeDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public bool PlayerAllied;
        public bool AIAllied;
        public bool MinorFactionAllied;
        public byte MaxMarkPlanetForBuilding; //can be 0 if we don't care (this is just for AI-allied scourge)
        //experience stuff
        public FInt ExperiencePerSecond;
        public FInt BaseExperienceForLevelupSpawners;
        public FInt ExperienceForLevelupSpawnersPerMark;
        public FInt BaseExperienceForLevelupArmories;
        public FInt ExperienceForLevelupArmoriesPerMark;
        public FInt BaseExperienceForLevelupFortress; //only high enough intensity scourge build fortresses
        public FInt ExperienceForLevelupFortressPerMark;
        public FInt BaseExperienceForLevelupWarriors;
        public FInt ExperienceForLevelupWarriorsPerMark;
        public FInt BaseExperienceForLevelupBuilders;
        public FInt ExperienceForLevelupBuildersPerMark;
        public FInt NeophyteExperienceRequired;
        //metal income stuff
        public FInt BaseSpawnerMetalIncome;
        public FInt SpawnerMetalIncomeIncreasePerMark;
        public FInt BaseBuilderMetalIncome;
        public FInt BuilderMetalIncomeIncreasePerMark;

        //The AI-allied scourges get bonus scaling based on player science earned
        public int UnitForScienceScaling;
        public FInt AllowedBuildersIncreasePerScienceUnit; //we want everything to scale this way
        public FInt BuilderIncomeIncreasePerScienceUnit; //this should be 0 for non-ai allied scourge
        public FInt SpawnerIncomeIncreasePerScienceUnit; //this should be 0 for non-ai allied scourge
        public int ScienceRequiredForAllBuildingsToSpawnMark2; //should be -1 to disable
        public int ScienceRequiredForAllBuildingsToSpawnMark3; //should be -1 to disable

        //costs
        public int MetalCostForBuildingArmory;
        public int MetalCostForBuildingSpawner;
        public int MetalCostForBuildingFortress;
        public int MetalCostIncreaseForUpgradingFortressPerLevel;
        public int MetalCostIncreaseForUpgradingArmoriesPerLevel;
        public int MetalCostIncreaseForUpgradingSpawnersPerLevel;
        public FInt NeophyteCost;

        public int BaseNumberOfBuildersAllowed;

        //nemesis/subjugator stuff
        public Int16 TimeForNemesisSpawning;
        public int TopTierSpawnersRequiredPerSubjugator;
        public int RequiredLevelForSubjugatorToBecomeNemesis;

        //required levels for certain types of upgrades
        public int RequiredLevelForBaseWarriorsToEvolve;
        public int RequiredLevelForWarriorsToHybridize;
        public int RequiredLevelForArmoriesToEvolveWarriors;
        public int RequiredLevelForArmoriesToHybridizeWarriors;

        public Int16 PercentForceWarriorToHybridizeIfPossible;

        public Int16 FlowerGrowTime = 630;
        public Int16 SeedInterval = 480;
        public Int16 SeedVariance = 180;

        //fireteam calculation stuff
        public override string ToString()
        {
            string output = "ScourgeDifficulty " + name + ", Intensity" + Intensity + ", exp per second " + ExperiencePerSecond;
            if ( PlayerAllied )
                output += " player allied ";
            else if ( MinorFactionAllied )
                output += " minor faction allied ";
            else if ( AIAllied )
                output += " ai allied ";
            else
                output += " NO ALLEGIANCE ";
            return output;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ScourgeDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ScourgeDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<ScourgeDifficulty> Pool = new ConcurrentPool<ScourgeDifficulty>( "ScourgeDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ScourgeDifficulty(); } );

        public static ScourgeDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<ScourgeDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<ScourgeDifficulty>( new ScourgeDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class ScourgeDifficultyTable : ArcenDynamicTable<ScourgeDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static ScourgeDifficultyTable Instance;
        public ScourgeDifficultyTable() : base( "ScourgeDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override ScourgeDifficulty GetNewRowFromPool()
        {
            return ScourgeDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, ScourgeDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            string allegianceTemp = string.Empty;
            Data.Fill( "allegiance", ref allegianceTemp, !Data.ReadingPartialRecord );
            if ( ArcenStrings.Equals( allegianceTemp, "Player" ) )
                TypeDataObject.PlayerAllied = true;
            else if ( ArcenStrings.Equals( allegianceTemp, "AI" ) ||
                      ArcenStrings.Equals( allegianceTemp, "内战" ))
                TypeDataObject.AIAllied = true;
            else if ( ArcenStrings.Equals( allegianceTemp, "MinorFaction" ) )
                TypeDataObject.MinorFactionAllied = true;
            else
                throw new Exception( "Unknown allegiance for scourge difficulty " + TypeDataObject.name );

            Data.Fill( "MaxMarkPlanetForBuilding", ref TypeDataObject.MaxMarkPlanetForBuilding, false ); //reading in 0 is fine, and means we don't care!
            //experience
            Data.Fill( "ExperiencePerSecond", ref TypeDataObject.ExperiencePerSecond, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExperienceForLevelupSpawners", ref TypeDataObject.BaseExperienceForLevelupSpawners, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceForLevelupSpawnersPerMark", ref TypeDataObject.ExperienceForLevelupSpawnersPerMark, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExperienceForLevelupArmories", ref TypeDataObject.BaseExperienceForLevelupArmories, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceForLevelupArmoriesPerMark", ref TypeDataObject.ExperienceForLevelupArmoriesPerMark, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExperienceForLevelupFortress", ref TypeDataObject.BaseExperienceForLevelupFortress, false );
            Data.Fill( "ExperienceForLevelupFortressPerMark", ref TypeDataObject.ExperienceForLevelupFortressPerMark, false );
            Data.Fill( "BaseExperienceForLevelupWarriors", ref TypeDataObject.BaseExperienceForLevelupWarriors, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceForLevelupWarriorsPerMark", ref TypeDataObject.ExperienceForLevelupWarriorsPerMark, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExperienceForLevelupBuilders", ref TypeDataObject.BaseExperienceForLevelupBuilders, !Data.ReadingPartialRecord );
            Data.Fill( "ExperienceForLevelupBuildersPerMark", ref TypeDataObject.ExperienceForLevelupBuildersPerMark, !Data.ReadingPartialRecord );
            Data.Fill( "NeophyteExperienceRequired", ref TypeDataObject.NeophyteExperienceRequired, !Data.ReadingPartialRecord );

            //metal income
            Data.Fill( "BaseSpawnerMetalIncome", ref TypeDataObject.BaseSpawnerMetalIncome, !Data.ReadingPartialRecord );
            Data.Fill( "SpawnerMetalIncomeIncreasePerMark", ref TypeDataObject.SpawnerMetalIncomeIncreasePerMark, !Data.ReadingPartialRecord );
            Data.Fill( "BaseBuilderMetalIncome", ref TypeDataObject.BaseBuilderMetalIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BuilderMetalIncomeIncreasePerMark", ref TypeDataObject.BuilderMetalIncomeIncreasePerMark, !Data.ReadingPartialRecord );

            //science scaling
            Data.Fill( "UnitForScienceScaling", ref TypeDataObject.UnitForScienceScaling, !Data.ReadingPartialRecord );
            Data.Fill( "AllowedBuildersIncreasePerScienceUnit", ref TypeDataObject.AllowedBuildersIncreasePerScienceUnit, !Data.ReadingPartialRecord );
            Data.Fill( "SpawnerIncomeIncreasePerScienceUnit", ref TypeDataObject.SpawnerIncomeIncreasePerScienceUnit, false ); //optional
            Data.Fill( "BuilderIncomeIncreasePerScienceUnit", ref TypeDataObject.BuilderIncomeIncreasePerScienceUnit, false ); //optional
            Data.Fill( "ScienceRequiredForAllBuildingsToSpawnMark2", ref TypeDataObject.ScienceRequiredForAllBuildingsToSpawnMark2, !Data.ReadingPartialRecord );
            Data.Fill( "ScienceRequiredForAllBuildingsToSpawnMark3", ref TypeDataObject.ScienceRequiredForAllBuildingsToSpawnMark3, !Data.ReadingPartialRecord );

            //costs
            Data.Fill( "MetalCostForBuildingArmory", ref TypeDataObject.MetalCostForBuildingArmory, !Data.ReadingPartialRecord );
            Data.Fill( "MetalCostForBuildingSpawner", ref TypeDataObject.MetalCostForBuildingSpawner, !Data.ReadingPartialRecord );
            Data.Fill( "MetalCostForBuildingFortress", ref TypeDataObject.MetalCostForBuildingArmory, false );
            Data.Fill( "MetalCostIncreaseForUpgradingSpawnersPerLevel", ref TypeDataObject.MetalCostIncreaseForUpgradingSpawnersPerLevel, !Data.ReadingPartialRecord );
            Data.Fill( "MetalCostIncreaseForUpgradingArmoriesPerLevel", ref TypeDataObject.MetalCostIncreaseForUpgradingArmoriesPerLevel, !Data.ReadingPartialRecord );
            Data.Fill( "MetalCostIncreaseForUpgradingFortressPerLevel", ref TypeDataObject.MetalCostIncreaseForUpgradingArmoriesPerLevel, false );
            Data.Fill( "NeophyteCost", ref TypeDataObject.NeophyteCost, !Data.ReadingPartialRecord );

            Data.Fill( "BaseNumberOfBuildersAllowed", ref TypeDataObject.BaseNumberOfBuildersAllowed, !Data.ReadingPartialRecord );

            //Nemesis stuff
            Data.Fill( "TimeForNemesisSpawning", ref TypeDataObject.TimeForNemesisSpawning, !Data.ReadingPartialRecord );
            Data.Fill( "TopTierSpawnersRequiredPerSubjugator", ref TypeDataObject.TopTierSpawnersRequiredPerSubjugator, !Data.ReadingPartialRecord );
            Data.Fill( "MarkLevelAtWhichSubjugatorCanBecomeNemesis", ref TypeDataObject.RequiredLevelForSubjugatorToBecomeNemesis, !Data.ReadingPartialRecord );

            //required levels for upgrades
            Data.Fill( "RequiredLevelForBaseWarriorsToEvolve", ref TypeDataObject.RequiredLevelForBaseWarriorsToEvolve, !Data.ReadingPartialRecord );
            Data.Fill( "RequiredLevelForWarriorsToHybridize", ref TypeDataObject.RequiredLevelForWarriorsToHybridize, !Data.ReadingPartialRecord );
            Data.Fill( "RequiredLevelForArmoriesToEvolveWarriors", ref TypeDataObject.RequiredLevelForArmoriesToEvolveWarriors, !Data.ReadingPartialRecord );
            Data.Fill( "RequiredLevelForArmoriesToHybridizeWarriors", ref TypeDataObject.RequiredLevelForArmoriesToHybridizeWarriors, !Data.ReadingPartialRecord );

            Data.Fill( "PercentForceWarriorToHybridizeIfPossible", ref TypeDataObject.PercentForceWarriorToHybridizeIfPossible, false ); //can be 0
            if ( TypeDataObject.Intensity < 0 || TypeDataObject.Intensity > 10 )
                throw new Exception( "Unknown intensity for scourge difficulty " + TypeDataObject.name + ", intensity " + TypeDataObject.Intensity );

            return DelReturn.Continue;
        }

        public ScourgeDifficulty GetDifficultyForFaction( Faction faction )
        {
            string factionAllegiance = faction.BaseInfo.Allegiance;
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            ScourgeDifficulty outputRow = null;
            bool debug = false;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                ScourgeDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: Checking <" + row.ToString() + "> for  " + faction.ToString() + " allegiance: " + factionAllegiance, Verbosity.DoNotShow );
                if ( ArcenStrings.Equals( factionAllegiance, "对AI友好" ) ||
                     ArcenStrings.Equals( factionAllegiance, "内战" ))
                {
                    if ( !row.AIAllied )
                        continue;
                    outputRow = row;
                    break;
                }
                else if ( ArcenStrings.Equals( factionAllegiance, "对玩家友好" ) )
                {
                    if ( !row.PlayerAllied )
                        continue;
                    outputRow = row;
                    break;

                }
                else if ( factionAllegiance.Contains( "Minor Faction Team" ) )
                {
                    if ( !row.MinorFactionAllied )
                        continue;
                    outputRow = row;
                    break;
                }
                else
                {
                    throw new Exception( "Could not find valid allegiance " + factionAllegiance + " for " + faction.ToString() );
                }
            }
            if ( outputRow == null )
            {
                throw new Exception( "Could not find ScourgeDifficulty for " + faction.ToString() );
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + " as difficulty for " + faction.ToString() + " " + factionAllegiance + ", " + factionIntensity, Verbosity.DoNotShow );
            return outputRow;
        }
    }
}
