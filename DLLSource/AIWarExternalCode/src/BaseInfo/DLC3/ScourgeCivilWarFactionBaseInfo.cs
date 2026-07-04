using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgeCivilWarFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized
        //Not Serialized
        int Intensity = -1;
        public static ScourgeCivilWarFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        public ScourgeCivilWarFactionBaseInfo()
        {
            Cleanup();
        }
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }
        protected override void Cleanup()
        {
            Intensity = -1;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 40 + (Intensity * 5);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From ScourgeCivilWar" );
            return load;
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            return false;
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 4000;
            if ( Intensity == -1 )
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ScourgeCivilWar DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
