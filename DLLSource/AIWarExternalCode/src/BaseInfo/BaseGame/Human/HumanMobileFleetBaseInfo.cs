using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HumanMobileFleetBaseInfo : FleetMetricsBaseInfo
    {
        public HumanMobileFleetBaseInfo() { Cleanup(); }

        protected override void Cleanup() { base.Cleanup(); }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "HumanMobileFleetBaseInfo" );
            SerializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "HumanMobileFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "HumanMobileFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );
            DeserializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void AddToTooltipForFleet( ArcenCharacterBufferBase buffer, TooltipDetail detailLevel )
        {
            if ( detailLevel >= TooltipDetail.Full )
                AppendFleetMetricsTooltip( buffer, BuildCompositionSnapshot() );
        }
    }
}
