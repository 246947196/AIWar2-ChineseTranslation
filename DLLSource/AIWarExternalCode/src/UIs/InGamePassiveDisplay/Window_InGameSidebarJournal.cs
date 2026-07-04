using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarJournal : Window_InGameSidebarBase
    {
        public void MarkAllRead()
        {
            JournalEntryInCampaign entry;
            for ( int i = 0; i < World_AIW2.Instance.JournalHistory.Count; i++ )
            {
                entry = World_AIW2.Instance.JournalHistory[i];
                if ( entry == null )
                    continue;
                if ( entry.IsTipRatherThanJournalEntry )
                    continue;
                if ( entry.CalculateIsNewForLocalPlayer() )
                    EndpointFunctions.MarkJournalEntryAsReadForLocal( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), entry );    
            }
        }
        
        public int GetCountOfNewNonTipJournalEntries()
        {
            int newEntryCount = 0;
            JournalEntryInCampaign entry;
            for ( int i = 0; i < World_AIW2.Instance.JournalHistory.Count; i++ )
            {
                entry = World_AIW2.Instance.JournalHistory[i];
                if ( entry == null )
                    continue;
                if ( entry.IsTipRatherThanJournalEntry )
                    continue;
                if ( entry.CalculateIsNewForLocalPlayer() )
                    newEntryCount++;
            }
            return newEntryCount;
        }

        public static Window_InGameSidebarJournal Instance;
        public Window_InGameSidebarJournal()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Journal;
        }

        private static ButtonAbstractBase.ButtonPool<btnJournalItem> btnJournalItemPool;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarJournal.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                AdjustHeightToScreenMax( 60, "NotificationsScale", "SidebarScale", "ResourceBarScale", Window_InGameSidebarShips.CustomParentInstance,
                    Window_InGameSidebarFleets.CustomParentInstance, Window_InGameSidebarDirectBuild.CustomParentInstance,
                    Window_InGameSidebarScience.CustomParentInstance, Window_InGameSidebarHacking.CustomParentInstance,
                    Window_InGameSidebarOutguard.CustomParentInstance, Window_InGameSidebarObjectives.CustomParentInstance,
                    Window_InGameSidebarJournal.CustomParentInstance, Window_InGameSidebarTips.CustomParentInstance );

                if ( Window_InGameSidebarJournal.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnJournalItem.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnJournalItemPool = new ButtonAbstractBase.ButtonPool<btnJournalItem>( btnJournalItem.Original, 10 );
                        }
                    }
                    #endregion
                }

                float currentY = -4.6f; //the position of the first entry
                
                this.OnUpdateJournal( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnJournalItem.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public const float TEXT_ROW_HEIGHTS = 25f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            #region OnUpdateJournal
            public void OnUpdateJournal( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;

                btnJournalItemPool.Clear( 5 );
                int addedJournalEntries = 0;
                {
                    JournalEntryInCampaign entry;
                    //do them in reverse order, so newest is at top
                    for ( int i = World_AIW2.Instance.JournalHistory.Count - 1; i >= 0; i-- )
                    {
                        entry = World_AIW2.Instance.JournalHistory[i];
                        if ( entry.IsTipRatherThanJournalEntry )
                            continue;
                        addedJournalEntries++;
                        btnJournalItem item = btnJournalItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( entry );
                    }
                }

                //if no entries, show a message to that effect
                if ( addedJournalEntries <= 0 )
                {
                    btnJournalItem item = btnJournalItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        item.Assign( "暂无条目！" );
                }

                #region Positioning Logic
                {
                    btnJournalItemPool.ApplyItemsInRows( 0, ref currentY, 34f, 180, 32f );
                }
                #endregion
            }
            #endregion            
        }

        #region btnJournalItem
        public class btnJournalItem : ButtonAbstractBase
        {
            public static btnJournalItem Original;
            public btnJournalItem() { if ( Original == null ) Original = this; }

            private string RawMessage = string.Empty;
            private JournalEntryInCampaign CampaignEntry;

            public void Assign( string RawMessage )
            {
                this.RawMessage = RawMessage;
                this.CampaignEntry = null;
            }

            public void Assign( JournalEntryInCampaign JournalEntry )
            {
                this.RawMessage = string.Empty;
                this.CampaignEntry = JournalEntry;
            }
            
            public override bool GetShouldBeHidden()
            {
                return ( this.RawMessage.Length <= 0 && this.CampaignEntry== null );
            }

            public override void Clear()
            {
                this.RawMessage = string.Empty;
                this.CampaignEntry = null;
            }

            private bool hadCheckedForRelatedObjects = false;
            private TMPro.TextMeshProUGUI TimeText = null;
            private TMPro.TextMeshProUGUI NewText = null;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( !hadCheckedForRelatedObjects )
                {
                    this.hadCheckedForRelatedObjects = true;
                    this.TimeText = this.Element.RelatedObjects[0].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    this.NewText = this.Element.RelatedObjects[1].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                }

                JournalEntry entry = this.CampaignEntry == null ? null : JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( this.CampaignEntry.UniqueID );

                if ( entry == null )
                {
                    buffer.Add( this.RawMessage );
                    if ( this.TimeText )
                        this.TimeText.text = string.Empty;
                    if ( this.NewText )
                        this.NewText.text = string.Empty;
                }
                else
                {
                    buffer.Add( entry.DoLocalTextReplacements( this.CampaignEntry, entry.SidebarText ) );
                    if ( this.TimeText )
                        this.TimeText.text = Engine_Universal.ToHoursAndMinutesString( this.CampaignEntry.GameSecondLogged );
                    if ( this.NewText )
                        this.NewText.text = this.CampaignEntry.CalculateIsNewForLocalPlayer() ? "新！" : string.Empty;
                }
            }
            
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                JournalEntry entry = this.CampaignEntry == null ? null : JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( this.CampaignEntry.UniqueID );

                if ( entry == null )
                {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( input.LeftButtonClicked )
                {
                    {
                        ArcenExternalCodeHook journalOpenedHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "OnJournalOpened" );
                        if ( journalOpenedHook != null )
                            journalOpenedHook.HandleAllSubscribedHooks( this.CampaignEntry, entry, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        else
                            ArcenDebugging.ArcenDebugLog( "Could not find journalOpenedHook", Verbosity.ShowAsError );
                    }

                    //show the full text if you left-click
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, delegate
                    {
                        ArcenExternalCodeHook journalClosedHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "OnJournalClosed" );
                        if ( journalClosedHook != null )
                            journalClosedHook.HandleAllSubscribedHooks( this.CampaignEntry, entry, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        else
                            ArcenDebugging.ArcenDebugLog( "Could not find journalClosedHook", Verbosity.ShowAsError );
                    }, entry.DoLocalTextReplacements( this.CampaignEntry, entry.SidebarText ),
                        entry.DoLocalTextReplacements( this.CampaignEntry, entry.FullText ), "确定" );
                    //if it was new, it should not be new now!
                    EndpointFunctions.MarkJournalEntryAsReadForLocal( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), this.CampaignEntry );
                }
                else if ( input.RightButtonClicked )
                {
                    // Right click to dismiss newness
                    EndpointFunctions.MarkJournalEntryAsReadForLocal( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), this.CampaignEntry );
                }
                else
                {
                    //any other click, attempt to center on the stuff
                    this.CampaignEntry.CenterViewOnThisBasedOnData();
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                JournalEntry entry = this.CampaignEntry == null ? null : JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( this.CampaignEntry.UniqueID );

                if ( entry == null )
                    return;
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "左键查看详情。右键可将视角居中到本日志条目讨论的目标（如果存在）。", "GeneralTooltipScale" );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int newCount = Instance.GetCountOfNewNonTipJournalEntries();
                if ( newCount > 0 )
                    Buffer.Add( "日志 (<color=#31ffdf>" ).Add( newCount ).Add( " 新</color>)" );
                else
                    Buffer.Add( "日志" );
            }
        }
        #endregion
    }

    public class ExampleHandler_AnyJournalEvent : IArcenExternalCodeHookHandler
    {
        public void HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects, ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            JournalEntryInCampaign campaignEntry = MainObject as JournalEntryInCampaign;
            JournalEntry entry = SecondaryObject as JournalEntry;

            //you can switch on Hook.InternalName to handle multiple event types with one IArcenExternalCodeHookHandler, if you want

            //uncomment to display which event you just triggered:
            //ArcenDebugging.ArcenDebugLog( Hook.InternalName + ":\n" + entry.DoLocalTextReplacements( campaignEntry, entry.FullText ), Verbosity.Chat );
        }
    }
}
