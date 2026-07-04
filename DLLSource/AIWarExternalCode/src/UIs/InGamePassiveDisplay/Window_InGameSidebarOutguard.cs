using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarOutguard : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarOutguard Instance;
        public Window_InGameSidebarOutguard()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Outguard;
        }

        public static bool OutguardIconsAreOpen = true;
        private static ButtonAbstractBase.ButtonPool<btnMercenary> btnMercenaryPool;
        private static readonly List<OutguardInfo> availableOutguardStates = List<OutguardInfo>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarOutguard-availableOutguardStates" );

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarOutguard.CustomParentInstance = this;
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

                if ( Window_InGameSidebarOutguard.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnMercenary.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnMercenaryPool = new ButtonAbstractBase.ButtonPool<btnMercenary>( btnMercenary.Original, 10 );
                        }
                    }
                    #endregion
                }

                float currentY = 0;

                this.OnUpdateOutguard( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnMercenariesHeader.Instance.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region OnUpdateOutguard
            public void OnUpdateOutguard( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;
                btnMercenaryPool.Clear( 5 );
                if ( !OutguardIconsAreOpen )
                    return;

                availableOutguardStates.Clear();
                World_AIW2.Instance.EnsureSufficientOutguardStates();
                ProtectedList<OutguardInfo> outguardStates = World_AIW2.Instance.OutguardStates;
                for ( int x = 0; x < outguardStates.Count; x++ )
                {
                    OutguardInfo state = outguardStates[x];
                    if ( state.HasBeenContacted && !state.WasKilled )
                    {
                        OutguardGroupData group = state.GroupData;
                        if ( group == null )
                            continue;
                        //one outguard group might be given from many beacons, but only list them once.
                        availableOutguardStates.Add( state );
                    }
                }

                if ( availableOutguardStates.Count <= 0 ) //If this count == 0 then there are no outguardenary groups currently available
                    return;

                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

                for ( int i = 0; i < availableOutguardStates.Count; i++ )
                {
                    OutguardInfo info = availableOutguardStates[i];
                    btnMercenary item = btnMercenaryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    bool anyLeft = true;
                    if ( info.TotalSummonsCapacity - info.TimesSummoned <= 0 )
                        anyLeft = false;
                    bool isWilling = info.TimeLeftUntilNextSummonInSeconds <= 0;
                    if (isWilling)
                        for ( int j = 0; j < OutguardFactionBaseInfo.Instance.QueuedRequests.Count; j++ )
                            if ( OutguardFactionBaseInfo.Instance.QueuedRequests[j]?.Group == info.GroupData )
                            {
                                isWilling = false;
                                break;
                            }
                    item.Assign( info, anyLeft, isWilling, info.GroupData.GroupAllowedToSpawnOnPlanet( planet ) );
                }

                #region Positioning Logic
                //RectTransform rTran = null;
                {
                    //rTran = btnMercenariesHeader.Instance.Element.RelevantRect;
                    currentY -= TEXT_ROW_HEIGHTS;
                    currentY -= 20;

                    if ( OutguardIconsAreOpen )
                    {
                        btnMercenaryPool.ApplyItemsInRows( 0, ref currentY, 69f, 180, 67f );
                    }
                    else
                        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                }
                #endregion
            }
            #endregion
        }

        public const float TEXT_ROW_HEIGHTS = 20f;
        public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

        #region btnMercenariesHeader
        public class btnMercenariesHeader : ButtonAbstractBase
        {
            public static btnMercenariesHeader Instance;
            public btnMercenariesHeader() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( availableOutguardStates.Count > 0 )
                    OutguardIconsAreOpen = !OutguardIconsAreOpen;
                else
                    OutguardIconsAreOpen = true;
                
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( OutguardIconsAreOpen )
                    Buffer.Add( "(-)  " );
                else
                    Buffer.Add( "(+)  " );

                if ( availableOutguardStates.Count > 0 )
                    Buffer.Add( "外围守卫待命 (" ).Add( availableOutguardStates.Count ).Add( ")" );
                else
                    Buffer.Add( "未联络外围守卫" );
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "外围守卫是反AI的散兵游勇，可以被召唤来为你暂时作战。在银河系各处，你会找到外围守卫信标，可以用来联络特定的外围守卫团体。如果你入侵了某个星球上的信标，就可以雇佣另一端的任何外围守卫团体。外围守卫相比普通舰船的两个关键优势是不需要消耗能量，而且可以更快地被召唤。不过，他们会自行行动。" );
            }
        }
        #endregion

        #region btnMercenary
        public class btnMercenary : ButtonAbstractBase
        {
            public static btnMercenary Original;
            public btnMercenary() { if ( Original == null ) Original = this; }

            private OutguardInfo OutguardGroup = null;
            private bool CanSpawnAtThisPlanet = false;
            private bool AnyLeft = false;
            private bool IsWilling = false;

            public void Assign( OutguardInfo Group, bool AnyLeft, bool IsWilling, bool CanSpawnAtThisPlanet )
            {
                this.OutguardGroup = Group;
                this.AnyLeft = AnyLeft;
                this.IsWilling = IsWilling;
                this.CanSpawnAtThisPlanet = CanSpawnAtThisPlanet;
            }

            public override bool GetShouldBeHidden()
            {
                return OutguardGroup == null;
            }

            public override void Clear()
            {
                this.OutguardGroup = null;
                this.AnyLeft = false;
                this.IsWilling = false;
                this.CanSpawnAtThisPlanet = false;
            }

            public override int CompareSelfWithOther( ButtonAbstractBase O )
            {
                try
                {
                    btnMercenary other = (btnMercenary)O;
                    if ( this.OutguardGroup == null )
                        return PREFER_RIGHT;
                    if ( other.OutguardGroup == null )
                        return PREFER_LEFT;

                    if ( !this.CanSpawnAtThisPlanet || !other.CanSpawnAtThisPlanet )
                    {
                        if ( !this.CanSpawnAtThisPlanet )
                        {
                            if ( other.CanSpawnAtThisPlanet )
                                return PREFER_RIGHT;
                        }
                        else
                            return PREFER_LEFT;
                    }

                    return this.OutguardGroup.GroupData.DisplayName.CompareTo( other.OutguardGroup.GroupData.DisplayName );
                }
                catch
                {
                    return PREFER_NEITHER;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( OutguardGroup == null )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    if ( !this.CanSpawnAtThisPlanet || !this.AnyLeft || !this.IsWilling )
                        Buffer.Add( "<color=#ff594c>" );
                    Buffer.Add( "  " + this.OutguardGroup.GroupData.DisplayName );
                    OutguardInfo groupState = this.OutguardGroup;
                    int actualSummons = groupState.TotalSummonsCapacity - groupState.TimesSummoned;
                    // If information is not fully loaded, let the player know. 
                    // This will generally only be for a few seconds, and is usually not noticed, but its important we don't just let the player see invalid values.
                    if ( actualSummons < 0 || groupState.TotalSummonsCapacity < 0 )
                        Buffer.Add( "\n  正在尝试联络。\n  请稍候。" );
                    else
                    {
                        Buffer.Add( "\n  " + actualSummons + "/" + groupState.TotalSummonsCapacity + " 支舰队剩余。" );

                        OutguardSpawnRequest req;
                        for ( int i = 0; i < OutguardFactionBaseInfo.Instance.QueuedRequests.Count; i++ )
                        {
                            req = OutguardFactionBaseInfo.Instance.QueuedRequests[i];
                            if ( req != null && req.Group == groupState.GroupData )
                            {
                                Buffer.Add( "\n  正在前往 " ).AddPlanetNameFormated( req.Planet, false )
                                    .Add( "，预计 " ).AddHoursAndMinutes( req.SpawnSecond - World_AIW2.Instance.GameSecond ).Add( " 后到达。</color>" );
                                return;
                            }
                        }
                        if ( groupState.TimesSummoned == 0 )
                            Buffer.Add( "  <color=#79ff8f>已达到最大舰队容量。</color>" );
                        else if ( groupState.TimeLeftToRepairInSeconds <= 0 )
                            Buffer.Add( "  <color=#7e6efc>当前未准备舰队。</color>" );
                        else
                            Buffer.Add( "  <color=#ffcc7c>下一支在 " ).AddHoursAndMinutes( groupState.TimeLeftToRepairInSeconds ).Add( " 后。</color>" );

                        if ( groupState.TimeLeftUntilNextSummonInSeconds > 0 )
                            Buffer.Add( "\n  <color=#ffba4c>可雇佣在 " ).AddHoursAndMinutes( groupState.TimeLeftUntilNextSummonInSeconds ).Add( " 后。</color>" );
                        else if ( actualSummons <= 0 )
                            Buffer.Add( "\n  <color=#7e71e2>没有可用舰队。" );
                        else
                            Buffer.Add( "\n  <color=#4cff69>已准备就绪。</color>" );
                    }

                    debugStage = 2;

                    debugStage = 15;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in ShipIconImageBase.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.OutguardGroup == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                {
                    EntityText.ShowContents( this.OutguardGroup );
                    return MouseHandlingResult.None;
                }
                #endregion

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                // Stop if not a valid planet.
                if ( !this.CanSpawnAtThisPlanet )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( this.OutguardGroup.GroupData.DisplayName + " 不接受你在此类星球上雇佣他们的请求。他们的指挥官建议你查看他们的合同。", 
                            ChatType.ShowLocallyOnly, null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                // Stop if we don't have any fleets available.
                if ( !this.AnyLeft )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( this.OutguardGroup.GroupData.DisplayName + " 没有可被呼叫的舰队。" +
                                                                    "他们的指挥官想知道你在搞什么鬼。", ChatType.ShowLocallyOnly, null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                // Stop if we're still cooling down since our last attack.
                if ( !this.IsWilling )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( this.OutguardGroup.GroupData.DisplayName + " 不想被榨干，并且根据合同拒绝在接下来的 " +
                                                                    this.OutguardGroup.TimeLeftUntilNextSummonInSeconds + " 秒内被呼叫。" +
                                                                    "他们的指挥官想知道你在搞什么鬼。", ChatType.ShowLocallyOnly, null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( this.OutguardGroup.GroupData.SpawnLocation == OutguardSpawnLocation.Manual_ByClick)
                {
                    Engine_AIW2.Instance.PlacingOutguardDeployable = OutguardGroup.GroupData;
                }
                else
                {
                    if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null )
                        Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                    else
                    {
                        GameCommand_QueueOutguard.QueueCommand( this.OutguardGroup.GroupData, planet,
                            Engine_AIW2.Instance.CombatCenter, localFaction );
                    }
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarOutguard-btnMercenary-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.OutguardGroup == null )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "空类型！", "GeneralTooltipScale" );
                    return;
                }

                this.OutguardGroup.GroupData.WriteTooltipNameBasedOnSidebarSpawn( tooltipBuffer, this.CanSpawnAtThisPlanet, this.AnyLeft, this.IsWilling );
                this.OutguardGroup.GroupData.WriteTooltipInfo( tooltipBuffer );
                this.OutguardGroup.GroupData.WriteTooltipPostfixBasedOnSidebarSpawn( tooltipBuffer, this.CanSpawnAtThisPlanet, this.AnyLeft, this.IsWilling );

                OutguardSpawnRequest req;
                for ( int i = 0; i < OutguardFactionBaseInfo.Instance.QueuedRequests.Count; i++ )
                {
                    req = OutguardFactionBaseInfo.Instance.QueuedRequests[i];
                    if ( req != null && req.Group == OutguardGroup.GroupData )
                    {
                        tooltipBuffer.Add( "\n\n当前正前往 " ) .AddPlanetNameFormated( req.Planet, false )
                            .Add( "，预计 " ).Add( req.SpawnSecond - World_AIW2.Instance.GameSecond ).Add( " 秒后到达。" );
                    }
                }

                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, false, "该外围守卫团体" );

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "GeneralTooltipScale" );
            }
        }
        #endregion
    }
}
