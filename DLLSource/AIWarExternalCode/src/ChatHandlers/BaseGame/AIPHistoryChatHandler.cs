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
            Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "AIP 鍙樻洿鍘嗗彶", "鍏抽棴",
                delegate ( ArcenDoubleCharacterBuffer Buffer ) { return Window_ResourceBar.tAIP.GetAIPHistory( Buffer ); } );
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            Buffer.Add( "鐐瑰嚮鏌ョ湅 AIP 鍘嗗彶銆? );
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }
    }
}
