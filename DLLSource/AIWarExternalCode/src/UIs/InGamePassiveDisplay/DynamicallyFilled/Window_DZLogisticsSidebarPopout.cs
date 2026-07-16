using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_DZLogisticsSidebarPopout : Window_DynamicallyFilledAbstractBase
    {
        public static Window_DZLogisticsSidebarPopout Instance;
        private bool _open;

        public static readonly List<GameEntity_Squad> TerminiiInWindow =
            List<GameEntity_Squad>.Create_WillNeverBeGCed( 60, "Window_DZLogisticsSidebarPopout-TerminiiInWindow" );

        public static int SelectedTerminusIndex = -1;

        public static readonly List<GameEntity_Squad> TransportsForSelected =
            List<GameEntity_Squad>.Create_WillNeverBeGCed( 60, "Window_DZLogisticsSidebarPopout-TransportsForSelected" );

        public Window_DZLogisticsSidebarPopout()
        {
            Instance = this;
            this.topBuffer = -3;
            this.leftBuffer = 5;
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.rowBuffer = 1.5f;
        }

        #region Open/Close/Toggle
        public override void Close()
        {
            _open = false;
            SelectedTerminusIndex = -1;
        }

        public void Open()
        {
            _open = true;
        }

        public void Toggle()
        {
            if ( _open ) Close();
            else Open();
        }

        public bool GetIsOpen() => _open;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            return _open && ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction );
        }

        public override void OnHideAfterShowing()
        {
            _open = false;
            base.OnHideAfterShowing();
        }
        #endregion

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    Instance.Close();
            }
        }

        private const float W_PLANET = 110f;
        private const float W_RESOURCE = 130f;
        private const float W_INVENTORY = 90f;
        private const float W_INBOUND = 90f;
        private const float W_FULL = 420f;
        private const float W_COLOR_NAME = 110f;
        private const float W_COLOR_COUNT = 60f;
        private const float W_COLOR_PRODUCTION = 150f;
        private const float W_COLOR_BUTTON = 100f;

        //The implicit resource tech tree: Metal is the base resource;
        //White/Blue/Green are refined from Metal; Red is refined from White+Green+Blue; Black is refined from Red.
        private static readonly DZResource[] TerminusTierOrder = { DZResource.Metal, DZResource.White, DZResource.Blue, DZResource.Green, DZResource.Red, DZResource.Black };

        //If non-null at index i, a tier header with this text should be drawn just before AllTerminusResources[i]
        public static readonly List<string> TerminusTierHeaderBeforeIndex = List<string>.Create_WillNeverBeGCed( (int)DZResource.End, "Window_DZLogisticsSidebarPopout-TerminusTierHeaderBeforeIndex" );

        //all the "real" DZ resources, ie not None and not End
        public static readonly List<DZResource> AllTerminusResources = List<DZResource>.Create_WillNeverBeGCed( (int)DZResource.End, "Window_DZLogisticsSidebarPopout-AllTerminusResources" );
        public static readonly Dictionary<DZResource, int> TerminusCountsByResource = Dictionary<DZResource, int>.Create_WillNeverBeGCed( (int)DZResource.End + 1, "Window_DZLogisticsSidebarPopout-TerminusCountsByResource" );
        public static readonly Dictionary<DZResource, int> EpistylesQueuedByResource = Dictionary<DZResource, int>.Create_WillNeverBeGCed( (int)DZResource.End + 1, "Window_DZLogisticsSidebarPopout-EpistylesQueuedByResource" );
        public static readonly Dictionary<DZResource, int> TerminiiBuildingByResource = Dictionary<DZResource, int>.Create_WillNeverBeGCed( (int)DZResource.End + 1, "Window_DZLogisticsSidebarPopout-TerminiiBuildingByResource" );

        private ArcenCachedExternalTypeDirect type_tHeaderSummary =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tHeaderSummary ) );
        private ArcenCachedExternalTypeDirect type_tTierHeader =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTierHeader ) );
        private ArcenCachedExternalTypeDirect type_tTerminusCostNote =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTerminusCostNote ) );
        private ArcenCachedExternalTypeDirect type_tColorName =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tColorName ) );
        private ArcenCachedExternalTypeDirect type_tColorCount =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tColorCount ) );
        private ArcenCachedExternalTypeDirect type_tColorProduction =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tColorProduction ) );
        private ArcenCachedExternalTypeDirect type_bRequestTerminus =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bRequestTerminus ) );
        private ArcenCachedExternalTypeDirect type_tTerminusPlanet =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTerminusPlanet ) );
        private ArcenCachedExternalTypeDirect type_tTerminusResource =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTerminusResource ) );
        private ArcenCachedExternalTypeDirect type_bSelectTerminus =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bSelectTerminus ) );
        private ArcenCachedExternalTypeDirect type_tTransportDetail =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTransportDetail ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;

            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null ) { this.Close(); return; }

            DarkZenithSidekickFactionBaseInfo dzInfo = localFaction.TryGetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            if ( dzInfo == null ) { this.Close(); return; }

            float runningY = topBuffer;
            Rect leftBounds;

            #region Build Terminii list
            TerminiiInWindow.Clear();
            List<SafeSquadWrapper> terminiiList = dzInfo.Terminii.GetDisplayList();
            for ( int i = 0; i < terminiiList.Count; i++ )
            {
                GameEntity_Squad terminus = terminiiList[i].GetSquad();
                if ( terminus == null ) continue;
                TerminiiInWindow.Add( terminus );
            }
            #endregion

            #region Header summary row
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
            AddText( Set, type_tHeaderSummary, string.Empty, -1, -1, leftBounds, 11f );
            #endregion

            #region Terminus color summary rows
            AllTerminusResources.Clear();
            TerminusTierHeaderBeforeIndex.Clear();
            TerminusCountsByResource.Clear();
            EpistylesQueuedByResource.Clear();
            TerminiiBuildingByResource.Clear();
            for ( int i = 0; i < TerminusTierOrder.Length; i++ )
            {
                DZResource res = TerminusTierOrder[i];
                AllTerminusResources.Add( res );
                string tierHeader = null;
                if ( res == DZResource.Metal ) tierHeader = "第1阶 - 原矿石";
                else if ( res == DZResource.White ) tierHeader = "第2阶 - 试剂";
                else if ( res == DZResource.Red ) tierHeader = "第3阶 - 化合物";
                else if ( res == DZResource.Black ) tierHeader = "第4阶 - 精萃";
                TerminusTierHeaderBeforeIndex.Add( tierHeader );
                TerminusCountsByResource[res] = 0;
                EpistylesQueuedByResource[res] = 0;
                TerminiiBuildingByResource[res] = 0;
            }
            for ( int i = 0; i < TerminiiInWindow.Count; i++ )
            {
                DarkZenithPerUnitBaseInfo tData = TerminiiInWindow[i].TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( tData == null ) continue;
                if ( !TerminusCountsByResource.ContainsKey( tData.Resource ) ) continue;
                TerminusCountsByResource[tData.Resource]++;
            }

            //Epistyles whose current conversion will build a Terminus of a given resource color
            List<SafeSquadWrapper> epistylesForCounting = dzInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistylesForCounting.Count; i++ )
            {
                GameEntity_Squad ep = epistylesForCounting[i].GetSquad();
                if ( ep == null ) continue;
                DarkZenithPerUnitBaseInfo epd = ep.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                DZResourceConversion conv = epd?.NextConversion;
                if ( conv == null || conv.NameForUnit != "WarpingInDZTerminus" || conv.RelatedResource == DZResource.None ) continue;
                if ( !EpistylesQueuedByResource.ContainsKey( conv.RelatedResource ) ) continue;
                EpistylesQueuedByResource[conv.RelatedResource]++;
            }

            //Constructors actively building a Terminus, plus Terminii that are already warping in
            List<SafeSquadWrapper> constructorsForCounting = dzInfo.Constructors.GetDisplayList();
            for ( int i = 0; i < constructorsForCounting.Count; i++ )
            {
                GameEntity_Squad c = constructorsForCounting[i].GetSquad();
                if ( c == null ) continue;
                DarkZenithPerUnitBaseInfo cData = c.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( cData?.Unit == null || !cData.Unit.GetHasTag( "WarpingInDZTerminus" ) || cData.Resource == DZResource.None ) continue;
                if ( !TerminiiBuildingByResource.ContainsKey( cData.Resource ) ) continue;
                TerminiiBuildingByResource[cData.Resource]++;
            }
            List<SafeSquadWrapper> warpingInForCounting = dzInfo.WarpingInTerminii.GetDisplayList();
            for ( int i = 0; i < warpingInForCounting.Count; i++ )
            {
                GameEntity_Squad w = warpingInForCounting[i].GetSquad();
                if ( w == null ) continue;
                DarkZenithPerUnitBaseInfo wData = w.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( wData == null || wData.Resource == DZResource.None ) continue;
                if ( !TerminiiBuildingByResource.ContainsKey( wData.Resource ) ) continue;
                TerminiiBuildingByResource[wData.Resource]++;
            }

            this.rowHeight = ROW_HEIGHT_DEFAULT;
            for ( int i = 0; i < AllTerminusResources.Count; i++ )
            {
                if ( TerminusTierHeaderBeforeIndex[i] != null )
                {
                    Rect headerBounds;
                    CalculateBoundsSingle( out headerBounds, ref runningY, W_FULL );
                    AddText( Set, type_tTierHeader, string.Empty, i, -1, headerBounds, 11f );
                }

                float rowX = leftBuffer;
                Rect nameBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_COLOR_NAME, rowHeight );
                rowX += W_COLOR_NAME;
                Rect countBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_COLOR_COUNT, rowHeight );
                rowX += W_COLOR_COUNT;
                Rect productionBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_COLOR_PRODUCTION, rowHeight );
                rowX += W_COLOR_PRODUCTION;
                Rect buttonBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_COLOR_BUTTON, rowHeight );
                runningY += rowHeight + rowBuffer;

                AddText( Set, type_tColorName, string.Empty, i, -1, nameBounds, 10f );
                AddText( Set, type_tColorCount, string.Empty, i, -1, countBounds, 10f );
                AddText( Set, type_tColorProduction, string.Empty, i, -1, productionBounds, 10f );
                AddButton( Set, type_bRequestTerminus, string.Empty, i, -1, buttonBounds, 10f );

                DZResource resource = AllTerminusResources[i];
                if ( resource == DZResource.Red || resource == DZResource.Black )
                {
                    Rect costBounds;
                    CalculateBoundsSingle( out costBounds, ref runningY, W_FULL );
                    AddText( Set, type_tTerminusCostNote, string.Empty, i, -1, costBounds, 10f );
                }
            }
            #endregion

            #region Per-Terminus rows
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            for ( int i = 0; i < TerminiiInWindow.Count; i++ )
            {
                float rowX = leftBuffer;
                Rect planetBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_PLANET, rowHeight );
                rowX += W_PLANET;
                Rect resourceBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_RESOURCE, rowHeight );
                rowX += W_RESOURCE;
                Rect inventoryBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_INVENTORY, rowHeight );
                rowX += W_INVENTORY;
                Rect inboundBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_INBOUND, rowHeight );
                runningY += rowHeight + rowBuffer;

                AddText( Set, type_tTerminusPlanet, string.Empty, i, -1, planetBounds, 10f );
                AddButton( Set, type_bSelectTerminus, string.Empty, i, -1, resourceBounds, 10f );
                AddText( Set, type_tTerminusResource, string.Empty, i, 0, inventoryBounds, 10f );
                AddText( Set, type_tTerminusResource, string.Empty, i, 1, inboundBounds, 10f );

                if ( SelectedTerminusIndex == i )
                {
                    GameEntity_Squad selTerminus = TerminiiInWindow[i];
                    TransportsForSelected.Clear();
                    List<SafeSquadWrapper> transports = dzInfo.Transports.GetDisplayList();
                    for ( int ti = 0; ti < transports.Count; ti++ )
                    {
                        GameEntity_Squad ship = transports[ti].GetSquad();
                        if ( ship == null ) continue;
                        DarkZenithPerUnitBaseInfo tData = ship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                        if ( tData == null ) continue;
                        if ( tData.Destination == selTerminus || tData.SecondaryDestination == selTerminus )
                            TransportsForSelected.Add( ship );
                    }

                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    if ( TransportsForSelected.Count == 0 )
                    {
                        Rect noneBounds;
                        CalculateBoundsSingle( out noneBounds, ref runningY, W_FULL );
                        AddText( Set, type_tTransportDetail, string.Empty, i, -1, noneBounds, 10f );
                    }
                    else
                    {
                        for ( int ti = 0; ti < TransportsForSelected.Count; ti++ )
                        {
                            Rect detailBounds;
                            CalculateBoundsSingle( out detailBounds, ref runningY, W_FULL );
                            AddText( Set, type_tTransportDetail, string.Empty, i, ti, detailBounds, 10f );
                        }
                    }
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                }
            }
            #endregion

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        //Finds the "Build X Terminus" conversion for this faction's allegiance, so we can show its resource cost
        private static DZResourceConversion FindTerminusConversionForResource( DarkZenithFactionBaseInfoRoot dzBaseInfo, DZResource resource )
        {
            DarkZenithResourceConversionTable table = DarkZenithResourceConversionTable.Instance;
            for ( int i = 0; i < table.Rows.Count; i++ )
            {
                DZResourceConversion row = table.Rows[i];
                if ( row.NameForUnit != "WarpingInDZTerminus" || row.RelatedResource != resource )
                    continue;
                if ( dzBaseInfo.PlayerAllied && !row.ForHumanAllied ||
                     dzBaseInfo.MinorFactionAllied && !row.ForMinorFactionAllied ||
                     dzBaseInfo.AIAllied && !row.ForAIAllied ||
                     dzBaseInfo.IsPlayer && !row.ForPlayer ||
                     !dzBaseInfo.IsPlayer && row.ForPlayer ||
                     !dzBaseInfo.MinorFactionAllied && row.ForMinorFactionAllied ||
                     !dzBaseInfo.AIAllied && row.ForAIAllied ||
                     !dzBaseInfo.PlayerAllied && row.ForHumanAllied )
                    continue;
                return row;
            }
            return null;
        }

        #region Inner UI classes

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<b>暗天顶后勤</b>", "a1d4ff" );
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<color=#999999>关闭</color>" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class tHeaderSummary : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithSidekickFactionBaseInfo dzInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                int transportCount = dzInfo != null ? dzInfo.Transports.Count : 0;

                Buffer.Add( "终点站：" ).Add( TerminiiInWindow.Count )
                      .Add( "   运输船：" ).Add( transportCount )
                      .Add( "  （点击终点站查看入站运输船）", "888888" );
            }
        }

        public class tTierHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= TerminusTierHeaderBeforeIndex.Count ) return;
                string header = TerminusTierHeaderBeforeIndex[idx];
                if ( header == null ) return;
                Buffer.Add( "<b>" + header + "</b>", "ffd27f" );
            }
        }

        public class tTerminusCostNote : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) return;
                DZResource resource = AllTerminusResources[idx];

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
                if ( dzBaseInfo == null ) return;

                DZResourceConversion conv = FindTerminusConversionForResource( dzBaseInfo, resource );
                if ( conv == null ) return;

                Buffer.Add( "\t还需：", "888888" );
                bool first = true;
                foreach ( KeyValuePair<DZResource, int> kv in conv.Cost )
                {
                    if ( kv.Value <= 0 || kv.Key == DZResource.Metal || kv.Key == DZResource.None ) continue;
                    if ( !first ) Buffer.Add( ", ", "888888" );
                    Buffer.Add( kv.Value.ToString() + " " + DarkZenithFactionBaseInfoRoot.ResourceFancyName[kv.Key], DarkZenithFactionBaseInfoRoot.ResourceColour[kv.Key] );
                    first = false;
                }
                if ( first )
                    Buffer.Add( "无需其他", "888888" );
            }
        }

        public class tColorName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) return;
                DZResource resource = AllTerminusResources[idx];
                string col = DarkZenithFactionBaseInfoRoot.ResourceColour != null ? DarkZenithFactionBaseInfoRoot.ResourceColour[resource] : "cddcdc";
                if ( DarkZenithFactionBaseInfoRoot.ResourceFancyName != null )
                    Buffer.Add( DarkZenithFactionBaseInfoRoot.ResourceFancyName[resource], col );
                else
                    Buffer.Add( resource.ToString(), col );
            }
        }

        public class tColorCount : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) return;
                DZResource resource = AllTerminusResources[idx];

                int count = 0;
                TerminusCountsByResource.TryGetValue( resource, out count );
                Buffer.Add( count.ToString(), count > 0 ? "cddcdc" : "666666" );
            }
        }

        public class tColorProduction : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) return;
                DZResource resource = AllTerminusResources[idx];

                int building = 0;
                TerminiiBuildingByResource.TryGetValue( resource, out building );
                int queued = 0;
                EpistylesQueuedByResource.TryGetValue( resource, out queued );

                if ( building == 0 && queued == 0 )
                {
                    Buffer.Add( "无队列", "666666" );
                    return;
                }

                bool first = true;
                if ( building > 0 )
                {
                    Buffer.Add( building.ToString(), "a1d4ff" ).Add( " 建造中", "888888" );
                    first = false;
                }
                if ( queued > 0 )
                {
                    if ( !first ) Buffer.Add( ", " );
                    Buffer.Add( queued.ToString(), "a1ffa1" ).Add( " 已队列", "888888" );
                }
            }
        }

        public class bRequestTerminus : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) { Buffer.Add( "?" ); return; }
                DZResource resource = AllTerminusResources[idx];

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();

                bool isRequested = dzBaseInfo != null && dzBaseInfo.RequestedTerminusResource == resource;

                if ( isRequested )
                    Buffer.Add( "取消：下一个建造", "ffffa1" );
                else
                    Buffer.Add( "下一个建造", "a1d4ff" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= AllTerminusResources.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                DZResource resource = AllTerminusResources[idx];

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.RequestTerminusBuild], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedMagnitude = (int)resource;
                Faction localFactionToQueue = World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull();
                command.RelatedFactionIndex = localFactionToQueue.FactionIndex;
                World_AIW2.Instance.QueueGameCommand( localFactionToQueue, command, true );
                return MouseHandlingResult.None;
            }
        }

        public class tTerminusPlanet : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= TerminiiInWindow.Count ) return;
                GameEntity_Squad terminus = TerminiiInWindow[idx];
                if ( terminus == null ) return;
                Buffer.Add( terminus.Planet?.Name ?? "???", "8ab4cc" );
            }
        }

        public class bSelectTerminus : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= TerminiiInWindow.Count ) { Buffer.Add( "?" ); return; }
                GameEntity_Squad terminus = TerminiiInWindow[idx];
                if ( terminus == null ) { Buffer.Add( "?" ); return; }
                DarkZenithPerUnitBaseInfo tData = terminus.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                bool isSelected = SelectedTerminusIndex == idx;
                string col = ( tData != null && DarkZenithFactionBaseInfoRoot.ResourceColour != null ) ? DarkZenithFactionBaseInfoRoot.ResourceColour[tData.Resource] : "cddcdc";
                if ( isSelected ) Buffer.Add( "▶ ", "ffffa1" );
                if ( tData != null && DarkZenithFactionBaseInfoRoot.ResourceFancyName != null )
                    Buffer.Add( DarkZenithFactionBaseInfoRoot.ResourceFancyName[tData.Resource], col );
                else
                    Buffer.Add( "终点站", col );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= TerminiiInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                SelectedTerminusIndex = ( SelectedTerminusIndex == idx ) ? -1 : idx;
                return MouseHandlingResult.None;
            }
        }

        public class tTerminusResource : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                int column = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2;
                if ( idx < 0 || idx >= TerminiiInWindow.Count ) return;
                GameEntity_Squad terminus = TerminiiInWindow[idx];
                if ( terminus == null ) return;
                DarkZenithPerUnitBaseInfo tData = terminus.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( tData == null ) return;

                if ( column == 0 )
                {
                    //Inventory of the terminus's primary resource
                    int have = 0;
                    tData.Inventory.TryGetValue( tData.Resource, out have );
                    string col = DarkZenithFactionBaseInfoRoot.ResourceColour != null ? DarkZenithFactionBaseInfoRoot.ResourceColour[tData.Resource] : "cddcdc";
                    Buffer.Add( have, col );
                }
                else
                {
                    //Inbound transport count
                    DarkZenithSidekickFactionBaseInfo dzInfo = terminus.GetFactionOrNull_Safe()?.TryGetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                    if ( dzInfo == null ) return;
                    int inboundCount = 0;
                    List<SafeSquadWrapper> transports = dzInfo.Transports.GetDisplayList();
                    for ( int ti = 0; ti < transports.Count; ti++ )
                    {
                        GameEntity_Squad ship = transports[ti].GetSquad();
                        if ( ship == null ) continue;
                        DarkZenithPerUnitBaseInfo shipData = ship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                        if ( shipData == null ) continue;
                        if ( shipData.Destination == terminus || shipData.SecondaryDestination == terminus )
                            inboundCount++;
                    }
                    if ( inboundCount > 0 )
                        Buffer.Add( inboundCount ).Add( " 运输中", "a1d4ff" );
                    else
                        Buffer.Add( "-", "666666" );
                }
            }
        }

        public class tTransportDetail : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int terminusIdx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                int transportIdx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2;
                if ( terminusIdx < 0 || terminusIdx >= TerminiiInWindow.Count ) return;
                GameEntity_Squad terminus = TerminiiInWindow[terminusIdx];
                if ( terminus == null ) return;

                if ( transportIdx < 0 || transportIdx >= TransportsForSelected.Count )
                {
                    Buffer.Add( "\t目前没有运输船前往此地。", "888888" );
                    return;
                }

                GameEntity_Squad ship = TransportsForSelected[transportIdx];
                if ( ship == null ) return;
                DarkZenithPerUnitBaseInfo shipData = ship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( shipData == null ) return;

                Buffer.Add( "\t- " );
                if ( ship.Planet == terminus.Planet )
                    Buffer.Add( "本星球上的运输船", "8ab4cc" );
                else
                    Buffer.Add( "在 " ).Add( ship.GetPlanetName_Safe(), "066006" ).Add( " 上的运输船" );

                if ( shipData.SecondaryDestination == terminus && shipData.Destination != terminus )
                    Buffer.Add( "（途经此处）", "888888" );

                if ( shipData.Inventory != null && DarkZenithFactionBaseInfoRoot.ResourceColour != null )
                {
                    Buffer.Add( "  运载 " );
                    bool first = true;
                    foreach ( var kv in shipData.Inventory )
                    {
                        if ( kv.Value <= 0 ) continue;
                        if ( !first ) Buffer.Add( ", " );
                        Buffer.Add( kv.Value ).Add( " " ).Add( DarkZenithFactionBaseInfoRoot.ResourceFancyName[kv.Key], DarkZenithFactionBaseInfoRoot.ResourceColour[kv.Key] );
                        first = false;
                    }
                    if ( first )
                        Buffer.Add( "空", "666666" );
                }
            }
        }

        #endregion
    }
}
