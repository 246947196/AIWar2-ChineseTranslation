using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIPHistoryChatHandler : NoDataChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "AIP Change History", "Close",
                delegate ( ArcenDoubleCharacterBuffer Buffer ) { return Window_ResourceBar.tAIP.GetAIPHistory( Buffer ); } );
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.Add( "Click here to view the AIP history." );
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
    }
}
