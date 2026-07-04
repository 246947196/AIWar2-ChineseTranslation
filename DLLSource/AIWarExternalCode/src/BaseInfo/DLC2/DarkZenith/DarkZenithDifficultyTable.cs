using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<DarkZenithDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public int PlanetsToSpawn;
        public short TerritorialSphereSize;
        public int HarvesterCapacity;
        public int MetalHarvestablePerSecond;
        public int MetalIncomeWithoutHarvesters;
        public int MaxHarvestersPerMetalTerminus;
        public int MaxTerminiiPerPlanet;
        public int MaxEpistylesPerPlanet;
        public int MaxUtilityStructuresPerPlanet;
        public int BaseTimeBetweenPrivateers;
        public int TimeBetweenResourceConversions;
        //The DZ can request the AI send exos against it.
        //Exo Rules: The Exos only apply to Full Invasion
        //           Exos start once the DZ has taken its Territorial Sphere
        //           Exos are sent against the Jormugandr (which will allow the Jormugandr to go rampaging)
        //           The intent of Exos is less to actually kill the DZ outright, and more to wear it down
        public int BaseExoStrength;
        public int BaseExoInterval;

        public int AIDragonsToSpawn; //only for full invasion; AI gets bonus defenses
        //Strength values for initial invasion.
        public FInt BaseInvasionStrengthPerPlanet; //for ships spawned at invasion start
        public FInt StrengthMultiplierPerEnemyPowerLevel; //ships spawned at invasion start; balance lever
        public int AIDifficultyIncreasePoint; //ships spawned at invasion start; balance lever. Note the multiplier starts at 1
        public FInt StrengthMultiplierIncreasePerDifficultyOverPoint; //ships spawned at invasion start; balance lever. Note the multiplier starts at 1
        public int InitialMetal; //free resources for epistyles at invasion time
        public int InitialOtherResources;//free resources for epistyles at invasion time
        //while our initial conquest is happening, every Interval seconds the DZ gets some bonus stuff; free ships and resources at Epistyles
        public int InitialInvasionInterval;
        public int InitialInvasionFreeShips; //weak ships
        public int InitialInvasionFreeShipsPerHour; //for each hour after the games' start, boost the income during invasions. It was felt that late game DZs were too weak
        public int InitialInvasionMetalIncome;
        public int InitialInvasionOtherIncome;
        public int InitialInvasionExtraMetalPerHour;
        public int InitialInvasionOtherIncomePerHour;

        public int SpireRelatedMetalIncome = 10;
        public int SpireRelatedOtherIncome = 1;

        public int SpireRelatedIncomeInterval = 25;

        public int SciencePerPlanetFimbulwintered = 25;

        //Jormugandr specific stuff
        public int NumJormugandrToSpawn;
        public int TimeBetweenJormugandrDormantMoves;
        public int TimeJorumugandrActivePerDZHomeworldAssault;

        //Fimbulwinter stuff
        public int TimeToConvertPlanet;
        //different balance for different allegiances
        public bool ForHumanAllied;
        public bool ForMinorFactionAllied;
        public bool ForPlayer;
        public bool ForAIAllied;
        public override string ToString()
        {
            return "Intensity " + this.Intensity + " BaseInvasionStrengthPerPlanet " + this.BaseInvasionStrengthPerPlanet + " mult " + StrengthMultiplierPerEnemyPowerLevel;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private DarkZenithDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "DarkZenithDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<DarkZenithDifficulty> Pool = new ConcurrentPool<DarkZenithDifficulty>( "DarkZenithDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DarkZenithDifficulty(); } );

        public static DarkZenithDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<DarkZenithDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<DarkZenithDifficulty>( new DarkZenithDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }

    public class DarkZenithDifficultyTable : ArcenDynamicTable<DarkZenithDifficulty>
    {
        public static DarkZenithDifficultyTable Instance;

        public DarkZenithDifficultyTable() : base( "DarkZenithDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override DarkZenithDifficulty GetNewRowFromPool()
        {
            return DarkZenithDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, DarkZenithDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );
            Data.Fill( "PlanetsToSpawn", ref TypeDataObject.PlanetsToSpawn, !Data.ReadingPartialRecord );
            Data.Fill( "TerritorialSphereSize", ref TypeDataObject.TerritorialSphereSize, !Data.ReadingPartialRecord );
            Data.Fill( "HarvesterCapacity", ref TypeDataObject.HarvesterCapacity, !Data.ReadingPartialRecord );
            Data.Fill( "MetalHarvestablePerSecond", ref TypeDataObject.MetalHarvestablePerSecond, !Data.ReadingPartialRecord );
            Data.Fill( "MetalIncomeWithoutHarvesters", ref TypeDataObject.MetalIncomeWithoutHarvesters, !Data.ReadingPartialRecord );
            Data.Fill( "MaxHarvesters", ref TypeDataObject.MaxHarvestersPerMetalTerminus, !Data.ReadingPartialRecord );
            Data.Fill( "MaxTerminiiPerPlanet", ref TypeDataObject.MaxTerminiiPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "MaxEpistylesPerPlanet", ref TypeDataObject.MaxEpistylesPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "MaxUtilityStructuresPerPlanet", ref TypeDataObject.MaxUtilityStructuresPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "BaseTimeBetweenPrivateers", ref TypeDataObject.BaseTimeBetweenPrivateers, !Data.ReadingPartialRecord );
            Data.Fill( "TimeBetweenResourceConversions", ref TypeDataObject.TimeBetweenResourceConversions, !Data.ReadingPartialRecord );

            Data.Fill( "BaseInvasionStrengthPerPlanet", ref TypeDataObject.BaseInvasionStrengthPerPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "StrengthMultiplierPerEnemyPowerLevel", ref TypeDataObject.StrengthMultiplierPerEnemyPowerLevel, !Data.ReadingPartialRecord );
            Data.Fill( "AIDifficultyIncreasePoint", ref TypeDataObject.AIDifficultyIncreasePoint, !Data.ReadingPartialRecord );
            Data.Fill( "StrengthMultiplierIncreasePerDifficultyOverPoint", ref TypeDataObject.StrengthMultiplierIncreasePerDifficultyOverPoint, !Data.ReadingPartialRecord );
            Data.Fill( "InitialMetal", ref TypeDataObject.InitialMetal, !Data.ReadingPartialRecord );
            Data.Fill( "InitialOtherResources", ref TypeDataObject.InitialOtherResources, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionInterval", ref TypeDataObject.InitialInvasionInterval, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionFreeShips", ref TypeDataObject.InitialInvasionFreeShips, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionExtraFreeShipsPerHour", ref TypeDataObject.InitialInvasionFreeShipsPerHour, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionMetalIncome", ref TypeDataObject.InitialInvasionMetalIncome, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionOtherIncome", ref TypeDataObject.InitialInvasionOtherIncome, !Data.ReadingPartialRecord );
            Data.Fill( "SpireRelatedMetalIncome", ref TypeDataObject.SpireRelatedMetalIncome, false ); //uses the C# default otherwise
            Data.Fill( "SpireRelatedOtherIncome", ref TypeDataObject.SpireRelatedOtherIncome, false );
            Data.Fill( "SpireRelatedIncomeInterval", ref TypeDataObject.SpireRelatedIncomeInterval, false );
            Data.Fill( "InitialInvasionExtraMetalPerHour", ref TypeDataObject.InitialInvasionExtraMetalPerHour, !Data.ReadingPartialRecord );
            Data.Fill( "InitialInvasionOtherIncomePerHour", ref TypeDataObject.InitialInvasionOtherIncomePerHour, !Data.ReadingPartialRecord );

            Data.Fill( "NumJormugandrToSpawn", ref TypeDataObject.NumJormugandrToSpawn, false ); //this can be 0
            Data.Fill( "TimeBetweenJormugandrDormantMoves", ref TypeDataObject.TimeBetweenJormugandrDormantMoves, !Data.ReadingPartialRecord );
            Data.Fill( "TimeJorumugandrActivePerDZHomeworldAssault", ref TypeDataObject.TimeJorumugandrActivePerDZHomeworldAssault, !Data.ReadingPartialRecord );
            Data.Fill( "ForHumanAllied", ref TypeDataObject.ForHumanAllied, false ); //this can be 0
            Data.Fill( "ForPlayer", ref TypeDataObject.ForPlayer, false ); //this can be 0
            Data.Fill( "ForAIAllied", ref TypeDataObject.ForAIAllied, false ); //this can be 0
            Data.Fill( "ForMinorFactionAllied", ref TypeDataObject.ForMinorFactionAllied, false ); //this can be 0
            Data.Fill( "TimeToConvertPlanet", ref TypeDataObject.TimeToConvertPlanet, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExoStrength", ref TypeDataObject.BaseExoStrength, false );
            Data.Fill( "BaseExoInterval", ref TypeDataObject.BaseExoInterval, false );
            Data.Fill( "AIDragonsToSpawn", ref TypeDataObject.AIDragonsToSpawn, false );

            Data.Fill( "SciencePerPlanetFimbulwintered", ref TypeDataObject.SciencePerPlanetFimbulwintered, false );
            
            if ( TypeDataObject.BaseExoInterval > 0 && TypeDataObject.BaseExoStrength <= 0 ||
                 TypeDataObject.BaseExoInterval <= 0 && TypeDataObject.BaseExoStrength > 0 )
                throw new Exception( "DZ Difficulty " + TypeDataObject.name + " XML error: Exo Interval: " + TypeDataObject.BaseExoInterval + " and strength " + TypeDataObject.BaseExoStrength );
            return DelReturn.Continue;
        }
        public DarkZenithDifficulty GetRowByIntensity( int factionItensity, Faction faction )
        {
            string factionAllegiance = faction.BaseInfo.Allegiance;
            if ( faction.Type == FactionType.Player )
                factionAllegiance = "MinorFaction"; //player factions are always given the Difficulty of the Minor Faction (thats the hardest)
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                if ( this.Rows[i].Intensity == factionItensity )
                {
                    if ( faction.Type == FactionType.Player )
                    {
                        if ( this.Rows[i].ForPlayer )
                            return this.Rows[i];
                        continue; 
                    }
                    if ( this.Rows[i].ForHumanAllied )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "Friendly To Players" ) )
                        {
                            return this.Rows[i];
                        }
                    }
                    else if ( this.Rows[i].ForAIAllied )
                    {
                        if ( ArcenStrings.Equals( factionAllegiance, "Allied To AI" ) ||
                             ArcenStrings.Equals( factionAllegiance, "Civil War" ) )
                            
                        {
                            return this.Rows[i];
                        }
                    }
                    else if ( this.Rows[i].ForMinorFactionAllied )
                    {
                        if ( StringExtensions.Contains( factionAllegiance, "MinorFaction", StringComparison.CurrentCultureIgnoreCase ) )
                            return this.Rows[i];
                    }
                    else if ( this.Rows[i].ForPlayer )
                    {
                        if ( faction.Type == FactionType.Player )
                            return this.Rows[i];
                    }

                    else
                    {
                        if ( !ArcenStrings.Equals( factionAllegiance, "MinorFaction" ) &&
                             !ArcenStrings.Equals( factionAllegiance, "Friendly To Players" ) )
                            return this.Rows[i];
                    }
                }
            }
            return null;
        }
    }
}
