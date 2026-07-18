using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MissingFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized

        //not serialized

        public MissingFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant here
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "1 来自缺失派系的负载" );
            return 1;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            //ConfigurationForFaction cfg = this.AttachedFaction.Config;
            //Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion        
    }
}
