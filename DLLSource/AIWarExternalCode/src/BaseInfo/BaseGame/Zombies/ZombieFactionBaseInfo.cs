using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class ZombieFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized

        //not serialized

        //constants

        public ZombieFactionBaseInfo()
        {
            Cleanup();
        }
        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are always here, don't tell us about this
            return 0;
        }

        protected sealed override void Cleanup()
        {
            this.SubCleanup();
        }

        protected abstract void SubCleanup();

        public sealed override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubSerializeFactionTo( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubDeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant for zombies
        }

        protected sealed override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            this.SubDoGeneralAggregationsPausedOrUnpaused();
        }
        protected abstract void SubDoGeneralAggregationsPausedOrUnpaused();

        public sealed override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            this.SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
        }
        protected abstract void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context );
    }
}
