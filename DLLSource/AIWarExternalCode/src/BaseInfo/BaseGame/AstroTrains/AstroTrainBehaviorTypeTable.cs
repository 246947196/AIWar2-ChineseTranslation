using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    /* These classes are for the XML data in the AstroTrainBehaviorType xml directory. We read the data from the XML, put it in this table,
       then copy it into the PerDepotData where it's actually used. */
    public class AstroTrainBehaviorType : ArcenDynamicTableRow, IConcurrentPoolable<AstroTrainBehaviorType>, IProtectedListable
    {
        public string name;
        public bool FiresOnEveryTrain;
        public int TrainsNeededBeforeFiring;
        public int TrainsToSendBeforeFiring;
        public int TrainsAllowedOnMap;
        public int NumUnitsToSpawn;
        public FInt StrengthAssociated;
        public FInt BonusStrengthPerAIP;
        public string TagsToSpawn;
        public bool SpawnOnLocalPlanet;
        public bool SelfDestructOnFiring;
        public int MinIntensityAllowed = -1;
        public int MaxIntensityAllowed = -1;
        public string DestinationFaction;
        public string EffectType;
        public string BudgetToBoost;
        public bool TrainsMustGoNearPlayer;
        public FInt EffectMultiplierLowDifficulty;
        public FInt EffectMultiplierMediumDifficulty;
        public FInt EffectMultiplierHighDifficulty;


        public int id;
        public override string ToString()
        {
            //this returns a string representation of the effect. Used for various player descriptions
            AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
            byte difficulty = highestDifficulty.Difficulty;
            FInt difficultyMultiplier;

            if ( difficulty <= 3 )
                difficultyMultiplier = this.EffectMultiplierLowDifficulty;
            else if ( difficulty <= 7 )
                difficultyMultiplier = this.EffectMultiplierMediumDifficulty;
            else
                difficultyMultiplier = this.EffectMultiplierHighDifficulty;
            //            ArcenDebugging.ArcenDebugLogSingleLine("current difficulty " + difficulty + " mult " + difficultyMultiplier, Verbosity.DoNotShow );
            string output;
            if ( this.name == "ReinforceHunterFleet" )
            {
                output = "Reinforces the Hunter fleet with ships of strength " + (this.StrengthAssociated.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + " + " +
                    (this.BonusStrengthPerAIP.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + "xAIP every time a train arrives.";
            }
            else if ( this.name == "ReinforceWardenFleet" )
            {
                output = "Reinforces the Warden fleet with ships of strength " + (this.StrengthAssociated.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + " + " +
                    (this.BonusStrengthPerAIP.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + "xAIP every time a train arrives.";
            }
            else if ( this.name == "SpawnNewStation" )
            {
                output = "Spawns a new station on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnGolem" )
            {
                output = "Spawns a Golem on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnAIWave" )
            {
                if ( this.TrainsNeededBeforeFiring == -1 )
                    output = "Spawns a wave for the AI every time a train arrives.";
                else
                    output = "Spawns a wave for the AI when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnFortress" )
            {
                output = "Spawns a Fortress on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnDireGuardian" )
            {
                output = "Spawns a Dire Guardian on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnHunterKiller" )
            {
                output = "Spawns a Hunter / Killer on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnRavenousShadow" )
            {
                output = "Spawns a Ravenous Shadow on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "BoostCPA" )
            {
                output = "Gives the AI an additional " + (this.StrengthAssociated.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + " + " +
                    (this.BonusStrengthPerAIP.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + "xAIP strength for its next CPA for each train that arrives.";
            }
            else if ( this.name == "BoostWave" )
            {
                output = "Gives the AI an additional " + (this.StrengthAssociated.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + " + " +
                    (this.BonusStrengthPerAIP.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + "xAIP strength for its next Wave for each train that arrives.";
            }
            else if ( this.name == "BoostWormholeInvasion" )
            {
                output = "Gives the AI an additional " + (this.StrengthAssociated.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + " + " +
                    (this.BonusStrengthPerAIP.ToFloatNonSim() / 1000f).ToString( "#,##0.0#" ) + "xAIP strength for its next Wormhole Invasion for each train that arrives.";
            }
            else if ( this.name == "SpawnFenrirPrototype" )
            {
                output = "Spawns a Fenrir prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnReanimatorPrototype" )
            {
                output = "Spawns a Reanimator prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnUmbraPrototype" )
            {
                output = "Spawns an Umbra prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnShellshockerPrototype" )
            {
                output = "Spawns a Shellshocker prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnCustodianPrototype" )
            {
                output = "Spawns a Custodian prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else if ( this.name == "SpawnWarspitePrototype" )
            {
                output = "Spawns a Warspite prototype on the map when " + this.TrainsNeededBeforeFiring + " have arrived.";
            }
            else
            {
                output = "C# code in AstroTrains.cs needs to be added to handle depot type " + this.name + ".";
            }

            if ( difficultyMultiplier != FInt.Zero && difficultyMultiplier != FInt.One )
                output += " Due to the current AI difficulty, there is a multiplier of " + difficultyMultiplier + " for this effect.";
            //                output += " Due to the current AI difficulty, there is a multiplier of " + ( difficultyMultiplier.ToFloatNonSim() / 1000f ).ToString( "#,##0.0#" ) + " for this effect."; //we use this ToFloatNonSim code for other FInts to print them, but it's always printing 0 here and I don't know why
            return output;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AstroTrainBehaviorType() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AstroTrainBehaviorTypes" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AstroTrainBehaviorType> Pool = new ConcurrentPool<AstroTrainBehaviorType>( "AstroTrainBehaviorTypes", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AstroTrainBehaviorType(); } );

        public static AstroTrainBehaviorType GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AstroTrainBehaviorType> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AstroTrainBehaviorType>( new AstroTrainBehaviorType() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class AstroTrainBehaviorTypeTable : ArcenDynamicTable<AstroTrainBehaviorType>
    {        
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AstroTrainBehaviorTypeTable Instance;
        public readonly DictionaryOfLists<int, AstroTrainBehaviorType> RowsByIntensity = DictionaryOfLists<int, AstroTrainBehaviorType>.Create_WillNeverBeGCed( 300, 20, "AstroTrainBehavior-RowsByIntensity" );
        public readonly Dictionary<int, AstroTrainBehaviorType> RowById = Dictionary<int, AstroTrainBehaviorType>.Create_WillNeverBeGCed( 200, "AstroTrainBehavior-RowById" );

        public AstroTrainBehaviorTypeTable() : base( "AstroTrainBehaviorType", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow,
            //if you don't read it in order on one thread, then the order of operations is complained about below
            ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AstroTrainBehaviorType GetNewRowFromPool()
        {
            return AstroTrainBehaviorType.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override void DoPreInitializationLogic()
        { }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            bool debug = false;
            RowsByIntensity.Clear();
            RowById.Clear();
            RowsByIntensity.Clear();
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                AstroTrainBehaviorType row = this.Rows[i];
                //This is no longer a restriction, but it's left as an example of the sort of XML scrubbing it's probably a good
                //idea to have for modders
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( i + " " + row.name + " faction " + row.DestinationFaction, Verbosity.DoNotShow );
                // if(row.NumUnitsToSpawn > 0 && row.StrengthAssociated > FInt.Zero)
                // {
                //     ArcenDebugging.ArcenDebugLogSingleLine("BUG: An Astro train depot cannot have NumUnitsToSpawn > 0 and StrengthAssociated > 0. Currently Depot class " + row.name + " has this", Verbosity.DoNotShow );
                // }
                RowById[row.id] = row;
                if ( row.MaxIntensityAllowed == -1 )
                    row.MaxIntensityAllowed = 10;
                for ( int j = row.MinIntensityAllowed; j <= row.MaxIntensityAllowed; j++ )
                {
                    RowsByIntensity[j].Add( row );
                }
                if ( row.id != i + 1 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Bug: Your Astro Train Depot ids are out of order for " + row.name + " id " + row.id + ". The id must be in ascending order in the XML file, each id going up one at a time. So row 1 must be id 1, row 2 == id 2, etc... The code was expecting it to be id " + i + 1, Verbosity.DoNotShow );
            }
        }
        public AstroTrainBehaviorType GetRowById( int id )
        {
            return RowById[id];
        }
        public AstroTrainBehaviorType GetRandomRowWithIntensity( ArcenSimContextAnyStatus Context, int currentIntensity )
        {
            bool debug = false;
            //picks a random row allowed for this intensity
            List<AstroTrainBehaviorType> rowList = RowsByIntensity[currentIntensity];
            if ( rowList == null || rowList.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "Error: AstroTrainBehaviorTypeTable::GetRandomRowWithIntensity(" + currentIntensity + ") has no available entries\n", Verbosity.ShowAsError );
                return null;
            }
            if ( debug )
            {
                string output = "";
                for ( int i = 0; i < rowList.Count; i++ )
                {
                    output += rowList[i].name + ", ";
                }
                ArcenDebugging.ArcenDebugLogSingleLine( "For intensity " + currentIntensity + " choosing from the following Depots: " + output, Verbosity.DoNotShow );
            }
            return rowList[Context.RandomToUse.Next( 0, rowList.Count )];
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AstroTrainBehaviorType TypeDataObject )
        {
            bool debug = false;
            //Each entry is added to the XML, then code parsing that XML entry goes here,
            //and the variable the entry is stored in goes into the AstroTrainBehaviorType object

            //passing in "false" means that the field can be empty
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Parsed Astro train depot " + TypeDataObject.name, Verbosity.DoNotShow );
            Data.Fill( "fires_on_every_train", ref TypeDataObject.FiresOnEveryTrain, !Data.ReadingPartialRecord );
            Data.Fill( "trains_needed_before_firing", ref TypeDataObject.TrainsNeededBeforeFiring, !Data.ReadingPartialRecord );
            Data.Fill( "trains_to_send_before_firing", ref TypeDataObject.TrainsToSendBeforeFiring, !Data.ReadingPartialRecord );
            Data.Fill( "trains_allowed_on_map", ref TypeDataObject.TrainsAllowedOnMap, !Data.ReadingPartialRecord );
            Data.Fill( "num_units_to_spawn", ref TypeDataObject.NumUnitsToSpawn, false );
            Data.Fill( "strength_associated", ref TypeDataObject.StrengthAssociated, false );
            Data.Fill( "unit_tags_to_spawn", ref TypeDataObject.TagsToSpawn, false );
            Data.Fill( "bonus_strength_per_aip", ref TypeDataObject.BonusStrengthPerAIP, false );
            Data.Fill( "spawn_on_local_planet", ref TypeDataObject.SpawnOnLocalPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "depot_self_destructs_on_firing", ref TypeDataObject.SelfDestructOnFiring, !Data.ReadingPartialRecord );
            Data.Fill( "min_intensity_to_enable", ref TypeDataObject.MinIntensityAllowed, !Data.ReadingPartialRecord );
            Data.Fill( "max_intensity_to_enable", ref TypeDataObject.MaxIntensityAllowed, !Data.ReadingPartialRecord );
            Data.Fill( "budget_to_boost", ref TypeDataObject.BudgetToBoost, false );
            Data.Fill( "faction_to_own_units", ref TypeDataObject.DestinationFaction, !Data.ReadingPartialRecord );
            Data.Fill( "effect_type", ref TypeDataObject.EffectType, !Data.ReadingPartialRecord );
            Data.Fill( "trains_must_go_near_player", ref TypeDataObject.TrainsMustGoNearPlayer, !Data.ReadingPartialRecord );
            Data.Fill( "effect_multiplier_for_low_difficulty", ref TypeDataObject.EffectMultiplierLowDifficulty, false );
            Data.Fill( "effect_multiplier_for_medium_difficulty", ref TypeDataObject.EffectMultiplierMediumDifficulty, false );
            Data.Fill( "effect_multiplier_for_high_difficulty", ref TypeDataObject.EffectMultiplierHighDifficulty, false );

            Data.Fill( "id", ref TypeDataObject.id, !Data.ReadingPartialRecord );
            if ( TypeDataObject.MinIntensityAllowed > TypeDataObject.MaxIntensityAllowed ||
                 TypeDataObject.MinIntensityAllowed <= 0 || TypeDataObject.MaxIntensityAllowed <= 0 )
                ArcenDebugging.ArcenDebugLogSingleLine( "Train Depot " + TypeDataObject.name + " has a problem with min/max intensities", Verbosity.DoNotShow );
            return DelReturn.Continue;
        }
    }
}
