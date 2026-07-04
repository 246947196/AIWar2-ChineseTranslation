using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class EnergyUsageChatHandler : FactionViewChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            Faction fac = this.Faction.GetFaction();
            if ( fac == null )
                return;
            Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.5f, 2f, fac.GetDisplayName() + " 的能源生产与消耗", "关闭",
                    delegate ( ArcenDoubleCharacterBuffer Buffer ) { return Window_ResourceBar.tEnergy.GetEnergyData( Buffer ); } );
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            Faction fac = this.Faction.GetFaction();
            Buffer.Add( "点击查看 " ).Add( fac?.GetDisplayName() ).Add( " 的能源生产与消耗" );
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFactionIndex_Neg1ToPos( MetaData, this.Faction.GetFactionIndex(), "Faction" );
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Faction = LazyLoadFactionWrapper.Create( Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "Faction" ), true, "EnergyUsageChatHandler" );
        }
    }
}
