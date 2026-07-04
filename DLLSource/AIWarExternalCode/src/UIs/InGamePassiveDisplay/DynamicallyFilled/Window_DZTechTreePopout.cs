using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_DZTechTreePopout : Window_DynamicallyFilledAbstractBase
    {
        public static Window_DZTechTreePopout Instance;
        private bool _open;

        private static readonly List<DZUpgrade> VariantUpgrades =
            List<DZUpgrade>.Create_WillNeverBeGCed( 24, "Window_DZTechTreePopout-VariantUpgrades" );
        private static readonly List<DZUpgrade> ProgressionUpgrades =
            List<DZUpgrade>.Create_WillNeverBeGCed( 16, "Window_DZTechTreePopout-ProgressionUpgrades" );

        //Ship Tier and Mark Level upgrades gate each other in lockstep; this is the order they actually become available
        private static readonly int[] ProgressionOrder = { 19, 20, 100, 101, 21, 102, 103, 22, 104, 105 };

        //VariantUpgrades is sorted by UpgradeIndex, which (30-34, 40-44, 50-54, 60-64) is naturally
        //a 4-row x 5-column grid: row = ship tier, column = variant color
        private static readonly string[] VariantTierRowLabels = { "突击机", "守护者", "凶兆", "外星" };
        private const int VARIANT_COLUMNS = 5;
        private const int VARIANT_ROWS = 4;

        //Whether the player currently has (or is building) a Terminus producing each resource;
        //used to flag variant unlocks that are technically available but practically stuck at 0 income
        private static readonly bool[] ResourceHasTerminus = new bool[(int)DZResource.End + 1];

        //Flat list of requirement lines for locked progression upgrades, built fresh each populate;
        //rows reference into this by flat index via CodeDirectiveTag1
        private static readonly List<string> ProgressionRequirementText =
            List<string>.Create_WillNeverBeGCed( 40, "Window_DZTechTreePopout-ProgressionRequirementText" );
        private static readonly List<bool> ProgressionRequirementMet =
            List<bool>.Create_WillNeverBeGCed( 40, "Window_DZTechTreePopout-ProgressionRequirementMet" );

        public Window_DZTechTreePopout()
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

        #region Upgrade helper logic
        public enum UpgradeStatus
        {
            Completed,
            InProgress,
            Available,
            Locked
        }

        private static string GetUpgradeDescription( DZUpgrade upgrade )
        {
            if ( upgrade.UnlockShipTier )
                return "解锁舰船等级 " + upgrade.RelatedInteger1;
            if ( upgrade.UnlockShipVariant )
                return "解锁 " + DarkZenithFactionBaseInfoRoot.GetShipVariantTypeFromResource( upgrade.RelatedResource ) + " 变体（等级 " + upgrade.RelatedInteger1 + "）";
            if ( upgrade.UnlockMarkLevel )
                return "解锁标记等级 " + upgrade.RelatedInteger1;
            return upgrade.GetDisplayName();
        }

        public static UpgradeStatus GetUpgradeStatus( DarkZenithFactionBaseInfoRoot dzBaseInfo, DZUpgrade upgrade )
        {
            if ( dzBaseInfo.HasUpgradeBeenDone( upgrade ) )
                return UpgradeStatus.Completed;
            if ( dzBaseInfo.IsAnotherEpistyleUpgradingForMe( upgrade ) )
                return UpgradeStatus.InProgress;
            if ( dzBaseInfo.IsUpgradeAllowed( upgrade, dzBaseInfo.GetVariantUpgrades() ) )
                return UpgradeStatus.Available;
            return UpgradeStatus.Locked;
        }

        //Appends one line per requirement of a locked upgrade, in priority order, with whether each is already met
        private static void AppendRequirementLines( DarkZenithFactionBaseInfoRoot dzBaseInfo, DZUpgrade upgrade, List<string> lines, List<bool> met )
        {
            if ( upgrade.PrereqUpgrade1 > 0 )
            {
                DZUpgrade prereq = DarkZenithUpgradeTable.Instance.GetRowById( upgrade.PrereqUpgrade1 );
                bool done = dzBaseInfo.HasUpgradeBeenDone( prereq );
                lines.Add( done ? GetUpgradeDescription( prereq ) : "需要 " + GetUpgradeDescription( prereq ) );
                met.Add( done );
            }
            if ( upgrade.PrereqUpgrade2 > 0 )
            {
                DZUpgrade prereq = DarkZenithUpgradeTable.Instance.GetRowById( upgrade.PrereqUpgrade2 );
                bool done = dzBaseInfo.HasUpgradeBeenDone( prereq );
                lines.Add( done ? GetUpgradeDescription( prereq ) : "需要 " + GetUpgradeDescription( prereq ) );
                met.Add( done );
            }
            if ( upgrade.RequiredVariantUpgrades > 0 )
            {
                int numVariants = dzBaseInfo.GetVariantUpgrades();
                bool done = numVariants >= upgrade.RequiredVariantUpgrades;
                lines.Add( done
                    ? upgrade.RequiredVariantUpgrades + " 个变体解锁"
                    : "需要 " + upgrade.RequiredVariantUpgrades + " 个变体解锁（已有 " + numVariants + " 个）" );
                met.Add( done );
            }
            //Intensity doesn't gate the player (see IsUpgradeAllowed), so it isn't shown as a requirement here
            if ( !dzBaseInfo.IsPlayer && upgrade.RequiredIntensity > 0 )
            {
                bool done = dzBaseInfo.Intensity >= upgrade.RequiredIntensity;
                lines.Add( done
                    ? "强度 " + upgrade.RequiredIntensity
                    : "需要强度 " + upgrade.RequiredIntensity + "（当前 " + dzBaseInfo.Intensity + "）" );
                met.Add( done );
            }
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

        private const float W_FULL = 420f;
        private const float W_GRID_LABEL = 70f;
        private const float W_GRID_CELL = 70f;

        private ArcenCachedExternalTypeDirect type_tVariantsHeader =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tVariantsHeader ) );
        private ArcenCachedExternalTypeDirect type_tVariantColumnHeader =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tVariantColumnHeader ) );
        private ArcenCachedExternalTypeDirect type_tVariantRowLabel =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tVariantRowLabel ) );
        private ArcenCachedExternalTypeDirect type_tVariantCell =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tVariantCell ) );
        private ArcenCachedExternalTypeDirect type_tVariantSummary =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tVariantSummary ) );
        private ArcenCachedExternalTypeDirect type_tProgressionHeader =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tProgressionHeader ) );
        private ArcenCachedExternalTypeDirect type_tProgressionRow =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tProgressionRow ) );
        private ArcenCachedExternalTypeDirect type_tProgressionRequirement =
            ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tProgressionRequirement ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;

            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null ) { this.Close(); return; }

            DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            if ( dzBaseInfo == null ) { this.Close(); return; }

            float runningY = topBuffer;
            Rect leftBounds;

            #region Build the lists of upgrades to show
            VariantUpgrades.Clear();
            ProgressionUpgrades.Clear();

            DarkZenithUpgradeTable upgradeTable = DarkZenithUpgradeTable.Instance;
            for ( int i = 0; i < upgradeTable.Rows.Count; i++ )
            {
                DZUpgrade upgrade = upgradeTable.Rows[i];
                if ( upgrade.UpgradeIndex <= 0 || upgrade.UnusualUpgrade )
                    continue;
                if ( upgrade.UnlockShipVariant )
                    VariantUpgrades.Add( upgrade );
                else if ( upgrade.UnlockShipTier || upgrade.UnlockMarkLevel )
                    ProgressionUpgrades.Add( upgrade );
            }
            VariantUpgrades.Sort( static ( a, b ) => a.UpgradeIndex.CompareTo( b.UpgradeIndex ) );
            ProgressionUpgrades.Sort( static ( a, b ) =>
                Array.IndexOf( ProgressionOrder, (int)a.UpgradeIndex ).CompareTo( Array.IndexOf( ProgressionOrder, (int)b.UpgradeIndex ) ) );
            #endregion

            #region Determine which resources currently have (or are getting) a Terminus
            Array.Clear( ResourceHasTerminus, 0, ResourceHasTerminus.Length );
            List<SafeSquadWrapper> terminiiForCheck = dzBaseInfo.Terminii.GetDisplayList();
            for ( int i = 0; i < terminiiForCheck.Count; i++ )
            {
                GameEntity_Squad t = terminiiForCheck[i].GetSquad();
                DarkZenithPerUnitBaseInfo tData = t?.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( tData == null ) continue;
                ResourceHasTerminus[(int)tData.Resource] = true;
            }
            List<SafeSquadWrapper> warpingInForCheck = dzBaseInfo.WarpingInTerminii.GetDisplayList();
            for ( int i = 0; i < warpingInForCheck.Count; i++ )
            {
                GameEntity_Squad w = warpingInForCheck[i].GetSquad();
                DarkZenithPerUnitBaseInfo wData = w?.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( wData == null ) continue;
                ResourceHasTerminus[(int)wData.Resource] = true;
            }
            #endregion

            #region Ship Variants grid
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
            AddText( Set, type_tVariantsHeader, string.Empty, -1, -1, leftBounds, 11f );

            if ( VariantUpgrades.Count == VARIANT_COLUMNS * VARIANT_ROWS )
            {
                //Column header row (one column per variant color)
                float rowX = leftBuffer + W_GRID_LABEL;
                for ( int c = 0; c < VARIANT_COLUMNS; c++ )
                {
                    Rect colBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_GRID_CELL, rowHeight );
                    rowX += W_GRID_CELL;
                    AddText( Set, type_tVariantColumnHeader, string.Empty, c, -1, colBounds, 9f );
                }
                runningY += rowHeight + rowBuffer;

                //One row per ship tier
                for ( int r = 0; r < VARIANT_ROWS; r++ )
                {
                    rowX = leftBuffer;
                    Rect labelBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_GRID_LABEL, rowHeight );
                    rowX += W_GRID_LABEL;
                    AddText( Set, type_tVariantRowLabel, string.Empty, r, -1, labelBounds, 9f );

                    for ( int c = 0; c < VARIANT_COLUMNS; c++ )
                    {
                        Rect cellBounds = ArcenRectangle.CreateUnityRect( rowX, runningY, W_GRID_CELL, rowHeight );
                        rowX += W_GRID_CELL;
                        AddText( Set, type_tVariantCell, string.Empty, r * VARIANT_COLUMNS + c, -1, cellBounds, 9f );
                    }
                    runningY += rowHeight + rowBuffer;
                }

                CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
                AddText( Set, type_tVariantSummary, string.Empty, -1, -1, leftBounds, 10f );
            }
            #endregion

            #region Ship Tier / Mark Level progression
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
            AddText( Set, type_tProgressionHeader, string.Empty, -1, -1, leftBounds, 11f );

            ProgressionRequirementText.Clear();
            ProgressionRequirementMet.Clear();
            for ( int i = 0; i < ProgressionUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = ProgressionUpgrades[i];

                CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
                AddText( Set, type_tProgressionRow, string.Empty, i, -1, leftBounds, 10f );

                if ( GetUpgradeStatus( dzBaseInfo, upgrade ) == UpgradeStatus.Locked )
                {
                    int startIdx = ProgressionRequirementText.Count;
                    AppendRequirementLines( dzBaseInfo, upgrade, ProgressionRequirementText, ProgressionRequirementMet );
                    for ( int r = startIdx; r < ProgressionRequirementText.Count; r++ )
                    {
                        CalculateBoundsSingle( out leftBounds, ref runningY, W_FULL );
                        AddText( Set, type_tProgressionRequirement, string.Empty, r, -1, leftBounds, 9f );
                    }
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
                Buffer.Add( "<b>暗黑深渊科技树</b>", "a1d4ff" );
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

        public class tVariantsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<b>舰船变体</b>", "ffd27f" );
            }
        }

        public class tVariantColumnHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= VariantUpgrades.Count ) return;
                DZUpgrade upgrade = VariantUpgrades[idx]; //row 0 of the grid; its RelatedResource defines this column
                string col = DarkZenithFactionBaseInfoRoot.ResourceColour != null ? DarkZenithFactionBaseInfoRoot.ResourceColour[upgrade.RelatedResource] : "cddcdc";
                Buffer.Add( DarkZenithFactionBaseInfoRoot.GetShipVariantTypeFromResource( upgrade.RelatedResource ), col );
            }
        }

        public class tVariantRowLabel : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= VariantTierRowLabels.Length ) return;
                Buffer.Add( VariantTierRowLabels[idx], "8ab4cc" );
            }
        }

        public class tVariantCell : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= VariantUpgrades.Count ) return;
                DZUpgrade upgrade = VariantUpgrades[idx];

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
                if ( dzBaseInfo == null ) return;

                UpgradeStatus status = GetUpgradeStatus( dzBaseInfo, upgrade );
                if ( status == UpgradeStatus.Available && !ResourceHasTerminus[(int)upgrade.RelatedResource] )
                {
                    Buffer.Add( "无 " + DarkZenithFactionBaseInfoRoot.ResourceFancyName[upgrade.RelatedResource], "ff9955" );
                    return;
                }

                switch ( status )
                {
                    case UpgradeStatus.Completed:
                        Buffer.Add( "已完成", "66cc66" );
                        break;
                    case UpgradeStatus.InProgress:
                        Buffer.Add( "进行中", "ffd27f" );
                        break;
                    case UpgradeStatus.Available:
                        Buffer.Add( "可用", "ffffa1" );
                        break;
                    case UpgradeStatus.Locked:
                        Buffer.Add( "已锁定", "666666" );
                        break;
                }
            }
        }

        public class tVariantSummary : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
                if ( dzBaseInfo == null ) return;

                Buffer.Add( "变体解锁已完成：" ).Add( dzBaseInfo.GetVariantUpgrades().ToString(), "a1ffa1" ).Add( " / " + VariantUpgrades.Count, "888888" );

                for ( int i = 0; i < VariantUpgrades.Count; i++ )
                {
                    DZUpgrade upgrade = VariantUpgrades[i];
                    if ( GetUpgradeStatus( dzBaseInfo, upgrade ) == UpgradeStatus.Available && !ResourceHasTerminus[(int)upgrade.RelatedResource] )
                    {
                        Buffer.Add( "   '无<资源>'表示该资源可以研究，但你还没有生产该资源的终点站", "888888" );
                        break;
                    }
                }
            }
        }

        public class tProgressionHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<b>舰船等级 / 标记等级进度</b>", "ffd27f" );
            }
        }

        public class tProgressionRow : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= ProgressionUpgrades.Count ) return;
                DZUpgrade upgrade = ProgressionUpgrades[idx];

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                DarkZenithFactionBaseInfoRoot dzBaseInfo = localFaction?.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
                if ( dzBaseInfo == null ) return;

                UpgradeStatus status = GetUpgradeStatus( dzBaseInfo, upgrade );
                string nameColor = upgrade.UnlockShipTier ? "ffcc88" : "88ccff";
                Buffer.Add( GetUpgradeDescription( upgrade ), nameColor );

                switch ( status )
                {
                    case UpgradeStatus.Completed:
                        Buffer.Add( "  [已完成]", "66cc66" );
                        break;
                    case UpgradeStatus.InProgress:
                        Buffer.Add( "  [进行中]", "ffd27f" );
                        break;
                    case UpgradeStatus.Available:
                        Buffer.Add( "  [可用]", "ffffa1" );
                        break;
                    case UpgradeStatus.Locked:
                        Buffer.Add( "  [已锁定]", "666666" );
                        break;
                }
            }
        }

        public class tProgressionRequirement : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int idx = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( idx < 0 || idx >= ProgressionRequirementText.Count ) return;

                bool met = ProgressionRequirementMet[idx];
                Buffer.Add( "        " + ( met ? "✓  " : "✗  " ) + ProgressionRequirementText[idx], met ? "66cc66" : "ff9955" );
            }
        }

        #endregion
    }
}
