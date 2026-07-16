using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_DZEconomySidebarPopout : Window_DynamicallyFilledAbstractBase
    {
        public static Window_DZEconomySidebarPopout Instance;
        private bool _open;

        public static readonly List<GameEntity_Squad> EpistylesInWindow =
            List<GameEntity_Squad>.Create_WillNeverBeGCed( 60, "Window_DZEconomySidebarPopout-EpistylesInWindow" );

        public static int SelectedEpistyleIndex = -1;

        public static readonly List<DZResourceConversion> ConversionsForSelected =
            List<DZResourceConversion>.Create_WillNeverBeGCed( 60, "Window_DZEconomySidebarPopout-ConversionsForSelected" );

        // Parallel to ConversionsForSelected; null = choosable, non-null = grayed out with this reason
        public static readonly List<string> ConversionsForSelectedBlockReasons =
            List<string>.Create_WillNeverBeGCed( 60, "Window_DZEconomySidebarPopout-ConversionsForSelectedBlockReasons" );

        public static int nOffense, nUtility, nInfra, nUpgrade, nIdle, nStarved;
        public static readonly int[] BottleneckCounts = new int[(int)DZResource.End + 1];

        public Window_DZEconomySidebarPopout()
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
            SelectedEpistyleIndex = -1;
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
        private const float W_CONVERSION = 190f;
        private const float W_LOCK = 58f;
        private const float W_PRIORITY = 62f;
        private const float W_FULL = 420f;
        private const float W_DEPOSIT = 110f;
        private const float CATEGORY_BAR_HEIGHT = 14f;
        private const float W_TIER_ONE_TOGGLE = 160f;

        private const int STARVED_THRESHOLD_SECONDS = 120;

        private static string ConversionColour( DZResourceConversion conv )
        {
            if ( conv == null )           return "666666";
            if ( conv.Upgrade != null )   return "a1ffa1";
            if ( conv.IsOffensive )       return "ffa1a1";
            if ( conv.IsUtility )         return "22a188";
            return "a1a1ff";
        }

        private static bool IsEpistyleTrulyStarved( DarkZenithPerUnitBaseInfo epd )
        {
            if ( epd.NextConversion == null || epd.CanWeDoResourceConversion( epd.NextConversion ) ) return false;
            if ( epd.TimeWeLastDidConversion < 0 ) return true;
            return World_AIW2.Instance.GameSecond - epd.TimeWeLastDidConversion > STARVED_THRESHOLD_SECONDS;
        }

        //Counts the DZ flagships on this epistyle's planet that have resources the epistyle currently
        //wants. Same eligibility the deposit hack uses; drives both the deposit button's label/enable
        //state and (re-evaluated host-side) the actual transfer.
        private static int CountEligibleDepositFlagships( GameEntity_Squad epistyle, DarkZenithPerUnitBaseInfo targetData )
        {
            if ( epistyle == null || epistyle.Planet == null || targetData == null ) return 0;
            Faction faction = epistyle.PlanetFaction.Faction;
            if ( faction == null ) return 0;
            int count = 0;
            foreach ( GameEntity_Squad flagship in faction.Squads( "DarkZenithFlagship" ) )
            {
                if ( flagship == null || flagship.Planet != epistyle.Planet ) continue;
                DarkZenithPerUnitBaseInfo fd = flagship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( fd == null || !fd.HasAnyResourcesAtAll() ) continue;
                if ( !targetData.WantsResourcesFrom( fd ) ) continue;
                count++;
            }
            return count;
        }

        private static string FormatWaitTime( int elapsed )
        {
            if ( elapsed < 0 ) return string.Empty;
            if ( elapsed < 60 ) return elapsed + "s";
            int m = elapsed / 60;
            int s = elapsed % 60;
            return s > 0 ? m + "m" + s + "s" : m + "m";
        }

        private ArcenCachedExternalTypeDirect type_tSummary =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSummary ) );
        private ArcenCachedExternalTypeDirect type_tUpgradeHint =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tUpgradeHint ) );
        private ArcenCachedExternalTypeDirect type_tStarvedLine =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tStarvedLine ) );
        private ArcenCachedExternalTypeDirect type_tEpistylePlanet =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tEpistylePlanet ) );
        private ArcenCachedExternalTypeDirect type_tEpistyleCategoryBar =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tEpistyleCategoryBar ) );
        private ArcenCachedExternalTypeDirect type_bTierOneToggle =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTierOneToggle ) );
        private ArcenCachedExternalTypeDirect type_bSelectEpistyle =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bSelectEpistyle ) );
        private ArcenCachedExternalTypeDirect type_bLockToggle =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bLockToggle ) );
        private ArcenCachedExternalTypeDirect type_bPriorityToggle =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bPriorityToggle ) );
        private ArcenCachedExternalTypeDirect type_bConversionChoice =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bConversionChoice ) );
        private ArcenCachedExternalTypeDirect type_tEpistyleResourceSummary =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tEpistyleResourceSummary ) );
        private ArcenCachedExternalTypeDirect type_bDepositHere =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bDepositHere ) );

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

            #region Build summary stats
            EpistylesInWindow.Clear();
            nOffense = 0; nUtility = 0; nInfra = 0; nUpgrade = 0; nIdle = 0; nStarved = 0;
            for ( int r = 0; r <= (int)DZResource.End; r++ ) BottleneckCounts[r] = 0;

            List<SafeSquadWrapper> epistylesList = dzInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistylesList.Count; i++ )
            {
                GameEntity_Squad ep = epistylesList[i].GetSquad();
                if ( ep == null ) continue;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) continue;
                EpistylesInWindow.Add( ep );
                if ( epd.NextConversion == null )
                {
                    nIdle++;
                }
                else
                {
                    if ( epd.NextConversion.Upgrade != null ) nUpgrade++;
                    else if ( epd.NextConversion.IsOffensive ) nOffense++;
                    else if ( epd.NextConversion.IsUtility ) nUtility++;
                    else nInfra++;

                    if ( IsEpistyleTrulyStarved( epd ) )
                    {
                        nStarved++;
                        foreach ( KeyValuePair<DZResource, int> kv in epd.NextConversion.Cost )
                        {
                            if ( kv.Value <= 0 ) continue;
                            int have = 0;
                            epd.Inventory.TryGetValue( kv.Key, out have );
                            if ( have < kv.Value )
                                BottleneckCounts[(int)kv.Key]++;
                        }
                    }
                }
            }
            #endregion

            #region Summary row
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
            AddText( Set, type_tSummary, string.Empty, -1, -1, leftBounds, 11f );

            if ( nUpgrade > 0 )
            {
                CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
                AddText( Set, type_tUpgradeHint, string.Empty, -1, -1, leftBounds, 11f );
            }

            if ( nStarved > 0 )
            {
                CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
                AddText( Set, type_tStarvedLine, string.Empty, -1, -1, leftBounds, 11f );
            }
            #endregion

            #region Per-Epistyle rows
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            for ( int i = 0; i < EpistylesInWindow.Count; i++ )
            {
                float rowX = leftBuffer;
                Rect planetBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_PLANET, rowHeight );
                rowX += W_PLANET;
                Rect convBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_CONVERSION, rowHeight );
                rowX += W_CONVERSION;
                Rect lockBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_LOCK, rowHeight );
                rowX += W_LOCK;
                Rect prioBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_PRIORITY, rowHeight );
                runningY += rowHeight + rowBuffer;

                Rect categoryBarBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, W_PLANET, CATEGORY_BAR_HEIGHT );
                Rect tierOneToggleBounds = ArcenRectangle.CreateUnityRect( leftBuffer + W_PLANET, runningY, W_TIER_ONE_TOGGLE, CATEGORY_BAR_HEIGHT );
                runningY += CATEGORY_BAR_HEIGHT + rowBuffer;

                AddText( Set, type_tEpistylePlanet, string.Empty, i, -1, planetBounds, 10f );
                AddButton( Set, type_bSelectEpistyle, string.Empty, i, -1, convBounds, 9f );
                AddButton( Set, type_bLockToggle, string.Empty, i, -1, lockBounds, 10f );
                AddButton( Set, type_bPriorityToggle, string.Empty, i, -1, prioBounds, 10f );
                AddText( Set, type_tEpistyleCategoryBar, string.Empty, i, -1, categoryBarBounds, 8f );
                AddButton( Set, type_bTierOneToggle, string.Empty, i, -1, tierOneToggleBounds, 9f );

                if ( SelectedEpistyleIndex == i )
                {
                    GameEntity_Squad selEp = EpistylesInWindow[i];
                    DarkZenithPerUnitBaseInfo selEpd = selEp.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    ConversionsForSelected.Clear();
                    ConversionsForSelectedBlockReasons.Clear();
                    if ( selEpd != null )
                    {
                        for ( int ci = 0; ci < selEpd.ConversionBag.InternalListSize; ci++ )
                        {
                            DZResourceConversion conv = selEpd.ConversionBag.GetInternalListItemAtIndex( ci );
                            if ( !dzInfo.ShouldShowEpistyleChoice( conv ) ) continue;
                            ConversionsForSelected.Add( conv );
                            ConversionsForSelectedBlockReasons.Add( dzInfo.GetEpistyleChoiceBlockReason( conv ) );
                        }
                    }

                    //Summary text and the deposit button share one row so the expansion stays compact
                    //(the fixed-height panel clips anything past the bottom, which hit the last epistyle).
                    Rect summaryBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, W_FULL - W_DEPOSIT, rowHeight );
                    Rect depositBounds = ArcenRectangle.CreateUnityRect( leftBuffer + ( W_FULL - W_DEPOSIT ), runningY, W_DEPOSIT, rowHeight );
                    runningY += rowHeight + rowBuffer;
                    AddText( Set, type_tEpistyleResourceSummary, string.Empty, i, -1, summaryBounds, 10f );
                    AddButton( Set, type_bDepositHere, string.Empty, i, -1, depositBounds, 10f );

                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    for ( int ci = 0; ci < ConversionsForSelected.Count; ci++ )
                    {
                        Rect choiceBounds;
                        CalculateBoundsSingle( out choiceBounds, ref runningY, W_FULL );
                        AddButton( Set, type_bConversionChoice, string.Empty, i, ci, choiceBounds, 10f );
                    }
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                }
            }
            #endregion

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        #region Inner UI classes

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<b>暗天顶经济管理器</b>", "a1d4ff" );
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

        public class tSummary : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int total = nOffense + nUtility + nInfra + nUpgrade + nIdle;
                Buffer.Add( "柱头：" ).Add( total );
                if ( nOffense > 0 ) Buffer.Add( "  " ).Add( nOffense ).Add( " 攻击", "ffa1a1" );
                if ( nUtility > 0 ) Buffer.Add( "  " ).Add( nUtility ).Add( " 工具", "22a188" );
                if ( nInfra > 0 )   Buffer.Add( "  " ).Add( nInfra ).Add( " 基建", "a1a1ff" );
                if ( nUpgrade > 0 ) Buffer.Add( "  " ).Add( nUpgrade ).Add( " 升级", "a1ffa1" );
                if ( nIdle > 0 )    Buffer.Add( "  " ).Add( nIdle ).Add( " 空闲", "888888" );
            }
        }

        public class tUpgradeHint : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "提示：", "ffffa1" ).Add( "要选择你的升级柱头接下来研究什么，请点击下方，或从舰队侧边栏打开" ).Add( "暗黑深渊科技树", "a1d4ff" ).Add( "来浏览所有升级及其前置条件。", "888888" );
            }
        }

        public class tStarvedLine : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( nStarved == 0 )
                {
                    Buffer.Add( "所有柱头都有资源可以继续", "a1ffa1" );
                    return;
                }
                Buffer.Add( "匮乏：" ).Add( nStarved, "ffaa44" ).Add( "  " );
                bool first = true;
                for ( int r = 1; r < (int)DZResource.End; r++ )
                {
                    if ( BottleneckCounts[r] == 0 ) continue;
                    if ( !first ) Buffer.Add( ", " );
                    DZResource res = (DZResource)r;
                    Buffer.Add( DarkZenithFactionBaseInfoRoot.ResourceFancyName[res], DarkZenithFactionBaseInfoRoot.ResourceColour[res] )
                          .Add( " (" ).Add( BottleneckCounts[r] ).Add( ")" );
                    first = false;
                }
            }
        }

        public class tEpistylePlanet : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );

                bool isStarved = epd != null && IsEpistyleTrulyStarved( epd );
                string col = isStarved ? "ffaa44" : "8ab4cc";
                Buffer.Add( ep.Planet?.Name ?? "???", col );
            }
        }

        public class tEpistyleCategoryBar : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) return;

                //Splashes of color under the planet name showing which conversion categories this
                //Epistyle's bag can draw from, using the same palette as ConversionColour
                const string segment = "▬▬▬▬";
                if ( epd.CanBuildOffensiveUnits )  Buffer.Add( segment, "ffa1a1" );
                if ( epd.CanBuildInfrastructure )  Buffer.Add( segment, "a1a1ff" );
                if ( epd.CanBuildUpgrades )        Buffer.Add( segment, "a1ffa1" );
                if ( epd.CanBuildUtility )         Buffer.Add( segment, "22a188" );
            }
        }

        public class bSelectEpistyle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) { Buffer.Add( "?" ); return; }
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) { Buffer.Add( "?" ); return; }
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) { Buffer.Add( "?" ); return; }

                bool isSelected = SelectedEpistyleIndex == idx;
                bool isStarved  = IsEpistyleTrulyStarved( epd );
                bool isWaiting  = !isStarved && epd.NextConversion != null && !epd.CanWeDoResourceConversion( epd.NextConversion );
                string col = isStarved ? "ffaa44" : ConversionColour( epd.NextConversion );

                if ( epd.NextConversion == null )
                {
                    Buffer.Add( isSelected ? "▶ [空闲]" : "[空闲 — 点击设置]", col );
                }
                else
                {
                    string name = epd.NextConversion.DisplayName ?? epd.NextConversion.InternalName;
                    if ( isSelected ) Buffer.Add( "▶ ", "ffffa1" );
                    Buffer.Add( name, col );

                    if ( isWaiting )
                    {
                        int elapsed = epd.TimeWeLastDidConversion < 0
                            ? World_AIW2.Instance.GameSecond
                            : World_AIW2.Instance.GameSecond - epd.TimeWeLastDidConversion;
                        string wait = FormatWaitTime( elapsed );
                        if ( wait.Length > 0 )
                            Buffer.Add( " (" + wait + ")", "666666" );
                    }
                    else if ( isStarved )
                    {
                        bool firstRes = true;
                        foreach ( KeyValuePair<DZResource, int> kv in epd.NextConversion.Cost )
                        {
                            if ( kv.Value <= 0 ) continue;
                            int have = 0;
                            epd.Inventory.TryGetValue( kv.Key, out have );
                            if ( have >= kv.Value ) continue;
                            Buffer.Add( firstRes ? " [" : ",", "888888" );
                            Buffer.Add( DarkZenithFactionBaseInfoRoot.ResourceFancyName[kv.Key],
                                        DarkZenithFactionBaseInfoRoot.ResourceColour[kv.Key] );
                            firstRes = false;
                        }
                        if ( !firstRes ) Buffer.Add( "]", "888888" );
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                SelectedEpistyleIndex = ( SelectedEpistyleIndex == idx ) ? -1 : idx;
                return MouseHandlingResult.None;
            }
        }

        public class bLockToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null || epd.NextConversion == null ) { Buffer.Add( "锁定", "444444" ); return; }
                Buffer.Add( epd.KeepConversion ? "已锁定" : "锁定", epd.KeepConversion ? "aaaaff" : "777777" );
            }

            public override bool GetShouldBeHidden()
            {
                int idx = this.Element?.CreatedByCodeDirective?.Identifier.CodeDirectiveTag1 ?? -1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return true;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return true;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                return epd == null || epd.NextConversion == null;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return MouseHandlingResult.PlayClickDeniedSound;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null || epd.NextConversion == null ) return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( ep.PrimaryKeyID );
                command.RelatedString = epd.NextConversion.InternalName;
                command.RelatedBools.Add( !epd.KeepConversion );
                command.RelatedBools.Add( epd.HighPriority );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                return MouseHandlingResult.None;
            }
        }

        public class bPriorityToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null || epd.NextConversion == null ) { Buffer.Add( "优先级", "444444" ); return; }
                Buffer.Add( epd.HighPriority ? "▲ 优先" : "普通", epd.HighPriority ? "ffdd44" : "777777" );
            }

            public override bool GetShouldBeHidden()
            {
                int idx = this.Element?.CreatedByCodeDirective?.Identifier.CodeDirectiveTag1 ?? -1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return true;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return true;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                return epd == null || epd.NextConversion == null;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return MouseHandlingResult.PlayClickDeniedSound;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null || epd.NextConversion == null ) return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( ep.PrimaryKeyID );
                command.RelatedString = epd.NextConversion.InternalName;
                command.RelatedBools.Add( epd.KeepConversion );
                command.RelatedBools.Add( !epd.HighPriority );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                return MouseHandlingResult.None;
            }
        }

        public class bTierOneToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) return;
                Buffer.Add( "建造上限：", "888888" );
                switch ( epd.MaxOffenseTier )
                {
                    case 1: Buffer.Add( "仅突击机", "aaffaa" ); break;
                    case 2: Buffer.Add( "最高护卫舰", "aaffaa" ); break;
                    default: Buffer.Add( "任意等级", "777777" ); break;
                }
            }

            public override bool GetShouldBeHidden()
            {
                int idx = this.Element?.CreatedByCodeDirective?.Identifier.CodeDirectiveTag1 ?? -1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return true;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return true;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                return epd == null || !epd.CanBuildOffensiveUnits;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return MouseHandlingResult.PlayClickDeniedSound;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( ep.PrimaryKeyID );
                command.RelatedMagnitude = (epd.MaxOffenseTier + 1) % 3; //cycle: Any (0) -> Strikecraft Only (1) -> Up to Frigates (2) -> Any (0)
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                return MouseHandlingResult.None;
            }
        }

        public class tEpistyleResourceSummary : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( epd == null ) return;

                Buffer.Add( "拥有：", "888888" );
                bool any = false;
                for ( DZResource res = DZResource.None + 1; res < DZResource.End; res++ )
                {
                    int have = 0;
                    epd.Inventory.TryGetValue( res, out have );
                    if ( have <= 0 ) continue;
                    if ( any ) Buffer.Add( ", " );
                    Buffer.Add( have.ToString(), DarkZenithFactionBaseInfoRoot.ResourceColour[res] )
                          .Add( " " + DarkZenithFactionBaseInfoRoot.ResourceFancyName[res] );
                    any = true;
                }
                if ( !any ) Buffer.Add( "无", "666666" );

                if ( epd.NextConversion == null ) return;
                Buffer.Add( "   需求：", "888888" );
                bool anyNeed = false;
                foreach ( KeyValuePair<DZResource, int> kv in epd.NextConversion.Cost )
                {
                    if ( kv.Value <= 0 ) continue;
                    int have = 0;
                    epd.Inventory.TryGetValue( kv.Key, out have );
                    if ( anyNeed ) Buffer.Add( ", " );
                    string col = have >= kv.Value ? DarkZenithFactionBaseInfoRoot.ResourceColour[kv.Key] : "ffaa44";
                    Buffer.Add( have + "/" + kv.Value, col ).Add( " " + DarkZenithFactionBaseInfoRoot.ResourceFancyName[kv.Key] );
                    anyNeed = true;
                }
                if ( !anyNeed ) Buffer.Add( "无需求", "a1ffa1" );
            }

            public override bool GetShouldBeHidden()
            {
                int idx = this.Element?.CreatedByCodeDirective?.Identifier.CodeDirectiveTag1 ?? -1;
                return idx < 0 || idx >= EpistylesInWindow.Count || idx != SelectedEpistyleIndex;
            }
        }

        public class bDepositHere : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return;
                DarkZenithPerUnitBaseInfo epd = ep.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( epd == null ) return;

                int eligible = CountEligibleDepositFlagships( ep, epd );
                if ( eligible <= 0 )
                {
                    Buffer.Add( "⬇ 此处无", "555555" );
                    return;
                }
                Buffer.Add( "⬇ 存入 ×" + eligible, "a1ffa1" );
            }

            public override bool GetShouldBeHidden()
            {
                int idx = this.Element?.CreatedByCodeDirective?.Identifier.CodeDirectiveTag1 ?? -1;
                return idx < 0 || idx >= EpistylesInWindow.Count || idx != SelectedEpistyleIndex;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                GameEntity_Squad ep = EpistylesInWindow[idx];
                if ( ep == null ) return MouseHandlingResult.PlayClickDeniedSound;
                DarkZenithPerUnitBaseInfo epd = ep.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( epd == null ) return MouseHandlingResult.PlayClickDeniedSound;
                if ( CountEligibleDepositFlagships( ep, epd ) <= 0 )
                    return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.DepositDZResources], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( ep.PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                return MouseHandlingResult.None;
            }
        }

        public class bConversionChoice : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int convIdx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2;
                if ( convIdx < 0 || convIdx >= ConversionsForSelected.Count ) { Buffer.Add( "?" ); return; }
                DZResourceConversion conv = ConversionsForSelected[convIdx];
                string blockReason = ConversionsForSelectedBlockReasons[convIdx];
                bool blocked = blockReason != null;
                string col = blocked ? "555555" : ConversionColour( conv );
                Buffer.Add( "  → ", col );
                Buffer.Add( conv.DisplayName ?? conv.InternalName, blocked ? "666666" : "a1ffa1" );
                if ( blocked )
                {
                    Buffer.Add( "  (" + blockReason + ")", "884444" );
                }
                else
                {
                    Buffer.Add( "  成本：", "888888" );
                    bool any = false;
                    foreach ( KeyValuePair<DZResource, int> kv in conv.Cost )
                    {
                        if ( kv.Value <= 0 ) continue;
                        if ( any ) Buffer.Add( ", " );
                        Buffer.Add( kv.Value.ToString(), DarkZenithFactionBaseInfoRoot.ResourceColour[kv.Key] )
                              .Add( " " + DarkZenithFactionBaseInfoRoot.ResourceFancyName[kv.Key] );
                        any = true;
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int epIdx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                int convIdx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2;
                if ( epIdx < 0 || epIdx >= EpistylesInWindow.Count ) return MouseHandlingResult.PlayClickDeniedSound;
                if ( convIdx < 0 || convIdx >= ConversionsForSelected.Count ) return MouseHandlingResult.PlayClickDeniedSound;

                if ( ConversionsForSelectedBlockReasons[convIdx] != null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                GameEntity_Squad ep = EpistylesInWindow[epIdx];
                if ( ep == null ) return MouseHandlingResult.PlayClickDeniedSound;
                DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( epd == null ) return MouseHandlingResult.PlayClickDeniedSound;

                DZResourceConversion conv = ConversionsForSelected[convIdx];
                bool keepConversion = input.RightButtonClicked;
                bool highPriority = epd.HighPriority;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( ep.PrimaryKeyID );
                command.RelatedString = conv.InternalName;
                command.RelatedBools.Add( keepConversion );
                command.RelatedBools.Add( highPriority );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                SelectedEpistyleIndex = -1;
                return MouseHandlingResult.None;
            }
        }

        #endregion
    }
}
