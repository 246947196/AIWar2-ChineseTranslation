using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //Tracks internal data needed by the Human Resistance Fighters
    public class HumanResistanceFighterFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        //these planets have recently had HRF reinforcements, so they are ineligible for a while. The int contains the gameSeconds that the HRF last arrived
        public readonly Dictionary<Planet, int> IneligiblePlanets = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "HumanResistanceFighterFactionBaseInfo-IneligiblePlanets" );
        public FInt Budget; //current budget for the HRF
        public string PostBattleBehaviour; //After a battle, HRF will either warp back out (and have their strength added to the budget again), or stick around to patrol

        //not serialized
        public static HumanResistanceFighterFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public int Intensity = 0;
        public FInt OverkillRatio = FInt.FromParts( 3, 000 ); //don't help if the humans are much stronger
        
        public int GetChanceOfHelping()
        {
            return ChanceToHelpHumans;
        }
        
        public HumanResistanceFighterFactionBaseInfo()
        {
            Cleanup();
        }
        
        protected override void Cleanup()
        {
            this.Budget = FInt.FromParts( -1, 000 ); //current budget for the HRF
            this.OverkillRatio = FInt.FromParts( 3, 000 ); //don't help if the humans are much stronger before we help
            this.PostBattleBehaviour = "HitAndRun"; //After a battle, HRF will either warp back out (and have their strength added to the budget again), or stick around to patrol

            Instance = null;
            Intensity = 0;
            IneligiblePlanets.Clear();
        }


        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "20 来自人类抵抗军的负载" );
            return 20;
        }

        #region Xml Data
        private bool HaveLoadedData;
        public int IneligibleSeconds = 0;
        public FInt FriendlyMinStrength;
        public FInt EnemyMinStrength;
        public FInt RatioForNeutralPlanet;
        public FInt RatioForEnemyPlanet;
        public FInt RatioForFriendlyPlanet;
        public FInt Braveness_Constant;
        public int ChanceToHelpHumans;
        public FInt BudgetPerSecond;
        public FInt StartingBudget;
        public FInt MaxBudget;
        public FInt MinBudgetToHelp;
        public FInt BudgetMultiplierIntensity1;
        public FInt BudgetMultiplierIntensity2;
        public FInt BudgetMultiplierIntensity3;
        public FInt BudgetMultiplierIntensity4;
        public FInt BudgetMultiplierIntensity5;
        public FInt BudgetMultiplierIntensity6;
        public FInt BudgetMultiplierIntensity7;
        public FInt BudgetMultiplierIntensity8;
        public FInt BudgetMultiplierIntensity9;
        public FInt BudgetMultiplierIntensity10;
        
        private bool logAll;
        private bool logStage1;
        private bool logStage2;
        private bool logStage3;
        private bool logBudget;

        public bool LogStage1
        {
            get
            {
                return this.logAll || logStage1;
            }
        }
        
        public bool LogStage2
        {
            get
            {
                return this.logAll || logStage2;
            }
        }
        
        public bool LogStage3
        {
            get
            {
                return this.logAll || logStage3;
            }
        }
        
        public bool LogBudget
        {
            get
            {
                return this.logAll || logBudget;
            }
        }
        
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            this.IneligibleSeconds = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanResistanceFighters_IneligibleSeconds" );
            this.FriendlyMinStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_FriendlyMinStrength" );
            this.EnemyMinStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_EnemyMinStrength" );
            this.RatioForNeutralPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_RatioForNeutralPlanet" );
            this.RatioForEnemyPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_RatioForEnemyPlanet" );
            this.RatioForFriendlyPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_RatioForFriendlyPlanet" );
            this.Braveness_Constant = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BravenessConstant" );
            this.ChanceToHelpHumans = ExternalConstants.Instance.GetCustomByte_Slow( "custom_int_HumanResistanceFighters_ChanceToHelpHumans" );
            this.BudgetPerSecond = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetPerSecond" );
            this.StartingBudget = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_StartingBudget" ); //the starting budget is mostly for testing
            this.MaxBudget = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BaseMaxBudget" );
            this.MinBudgetToHelp = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_MinBudgetToHelp" );
            this.BudgetMultiplierIntensity1 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity1" );
            this.BudgetMultiplierIntensity2 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity2" );
            this.BudgetMultiplierIntensity3 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity3" );
            this.BudgetMultiplierIntensity4 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity4" );
            this.BudgetMultiplierIntensity5 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity5" );
            this.BudgetMultiplierIntensity6 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity6" );
            this.BudgetMultiplierIntensity7 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity7" );
            this.BudgetMultiplierIntensity8 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity8" );
            this.BudgetMultiplierIntensity9 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity9" );
            this.BudgetMultiplierIntensity10 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanResistanceFighters_BudgetMultiplierIntensity10" );
            this.logAll = ExternalConstants.Instance.GetCustomBool_Slow("custom_hrf_log_all");
            this.logStage1 = ExternalConstants.Instance.GetCustomBool_Slow("custom_hrf_log_stage1");
            this.logStage2 = ExternalConstants.Instance.GetCustomBool_Slow("custom_hrf_log_stage2");
            this.logStage3 = ExternalConstants.Instance.GetCustomBool_Slow("custom_hrf_log_stage3");
            this.logBudget = ExternalConstants.Instance.GetCustomBool_Slow("custom_hrf_log_budget");
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.IneligiblePlanets.Count );
            foreach ( KeyValuePair<Planet, int> pair in this.IneligiblePlanets )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, pair.Key.Index );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
            }
            Buffer.AddFInt( MetaData, this.Budget );
            Buffer.AddString_Condensed( MetaData, PostBattleBehaviour );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            IneligiblePlanets.Clear();
            Int16 numLookupElements = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < numLookupElements; i++ )
            {
                //I break these variables out to make it clear what's happening
                Int16 planetIdx = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
                int numSeconds = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
                this.IneligiblePlanets[World_AIW2.Instance.GetPlanetByIndex( planetIdx )] = numSeconds;
            }
            this.Budget = Buffer.ReadFInt( MetaData );
            this.PostBattleBehaviour = Buffer.ReadString_Condensed( MetaData );

            if ( Buffer.FromGameVersion.GetLessThan( 5, 700 ) )
                Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion

        #region getBudgetMutliplier
        public FInt getBudgetMutliplier()
        {
            FInt multiplier;
            switch ( this.Intensity )
            {
                case 1:
                    multiplier = this.BudgetMultiplierIntensity1;
                    break;
                case 2:
                    multiplier = this.BudgetMultiplierIntensity2;
                    break;
                case 3:
                    multiplier = this.BudgetMultiplierIntensity3;
                    break;
                case 4:
                    multiplier = this.BudgetMultiplierIntensity4;
                    break;
                case 5:
                    multiplier = this.BudgetMultiplierIntensity5;
                    break;
                case 6:
                    multiplier = this.BudgetMultiplierIntensity6;
                    break;
                case 7:
                    multiplier = this.BudgetMultiplierIntensity7;
                    break;
                case 8:
                    multiplier = this.BudgetMultiplierIntensity8;
                    break;
                case 9:
                    multiplier = this.BudgetMultiplierIntensity9;
                    break;
                case 10:
                    multiplier = this.BudgetMultiplierIntensity10;
                    break;
                default:
                    throw new Exception( "Unexpected HRF intensity " + this.Intensity );
            }
            return multiplier;
        }
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
    }
}
