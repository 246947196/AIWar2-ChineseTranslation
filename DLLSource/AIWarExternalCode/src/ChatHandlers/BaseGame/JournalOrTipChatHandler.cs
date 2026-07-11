using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class JournalOrTipChatHandler : StringChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            JournalEntryInCampaign campaignEntry = World_AIW2.Instance.GetJournalEntryInCampaignByUniqueID( this.String );
            JournalEntry entry = JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( this.String );
            if ( campaignEntry == null || entry == null )
                return;

            {
                ArcenExternalCodeHook journalOpenedHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "OnJournalOpened" );
                if ( journalOpenedHook != null )
                    journalOpenedHook.HandleAllSubscribedHooks( campaignEntry, entry, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                else
                    ArcenDebugging.ArcenDebugLog( "Could not find journalOpenedHook", Verbosity.ShowAsError );
            }

            //show the full text
            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, 
                ()=>
                {
                    ArcenExternalCodeHook journalClosedHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "OnJournalClosed" );
                    if ( journalClosedHook != null )
                        journalClosedHook.HandleAllSubscribedHooks( campaignEntry, entry, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                    else
                        ArcenDebugging.ArcenDebugLog( "Could not find journalClosedHook", Verbosity.ShowAsError );
                }, 
                entry.DoLocalTextReplacements( campaignEntry, entry.SidebarText ),
                entry.DoLocalTextReplacements( campaignEntry, entry.FullText ), 
                "确定" );
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            JournalEntryInCampaign campaignEntry = World_AIW2.Instance.GetJournalEntryInCampaignByUniqueID( this.String );
            JournalEntry entry = JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( this.String );

            if ( campaignEntry == null || entry == null )
                Buffer.Add( "无法找到与此相关的日志条目或提示，因此无法点击。" );
            else if ( entry.IsForTheTipsTab )
                Buffer.Add( "点击查看提示：" ).Add( entry.DoLocalTextReplacements( campaignEntry, entry.SidebarText ) );
            else
                Buffer.Add( "点击查看日志条目：" ).Add( entry.DoLocalTextReplacements( campaignEntry, entry.SidebarText ) );
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddString_Condensed( MetaData, this.String, "String" );
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.String = Buffer.ReadString_Condensed( MetaData, "String" );
        }
    }
}
