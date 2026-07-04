using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class FallenSpireDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<FallenSpireDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;
        public byte RelicsForLevel1Income;
        public byte RelicsForLevel2Income;
        public byte RelicsForLevel3Income;
        public byte RelicsForLevel4Income;
        public byte RelicsForLevel5Income;

        public int BaseLevel1AIIncome;
        public int BaseLevel2AIIncome;
        public int BaseLevel3AIIncome;
        public int BaseLevel4AIIncome;
        public int BaseLevel5AIIncome;
        public int Level1AIIncomePer10AIP;
        public int Level2AIIncomePer10AIP;
        public int Level3AIIncomePer10AIP;
        public int Level4AIIncomePer10AIP;
        public int Level5AIIncomePer10AIP;

        public int BaseRelicResponseStrength;
        public int ResponseIncreasePerRelic;
        public FInt RelicResponseWaveMultiplier; //so relic responses scale with the AI wave budget (ie AIP)
        public int BaseExoStrength;
        public int AdditiveExoStrengthIncreasePerExo;
        public FInt MultiplicativeExoStrengthIncreasePerExo;

        public int BaseExoIncome;
        public FInt ExoIncomePer10AIP;

        public int DebrisToSpawnPerRelic;
        public int DebrisDuration;

        public int RelicsForDarkSpireConquestMode;

        public int SearchModePercent;
        public int RelicResponseInterval;
        public FInt RelicResponseIncreasePerPlanetChecked;

        public FInt RelicResponseMultiplierPerCity;
        public FInt ExoStrengthIncreaseMultiplierPerCity;
        public FInt ExoIncomeMultiplierPerCity;

        public int ImperialSpireWaitTime;
        public int ImperialSpireAttackInterval;
        public int ImperialSpireSecondaryAttackInterval;
        public FInt ImperialSpireSecondaryMultiplier;

        public FInt GeneralAIResponseIncreasePerCity; //for any hack that the AI responds to , increase it by (this value * numCities).
                                                      //there was a problem where superterminal hacks were trivial with the spire fleet

        public override string ToString()
        {
            //For debug purposes
            string output = "FallenSpireDifficulty " + name + ", Intensity" + Intensity + ", ";
            output += "RelicsForLevel1Income " + RelicsForLevel1Income + ", ";
            output += "RelicsForLevel2Income " + RelicsForLevel2Income + ", ";
            output += "RelicsForLevel3Income " + RelicsForLevel3Income + ", ";
            output += "RelicsForLevel4Income " + RelicsForLevel4Income + ", ";
            output += "RelicsForLevel5Income " + RelicsForLevel5Income + ",\n ";

            output += "BaseLevel1AIIncome " + BaseLevel1AIIncome + ", ";
            output += "BaseLevel2AIIncome " + BaseLevel2AIIncome + ", ";
            output += "BaseLevel3AIIncome " + BaseLevel3AIIncome + ", ";
            output += "BaseLevel4AIIncome " + BaseLevel4AIIncome + ", ";
            output += "BaseLevel5AIIncome " + BaseLevel5AIIncome + ", \n";

            output += "Level1AIIncomePer10AIP " + Level1AIIncomePer10AIP + ", ";
            output += "Level2AIIncomePer10AIP " + Level2AIIncomePer10AIP + ", ";
            output += "Level3AIIncomePer10AIP " + Level3AIIncomePer10AIP + ", ";
            output += "Level4AIIncomePer10AIP " + Level4AIIncomePer10AIP + ", ";
            output += "Level5AIIncomePer10AIP " + Level5AIIncomePer10AIP + ", \n";

            output += "BaseRelicResponseStrength " + BaseRelicResponseStrength + " ";
            output += "ResponseIncreasePerRelic " + ResponseIncreasePerRelic + " ";
            output += "RelicResponseWaveMultiplier " + RelicResponseWaveMultiplier + " ";
            output += "BaseExoIncome " + BaseExoIncome + " ";
            output += "AdditiveExoStrengthIncreasePerExo " + AdditiveExoStrengthIncreasePerExo + " \n";
            output += "MultiplicativeExoStrengthIncreasePerExo " + MultiplicativeExoStrengthIncreasePerExo + " ";
            output += "BaseExoIncome " + BaseExoIncome + " ";
            output += "ExoIncomePer10AIP " + ExoIncomePer10AIP + " ";
            output += "DebrisToSpawnPerRelic " + DebrisToSpawnPerRelic + " ";
            output += "DebrisDuration " + DebrisDuration + "\n ";
            output += "RelicsForDarkSpireConquestMode " + RelicsForDarkSpireConquestMode + " ";
            output += "SearchModePercent " + SearchModePercent + " ";
            output += "RelicResponseInterval " + RelicResponseInterval + " ";
            output += "RelicResponseIncreasePerPlanetChecked " + RelicResponseIncreasePerPlanetChecked + "\n";
            output += "RelicResponseMultiplierPerCity " + RelicResponseMultiplierPerCity + " ";
            output += "ExoStrengthIncreaseMultiplierPerCity " + ExoStrengthIncreaseMultiplierPerCity + " ";
            output += "ExoIncomeMultiplierPerCity " + ExoIncomeMultiplierPerCity + "\n";
            output += "ImperialSpireWaitTime " + ImperialSpireWaitTime + " ";
            output += "ImperialSpireAttackInterval " + ImperialSpireAttackInterval + " ";
            output += "GeneralAIResponseIncreasePerCity " + GeneralAIResponseIncreasePerCity + " ";
            return output;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private FallenSpireDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "FallenSpireDifficultys" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<FallenSpireDifficulty> Pool = new ConcurrentPool<FallenSpireDifficulty>( "FallenSpireDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new FallenSpireDifficulty(); } );

        public static FallenSpireDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<FallenSpireDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<FallenSpireDifficulty>( new FallenSpireDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class FallenSpireDifficultyTable : ArcenDynamicTable<FallenSpireDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static FallenSpireDifficultyTable Instance;
        public readonly FallenSpireDifficulty[] RowsByIntensity = new FallenSpireDifficulty[20]; //an excess of rows, that's fine

        public FallenSpireDifficultyTable() : base( "FallenSpireDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override FallenSpireDifficulty GetNewRowFromPool()
        {
            return FallenSpireDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, FallenSpireDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            //The AI gets spire ships as you take more relics, for balance
            Data.Fill( "RelicsForLevel1Income", ref TypeDataObject.RelicsForLevel1Income, !Data.ReadingPartialRecord );
            Data.Fill( "RelicsForLevel2Income", ref TypeDataObject.RelicsForLevel2Income, !Data.ReadingPartialRecord );
            Data.Fill( "RelicsForLevel3Income", ref TypeDataObject.RelicsForLevel3Income, !Data.ReadingPartialRecord );
            Data.Fill( "RelicsForLevel4Income", ref TypeDataObject.RelicsForLevel4Income, !Data.ReadingPartialRecord );
            Data.Fill( "RelicsForLevel5Income", ref TypeDataObject.RelicsForLevel5Income, !Data.ReadingPartialRecord );
            Data.Fill( "BaseLevel1AIIncome", ref TypeDataObject.BaseLevel1AIIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseLevel2AIIncome", ref TypeDataObject.BaseLevel2AIIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseLevel3AIIncome", ref TypeDataObject.BaseLevel3AIIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseLevel4AIIncome", ref TypeDataObject.BaseLevel4AIIncome, !Data.ReadingPartialRecord );
            Data.Fill( "BaseLevel5AIIncome", ref TypeDataObject.BaseLevel5AIIncome, !Data.ReadingPartialRecord );
            Data.Fill( "Level1AIIncomePer10AIP", ref TypeDataObject.Level1AIIncomePer10AIP, !Data.ReadingPartialRecord );
            Data.Fill( "Level2AIIncomePer10AIP", ref TypeDataObject.Level2AIIncomePer10AIP, !Data.ReadingPartialRecord );
            Data.Fill( "Level3AIIncomePer10AIP", ref TypeDataObject.Level3AIIncomePer10AIP, !Data.ReadingPartialRecord );
            Data.Fill( "Level4AIIncomePer10AIP", ref TypeDataObject.Level4AIIncomePer10AIP, !Data.ReadingPartialRecord );
            Data.Fill( "Level5AIIncomePer10AIP", ref TypeDataObject.Level5AIIncomePer10AIP, !Data.ReadingPartialRecord );

            //balance numbers for the relic pursuit response
            Data.Fill( "BaseRelicResponseStrength", ref TypeDataObject.BaseRelicResponseStrength, !Data.ReadingPartialRecord );
            Data.Fill( "ResponseStrengthIncreasePerRelic", ref TypeDataObject.ResponseIncreasePerRelic, !Data.ReadingPartialRecord );
            Data.Fill( "RelicResponseWaveMultiplier", ref TypeDataObject.RelicResponseWaveMultiplier, !Data.ReadingPartialRecord );
            Data.Fill( "RelicResponseInterval", ref TypeDataObject.RelicResponseInterval, !Data.ReadingPartialRecord );
            Data.Fill( "RelicResponseIncreasePerPlanetChecked", ref TypeDataObject.RelicResponseIncreasePerPlanetChecked, !Data.ReadingPartialRecord );

            //balance numbers for Exos (periodic large assaults the AI sends
            Data.Fill( "BaseExoStrength", ref TypeDataObject.BaseExoStrength, !Data.ReadingPartialRecord );
            Data.Fill( "AdditiveExoStrengthIncreasePerExo", ref TypeDataObject.AdditiveExoStrengthIncreasePerExo, !Data.ReadingPartialRecord );
            Data.Fill( "MultiplicativeExoStrengthIncreasePerExo", ref TypeDataObject.MultiplicativeExoStrengthIncreasePerExo, !Data.ReadingPartialRecord );
            Data.Fill( "BaseExoIncome", ref TypeDataObject.BaseExoIncome, !Data.ReadingPartialRecord );
            Data.Fill( "ExoIncomePer10AIP", ref TypeDataObject.ExoIncomePer10AIP, !Data.ReadingPartialRecord );

            //balance for spire debris, created shortly after you capture a relic
            Data.Fill( "DebrisToSpawnPerRelic", ref TypeDataObject.DebrisToSpawnPerRelic, !Data.ReadingPartialRecord );
            Data.Fill( "DebrisDuration", ref TypeDataObject.DebrisDuration, !Data.ReadingPartialRecord );

            //how many relics will turn on dark spire conquest mode
            Data.Fill( "RelicsForDarkSpireConquestMode", ref TypeDataObject.RelicsForDarkSpireConquestMode, !Data.ReadingPartialRecord );

            //what percentage of relics are placed in "search mode" (ie you hack to find out how far you are from the relic; you don't know the planet)
            Data.Fill( "SearchModePercent", ref TypeDataObject.SearchModePercent, !Data.ReadingPartialRecord );

            //multiplier increases based on the number of spire cities you have
            Data.Fill( "RelicResponseMultiplierPerCity", ref TypeDataObject.RelicResponseMultiplierPerCity, !Data.ReadingPartialRecord );
            Data.Fill( "ExoStrengthIncreaseMultiplierPerCity", ref TypeDataObject.ExoStrengthIncreaseMultiplierPerCity, !Data.ReadingPartialRecord );
            Data.Fill( "ExoIncomeMultiplierPerCity", ref TypeDataObject.ExoIncomeMultiplierPerCity, !Data.ReadingPartialRecord );

            Data.Fill( "ImperialSpireWaitTime", ref TypeDataObject.ImperialSpireWaitTime, !Data.ReadingPartialRecord );
            Data.Fill( "ImperialSpireAttackInterval", ref TypeDataObject.ImperialSpireAttackInterval, !Data.ReadingPartialRecord );
            Data.Fill( "ImperialSpireSecondaryAttackInterval", ref TypeDataObject.ImperialSpireSecondaryAttackInterval, !Data.ReadingPartialRecord );
            Data.Fill( "ImperialSpireSecondaryMultiplier", ref TypeDataObject.ImperialSpireSecondaryMultiplier, !Data.ReadingPartialRecord );
            Data.Fill( "GeneralAIResponseIncreasePerCity", ref TypeDataObject.GeneralAIResponseIncreasePerCity, !Data.ReadingPartialRecord );
            return DelReturn.Continue;
        }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
            for ( int i = 0; i < RowsByIntensity.Length; i++ )
                RowsByIntensity[i] = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                FallenSpireDifficulty row = this.Rows[i];
                RowsByIntensity[row.Intensity] = row; //only meant to have a single row per intensity
            }
        }
        public FallenSpireDifficulty GetRowByIntensity( int Intensity )
        {
            return RowsByIntensity[Intensity];
        }
    }
}
