using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public enum InGameSidebarType
    {
        Ships,
        Fleets,
        DirectBuild,
        Science,
        Hacking,
        Outguard,
        Objectives,
        Journal,
        Tips,
        Closed
    }

    public class Window_InGameSidebarBase : WindowControllerAbstractBase
    {
        public static InGameSidebarType Current = InGameSidebarType.Ships;

        public class headerParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            public override void OnUpdate()
            {
                this.WindowController.myYPositionScale = GameSettings.Current.GetFloatBySetting( "ResourceBarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                //make sure the back bar of the tabs on the left stays tall enough as it gets smaller
                this.Element.RelevantRect.UI_SetHeight( 530 * Mathf.Max( 1f, Screen.height / 400 ) );

                //this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
            }

            public override void OnMainThreadUpdate()
            {
                
            }
        }

        public class tabShips : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Ships)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Ships;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("在本地星球",0,"OpenShipsTab",null);
            }
            
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Ships;
            }
        }

        public class tabFleets : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Fleets)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Fleets;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            
            private const string _desc = "此标签页用于管理你的舰队，左键点击舰队会打开舰队管理窗口。如果你想在此界面选择舰队，可以右键点击它。";
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("所有你的舰队",0,"OpenFleetsTab",_desc);
            }
            
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Fleets;
            }
        }

        public class tabBuild : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.DirectBuild)
                    Window_InGameSidebarBase.Current = InGameSidebarType.DirectBuild;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("直接建造",0,"OpenBuildTab",null);
            }
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.DirectBuild;
            }
        }

        public class tabTech : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Science)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Science;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("科技解锁",0,"OpenTechTab",null);
            }
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Science;
            }
        }

        public class tabHack : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Hacking)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Hacking;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("入侵星球目标",0,"OpenHackingTab","入侵通过旗舰在同星球执行，除非另有说明。一些非常强大的入侵需要在舰队管理窗口的舰队标签页中对你的舰队进行操作。");
            }
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Hacking;
            }
        }

        public class tabIntel : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Objectives)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Objectives;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover() 
            { 
                base.HandleTooltip("情报报告",ObjectiveCategoryTable.GetObjectiveCount(),"OpenObjectivesTab",null);
            }
            
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Objectives;
            }
            
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( ObjectiveCategoryTable.GetObjectiveCount() );
            }
        }

        public class tabJournal : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                /*
                if (input.RightButtonClicked && Window_InGameSidebarJournal.Instance.GetCountOfNewNonTipJournalEntries() > 0)
                {
                    LOG.Chat("Marking all Journals Read");
                    EndpointFunctions.MarkAllJournalAsReadForLocal();
                    
                    return MouseHandlingResult.None;
                }
                */
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Journal)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Journal;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                base.HandleTooltip("战役日志",Window_InGameSidebarJournal.Instance.GetCountOfNewNonTipJournalEntries(),"OpenJournalTab",null);
            }

            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Journal;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int newJournalCount = Window_InGameSidebarJournal.Instance.GetCountOfNewNonTipJournalEntries();
                if ( newJournalCount > 0 )
                {
                    Buffer.Add( "<color=#ffe325>" ).Add( newJournalCount ).Add("</color>");
                }
                else
                {
                    Buffer.Add( "-" );
                }
            }
        }

        public class tabTips : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.GetIsSelected() )
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Tips;
                
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                base.HandleTooltip("游戏玩法和策略提示", Window_InGameSidebarTips.Instance.GetCountOfNewTips(), "OpenTipsTab",null );
            }
            
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Tips;
            }
            
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int newTipsCount = Window_InGameSidebarTips.Instance.GetCountOfNewTips();
                if ( newTipsCount > 0 )
                {
                    switch ( ArcenTime.CycleIndex_4_EveryQuarterSecond )
                    {
                        case 0:
                            Buffer.StartColor( "ffcd36" );
                            break;
                        case 1:
                            Buffer.StartColor( "ffba36" );
                            break;
                        case 2:
                            Buffer.StartColor( "ff7836" );
                            break;
                        case 3:
                            Buffer.StartColor( "fa5a52" );
                            break;
                    }

                    Buffer.Add( newTipsCount );
                }
                else
                {
                    Buffer.Add( "<size=80%>提示</size>" );
                }
            }
        }

        public class tabMercs : SidebarTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_InGameSidebarBase.Current != InGameSidebarType.Outguard)
                    Window_InGameSidebarBase.Current = InGameSidebarType.Outguard;
                else
                    Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
               
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int num_deployable_now = 0;

                World_AIW2.Instance.EnsureSufficientOutguardStates();
                ProtectedList<OutguardInfo> outguardStates = World_AIW2.Instance.OutguardStates;
                for ( int x = 0; x < outguardStates.Count; x++ )
                {
                    OutguardInfo og = outguardStates[x];
                    if ( !og.HasBeenContacted )
                        continue;
                    if ( og.WasKilled )
                        continue;
                    if ( (og.TotalSummonsCapacity - og.TimesSummoned) <= 0 )
                        continue;
                    if ( og.TimeLeftUntilNextSummonInSeconds > 0 )
                        continue;
                    bool inQueue = false;
                    for ( int j = 0; j < OutguardFactionBaseInfo.Instance.QueuedRequests.Count; j++ )
                    {
                        if ( OutguardFactionBaseInfo.Instance.QueuedRequests[j]?.Group == og.GroupData )
                        {
                            inQueue = true;
                            break;
                        }
                    }
                    if ( inQueue )
                        continue;
                    num_deployable_now++;
                }

                if ( num_deployable_now > 0 )
                {
                    Buffer.Add( "<color=#ffe325>" ).Add( num_deployable_now ).Add("</color>");
                }
                else
                {
                    Buffer.Add( "-" );
                }
            }

            public override void HandleMouseover() 
            { 
                base.HandleTooltip("召唤外围守卫",0,"OpenOpsTab",null);
            }
            
            public override bool GetIsSelected()
            {
                return Window_InGameSidebarBase.Current == InGameSidebarType.Outguard;
            }
        }

        public abstract class SidebarTabBase : ButtonAbstractBase
        {
            private static Color unselectedColor = ColorMath.HexToColor( "969696" );
            private static Color selectedColor = ColorMath.HexToColor( "ffffff" );
            private SelectedStatus lastSelected = SelectedStatus.Unknown;
            private HoveredStatus lastHovered = HoveredStatus.Unknown;

            public ArcenUI_CustomUI customUIElement = null;
            public ArcenUI_Button elementAsButton = null;
            public override void OnUpdate()
            {
                if ( elementAsButton == null )
                    this.elementAsButton = (ArcenUI_Button)this.Element;
                if ( customUIElement == null )
                    this.customUIElement = this.elementAsButton.OptionalRelatedCustomUI;

                if ( this.elementAsButton != null )
                {
                    SelectedStatus newSelected = this.GetIsSelected() ? SelectedStatus.Selected : SelectedStatus.NotSelected;
                    HoveredStatus newHovered = this.elementAsButton.LastHadMouseWithin ? HoveredStatus.Hovered : HoveredStatus.NotHovered;
                    //update the background:
                    if ( newSelected != lastSelected )
                    {
                        this.elementAsButton.SetColor( newSelected == SelectedStatus.Selected ? selectedColor : unselectedColor );
                        this.elementAsButton.RelatedImages[0].sprite = this.elementAsButton.RelatedSprites[newSelected == SelectedStatus.Selected ? 1 : 0];
                    }
                    //update the image material:
                    if ( customUIElement != null && ( newSelected != lastSelected || newHovered != lastHovered ) )
                    {
                        var image1 = this.elementAsButton.RelatedImages.Length > 1 ? this.elementAsButton.RelatedImages[1] : null;
                        if ( image1 != null )
                        {
                            int materialIndex = 0; //normal
                            if ( newSelected == SelectedStatus.Selected )
                                materialIndex += 2; //selected
                            if ( newHovered == HoveredStatus.Hovered )
                                materialIndex += 1; //hovered

                            if ( materialIndex < this.customUIElement.RelatedMaterials.Length )
                                image1.material = this.customUIElement.RelatedMaterials[materialIndex];
                        }
                    }

                    lastHovered = newHovered;
                    lastSelected = newSelected;
                }
            }

            public abstract bool GetIsSelected();

            private enum SelectedStatus
            {
                Unknown = 0,
                Selected,
                NotSelected
            }

            private enum HoveredStatus
            {
                Unknown = 0,
                Hovered,
                NotHovered
            }
            
            public void HandleTooltip(string label, int count, string shortcut, string description)
            {
                var buffer = Window_AtMouseTooltipPanelNarrow.TooltipBuffer;
                
                buffer.Open(TextStyle.Sidebar_Button_Tooltip_Header);
                buffer.Add(label);
                if (InputActionTypeDataTable.Instance.IsActionBound(shortcut))
                    buffer.Add(" ").AddVarReplace(TextVarMap.InputAction, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction(shortcut).ToLower());
                buffer.Close(TextStyle.Sidebar_Button_Tooltip_Header);
                
                if (count > 0)
                {
                    //buffer.Add(" ");
                    //buffer.AddVarReplace(TextVarMap.InParenthesis, null, (a,b,c,d)=>c.Add(count), null);
                }
                
                buffer.Open(TextStyle.Sidebar_Button_Tooltip_Body );
                
                //if (!string.IsNullOrEmpty(shortcut))
                //{
                //    buffer.NewLine();
                //    var desc = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction(shortcut);
                //    buffer.Add("open/close ").AddVarReplace(TextVarMap.InputAction, desc);
                //}
                
                //if ( count > 0 )
                //{ 
                //    buffer.NewLine();
                //    buffer.AddVarReplace(TextVarMap.InputAction, "Right Click").Add(" to clear");
                //}
                
                if (!string.IsNullOrEmpty(description))
                {
                    buffer.Add(description);
                }
                
                buffer.Close(TextStyle.Sidebar_Button_Tooltip_Body);
                
                bool changed;
                var text = buffer.GetStringAndResetForNextUpdate(out changed);
                
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, text );
            }
        }
    }
}
