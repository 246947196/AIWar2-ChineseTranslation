using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TechUpgradeChatHandler : FactionViewChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            Faction fac = this.Faction.GetFaction();
            if ( fac == null )
                return;
            Window_ModalSelfUpdatingTextWindow.Instance.Open( 0.5f, 2f, "History of Tech Unlocks For " + fac.GetDisplayName(), "Close",
                delegate ( ArcenDoubleCharacterBuffer Buffer ) { return Window_ResourceBar.tScience.GetTechHistory( Buffer ); } );
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            Faction fac = this.Faction.GetFaction();
            Buffer.Add( "Click here to view the tech unlock history for " ).Add( fac?.GetDisplayName() );
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFactionIndex_Neg1ToPos( MetaData, this.Faction.GetFactionIndex(), "Faction" );
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Faction = LazyLoadFactionWrapper.Create( Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "Faction" ), true, "TechUpgradeChatHandler" );
        }
    }
}
