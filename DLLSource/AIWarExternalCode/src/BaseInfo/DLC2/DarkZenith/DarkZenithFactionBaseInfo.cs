using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithFactionBaseInfo : DarkZenithFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized

        //not serialized
        public static DarkZenithFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        protected override void SubCleanup()
        {
            Instance = null;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 10);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Dark Zenith" );
            return load;
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        public static int GetDZSidekickFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "DarkZenithSidekick", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "DarkZenithEmpire", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            
            return count;
        }
    }
}
