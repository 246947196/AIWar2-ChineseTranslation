using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class RiskAnalyzerFactionBaseInfo : ExternalFactionBaseInfoRoot, IExoDataHolder, IExternalBaseInfo_Singleton
    {
        //serialized
        public Int16 TotalAIPIncrease;
        public Int16 NetAIPIncrease;
        public int TimeForNextFiring;
        public readonly ExoData exoData = new ExoData();

        //not serialized
        public static RiskAnalyzerFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public int NumberToSeed = 0;
        public int ResponseStrength = 0;

        public RiskAnalyzerFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.TotalAIPIncrease = 0;
            this.NetAIPIncrease = 0;
            this.TimeForNextFiring = -1;
            NumberToSeed = 0;
            ResponseStrength = 0;
            this.exoData.Cleanup();
            ExoMultiplier = FInt.One;

            Instance = null;
            HaveLoadedData = false; //trigger a reload of xml
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "5 来自 AI 风险分析器的负载" );
            return 5;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt16( MetaData, ReadStyle.Signed, TotalAIPIncrease, "TotalAIPIncrease" );
            Buffer.AddInt16( MetaData, ReadStyle.Signed, NetAIPIncrease, "NetAIPIncrease" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextFiring, "timeForNextFiring" );
            this.exoData.SerializeTo( MetaData, Buffer, SerializationCmdType );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TotalAIPIncrease = Buffer.ReadInt16( MetaData, ReadStyle.Signed, "TotalAIPIncrease" );
            this.NetAIPIncrease = Buffer.ReadInt16( MetaData, ReadStyle.Signed, "NetAIPIncrease" );
            this.TimeForNextFiring = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "timeForNextFiring" );
            this.exoData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return NumberToSeed; //closet analogue we have here
        }

        public static readonly string ANALYZER_TAG = "RiskAnalyzer";

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            this.LoadCustomDataIfNeeded();
        }
        #endregion

        #region Xml Data & Settings
        private bool HaveLoadedData;
        public int timeIntervalForAnalyzer = 0;
        public int AIPIncrease = 0;
        public int AIPIncreaseOnDeath = 0;
        public int AIPDecrease = 0;
        public Int16 ExoPercentForWarning = 0;
        public FInt BaseExoWaveStrength = FInt.Zero;
        public FInt MaxExoWaveStrength;
        public FInt NextExoMultiplier;
        private FInt ExoIncomePerAIRiskAnalyzerBase;
        private FInt ExoIncomePerPlayerRiskAnalyzerBase;
        private FInt ExoIncomePerNeutralRiskAnalyzerBase;
        //For higher risk analyzer intensities, reduce the income
        //lest you get a ton of exos quickly
        public byte HighExoIntensity;
        private FInt ExoIncomePerAIRiskAnalyzerHighBase;
        private FInt ExoIncomePerPlayerRiskAnalyzerHighBase;
        private FInt ExoIncomePerNeutralRiskAnalyzerHighBase;
        public int MinutesInToStartChargingExo;

        private FInt ExoMultiplier;
        public FInt ExoIncomePerAIRiskAnalyzer => ExoIncomePerAIRiskAnalyzerBase * ExoMultiplier;
        public FInt ExoIncomePerPlayerRiskAnalyzer => ExoIncomePerPlayerRiskAnalyzerBase * ExoMultiplier;
        public FInt ExoIncomePerNeutralRiskAnalyzer => ExoIncomePerNeutralRiskAnalyzerBase * ExoMultiplier;
        public FInt ExoIncomePerAIRiskAnalyzerHigh => ExoIncomePerAIRiskAnalyzerHighBase * ExoMultiplier;
        public FInt ExoIncomePerPlayerRiskAnalyzerHigh => ExoIncomePerPlayerRiskAnalyzerHighBase * ExoMultiplier;
        public FInt ExoIncomePerNeutralRiskAnalyzerHigh => ExoIncomePerNeutralRiskAnalyzerHighBase * ExoMultiplier;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            // Old External Constants data.
            //timeIntervalForAnalyzer = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_RiskAnalyzer_TimeIntervalInSeconds" );  3600
            //AIPIncrease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_RiskAnalyzer_AIPIncrease" );                        1
            //AIPIncreaseOnDeath = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_RiskAnalyzer_AIPIncreaseOnDeath" );          10
            //AIPDecrease = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_RiskAnalyzer_AIPDecrease" );                        2
            this.ExoPercentForWarning = ExternalConstants.Instance.GetCustomInt16_Slow( "custom_int_RiskAnalyzer_ExoPercentForWarning" );
            this.BaseExoWaveStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_BaseExoWaveStrength" );
            this.MaxExoWaveStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_MaxExoWaveStrength" );
            this.NextExoMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_NextExoMultiplier" );
            this.ExoIncomePerAIRiskAnalyzerBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerAIRiskAnalyzer" );
            this.ExoIncomePerNeutralRiskAnalyzerBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerNeutralRiskAnalyzer" );
            this.ExoIncomePerPlayerRiskAnalyzerBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerPlayerRiskAnalyzer" );
            this.HighExoIntensity = ExternalConstants.Instance.GetCustomByte_Slow( "custom_int_RiskAnalyzer_HighExoIntensity" );
            this.ExoIncomePerAIRiskAnalyzerHighBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerAIRiskAnalyzerHigh" );
            this.ExoIncomePerNeutralRiskAnalyzerHighBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerNeutralRiskAnalyzerHigh" );
            this.ExoIncomePerPlayerRiskAnalyzerHighBase = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_RiskAnalyzer_ExoIncomePerPlayerRiskAnalyzerHigh" );

            this.MinutesInToStartChargingExo = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_RiskAnalyzer_MinutesInToStartChargingExo" );
        }
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            NumberToSeed = cfg.GetIntValueForCustomFieldOrDefaultValue( "NumberToSeed", true );
            ResponseStrength = cfg.GetIntValueForCustomFieldOrDefaultValue( "ResponseStrength", true );
            ExoMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow( $"custom_FInt_RiskAnalyzer_MultIntensity{ResponseStrength}" );
            timeIntervalForAnalyzer = cfg.GetIntValueForCustomFieldOrDefaultValue( "timeIntervalForAnalyzer", true ) * 60; // Config value is in minutes, faction works via GameSeconds.
            AIPIncrease = cfg.GetIntValueForCustomFieldOrDefaultValue( "AIPIncrease", true );
            AIPDecrease = cfg.GetIntValueForCustomFieldOrDefaultValue( "AIPDecrease", true );
            AIPIncreaseOnDeath = cfg.GetIntValueForCustomFieldOrDefaultValue( "AIPIncreaseOnDeath", true );
        }
        #endregion

        public int GetNetAIPChangeForThisFiring()
        {
            bool localDebug = false;
            int numAIAnalyzer = 0;
            int numNeutralAnalyzer = 0;
            int numHumanAnalyzer = 0;

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( ANALYZER_TAG ) )
            {
                if ( entity.Planet.GetControllingFactionType() == FactionType.Player )
                    numHumanAnalyzer++;
                else if ( entity.Planet.GetControllingFactionType() == FactionType.AI )
                    numAIAnalyzer++;
                else
                    numNeutralAnalyzer++;
            }
            if ( localDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Analyzers: owned by AI: " + numAIAnalyzer + " owned by humans: " + numHumanAnalyzer + " neutral: " + numNeutralAnalyzer, Verbosity.DoNotShow );
            int increase = numAIAnalyzer * AIPIncrease;
            int decrease = numHumanAnalyzer * AIPDecrease;
            return (increase - decrease);
        }
        public int GetAIPIncreaseOnlyForThisFiring()
        {
            int numAIAnalyzer = 0;
            int numNeutralAnalyzer = 0;
            int numHumanAnalyzer = 0;

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( ANALYZER_TAG ) )
            {
                if ( entity.Planet.GetControllingFactionType() == FactionType.Player )
                    numHumanAnalyzer++;
                else if ( entity.Planet.GetControllingFactionType() == FactionType.AI )
                    numAIAnalyzer++;
                else
                    numNeutralAnalyzer++;
            }
            return (numAIAnalyzer * AIPIncrease);
        }

        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            string value = this.AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue( "NumberToSeed", true );
            if ( value != null )
                buffer.Add( "星系中数量：" ).Add( value );
        }

        public override void SetStartingFactionRelationships()
        {
            //run the base logic first, just in case
            base.SetStartingFactionRelationships();
            //these are friendly with everyone since only the player should interact with them
            AllegianceHelper.AllyThisFactionToEveryoneButPlayers( this.AttachedFaction );
        }

        #region GetRiskAnalyzers_Threadsafe
        public static void GetRiskAnalyzers_Threadsafe( List<SafeSquadWrapper> ListToFill )
        {
            ListToFill.Clear();
            if( Instance == null )
                return;
            foreach ( GameEntity_Squad entity in Instance.AttachedFaction.Squads( ANALYZER_TAG ) )
            {
                ListToFill.Add( entity );
            }
        }
        #endregion

        public ExoData GetExoData() //IExoDataHolder
        {
            return this.exoData;
        }
    }
}
