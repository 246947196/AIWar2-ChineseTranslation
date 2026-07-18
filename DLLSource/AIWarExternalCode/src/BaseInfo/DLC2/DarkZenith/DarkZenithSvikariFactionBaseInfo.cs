using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithSvikariFactionBaseInfo : DarkZenithFactionBaseInfoRoot
    {
        //for now just a pure wrapper of the class below

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 10);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " 来自斯维卡里的负载" );
            return load;
        }

        protected override void SubCleanup()
        {
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
        }

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
    }
}
