using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    // Deprecated. Everything here is for old save compatibility.
    public class DysonSphereAntagonizedFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        public static DysonSphereAntagonizedFactionBaseInfo Instance;

        public DysonSphereAntagonizedFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Instance = null;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this
            return 0;
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {

        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            if ( Buffer.FromGameVersion.GetLessThan( 3, 803 ) ) // Dyson Rework
            {
                Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
                Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            }
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant here
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {

        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {

        }
        #endregion
    }
}
