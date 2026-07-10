using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;
using TMPro;
using Sys=System.Collections.Generic;
using System.Linq;

namespace Arcen.AIW2.External
{
    #region SortBy Value Calculators
    public static class GameEntityTypeDataExtensions
    {
        private static string[] Rows = new string[]
        {
            "AddedToFleet",
            "PlayerLogisticalCommand",
            "AddedToCommandStation",
            "AddedToFleet_MinorFaction",
            "OtherHackables"
        };

        public static int LineCap( this GameEntityTypeData data, GameEntityTypeData parent=null, int mark=-1, Log log=null )
        {
            log?.Msg("{0}.LineCap(parent={1}) called.", data.InternalName, parent.OrNull());
            
            int baseCap = 0;

            for (int i = 0; i < Rows.Length; i++)
            {
                var row = FleetDesignTemplateTable.Instance.GetRowByName(Rows[i]);

                FleetItem line;
                if (row.TryGetType( data, out line))
                {
                    log?.Msg("Found FleetItem with cap '{0}' in '{1}'.", line.Cap, row.InternalName);
                    
                    baseCap = line.Cap;
                    break;
                }
            }

            if (baseCap < 1)
            {
                foreach (var row in data.FleetDesignTemplatesIAmPartOf)
                {
                    log?.Msg("Checking in '{0}'.", row.InternalName);
                    
                    FleetItem line;
                    if (row.TryGetType( data, out line))
                    {
                        log?.Msg("Found FleetItem with cap '{0}' in '{1}'.", line.Cap, row.InternalName);
                        
                        baseCap = line.Cap;
                        break;
                    }
                }
            }
            
            if (baseCap < 1)
            {
                if (data.BaseShipLineForBuildPoints_TypeData != null &&
                    data.BaseShipLineForBuildPoints_TypeData != data)
                {
                    return data.BaseShipLineForBuildPoints_TypeData.LineCap();
                }
            }
            
            if (baseCap < 1)
            {
                log?.Msg("Found no FleetItem for cap.");
                
                return 0;
            }

            var cap = data.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap(
                        data, baseCap, baseCap, (byte)(mark!=-1 ? mark : data.EncyclopediaOnly_MarkLevel.Display) );

            return cap;
        }

        public static int LineMetal( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            var num = stats.MetalCost;
            
            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int LineEnergy( this GameEntityTypeData data )
        { 
            var num = data.EnergyUsage;
            
            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int LineShields( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            var num = stats.BaseShieldPoints;

            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int LineHull( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            var num = stats.BaseHullPoints;

            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int LineHealth( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            var num = stats.BaseHullPoints + stats.BaseShieldPoints;

            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int HealthValue( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            FInt num = (stats.BaseHullPoints + stats.BaseShieldPoints).ToFInt();

            if (stats.MetalCost == 0)
                return 0;

            num /= stats.MetalCost;

            return num.ToInt();
        }

        public static int LineDps(this GameEntityTypeData data )
        { 
            FInt num = FInt.Zero;
            foreach (var sys in data.SystemTypes)
            {
                var stats = sys.MarkStatsFor(data.EncyclopediaOnly_MarkLevel.Display);
                num += stats.CalculateShipOrFleetMemDamagePerSecond(null, null);
            }

            var cap = data.LineCap();
            if (cap > 0)
                num *= cap;

            return num.ToInt();
        }

        public static int DpsValue(this GameEntityTypeData data )
        { 
            FInt num = FInt.Zero;
            foreach (var sys in data.SystemTypes)
            {
                var stats = sys.MarkStatsFor(data.EncyclopediaOnly_MarkLevel.Display);
                num += stats.CalculateShipOrFleetMemDamagePerSecond(null, null);
            }

            var mstats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            if (mstats.MetalCost == 0)
                return 0;

            num *= 1000;
            num /= mstats.MetalCost;

            return num.ToInt();
        }

        public static int LineStrength( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            var cap = data.LineCap();

            var num = stats.StrengthPerSquad_CalculatedWithNullFleetMembership;
            if (cap > 0)
                num *= cap;

            return num;
        }

        public static int StrengthValue( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            FInt num = stats.StrengthPerSquad_CalculatedWithNullFleetMembership.ToFInt();

            if (stats.MetalCost == 0)
                return 0;

            num *= 1000;
            num /= stats.MetalCost;

            return num.ToInt();
        }

        public static int AICost( this GameEntityTypeData data )
        { 
            return data.CostForAIToPurchase;
        }

        public static int AIValue( this GameEntityTypeData data )
        { 
            var stats = data.MarkStatsFor( data.EncyclopediaOnly_MarkLevel.Display );
            FInt num = stats.StrengthPerSquad_CalculatedWithNullFleetMembership.ToFInt();

            if (data.CostForAIToPurchase == 0)
                return 0;

            num = num * 1000 / data.CostForAIToPurchase;

            return num.ToInt();
        }

        public static FInt AIValue( this GameEntityTypeData data, int markLevel )
        { 
            var stats = data.MarkStatsFor( (byte)markLevel );
            FInt num = stats.StrengthPerSquad_CalculatedWithNullFleetMembership.ToFInt();

            if (data.CostForAIToPurchase == 0)
                return (FInt)0;

            num = num / data.CostForAIToPurchase;

            return num;
        }
    }
    #endregion

    // jcf: idea: move the fields in gameentitytypedata for our benefit, out of there
    #region EntityTypeInfo
    
    #if false
    
    public class Encyclopedia_Type_Data : TimeBasedPoolable<Encyclopedia_Type_Data>
    {
        #region Pooling Logic
        private static readonly ReferenceTracker FleetRefTracker = new ReferenceTracker( "Fleets" );

        private Encyclopedia_Type_Data()
        {
            FleetRefTracker.IncrementObjectCount();
        }

        private static TimeBasedPool<Encyclopedia_Type_Data> Pool = TimeBasedPool<Encyclopedia_Type_Data>.Create_WillNeverBeGCed( "Encyclopedia_Type_Data", 2, 10, 25000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Encyclopedia_Type_Data(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        
        public static Encyclopedia_Type_Data GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }
        
        #endregion
        
        public GameEntityTypeData Type;
        
        public readonly ArcenDoubleCharacterBuffer EncyclopediaOnly_Tooltip = new ArcenDoubleCharacterBuffer("EntityTypeInfo.EncyclopediaOnly_Tooltip");
        public readonly DoubleBufferedValue<int> EncyclopediaOnly_CountByFactionFilter = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<Faction> EncyclopediaOnly_LastFactionForColor = new DoubleBufferedValue<Faction>( null );
        public readonly DoubleBufferedValue<bool> EncyclopediaOnly_CanHackToAcquire = new DoubleBufferedValue<bool>( false );
        public readonly DoubleBufferedValue<bool> EncyclopediaOnly_CanBeBuiltByHumansNow = new DoubleBufferedValue<bool>( false );
        
        public bool EncyclopediaOnly_DoesMatch = true;
        public byte EncyclopediaOnly_MarkLevel = 0;

        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOugoingReferences()
        {
        }

        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleToDo()
        {
            EncyclopediaOnly_Tooltip.Clear();
            EncyclopediaOnly_CountByFactionFilter.Clear();
            EncyclopediaOnly_LastFactionForColor.Clear();
            EncyclopediaOnly_CanHackToAcquire.Clear();
            EncyclopediaOnly_CanBeBuiltByHumansNow.Clear();
            EncyclopediaOnly_DoesMatch = true;
            EncyclopediaOnly_MarkLevel = 0;
        }
    }
    
    #endif 
    
    #endregion
    
    
    public class Window_UnitEncyclopedia : StackMenuWindowController, IInputActionHandler
    {
        protected float topBuffer = 3;
        protected float leftBuffer = 2;
        protected float rowHeight = 24;
        protected float rowBuffer = 1.5f;
        
        public static ThematicGroupType CurrentlyViewedThematicGroup;
        public static UnitEncyclopediaTextboxFunction CurrentTextboxFunction;
        public static UnitEncyclopediaListFilterStyle CurrentListFilterStyle;
        public static UnitEncyclopediaSortStyle CurrentSortStyle;
        public static string CurrentSearchText = string.Empty;
        public static int FactionIndex = -1; //-1 means "all", or everyone in the galaxy
        private static bool _ShowOnlyExistingInGame = true;
        
        public static bool InGame
        {
            get
            {
                if ( World_AIW2.Instance == null || 
                     World_AIW2.Instance.Factions.Count == 0 || 
                     World_AIW2.Instance.InSetupPhase )
                {
                    return false;
                }
                
                return true;
            }
        }
        
        public static bool ShowingOnlyInGame
        {
            get
            {
                if (!InGame)
                    return false;
                
                return _ShowOnlyExistingInGame;
            }
        }

        #region CalculateBoundsSingle
        protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsDual
        protected void CalculateBoundsDual( out Rect leftBounds, out Rect rightBounds, ref float runningY, float NameWidth, float ValueWidth )
        {
            leftBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            rightBounds = ArcenRectangle.CreateUnityRect( leftBounds.xMax, runningY, ValueWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsTriple
        protected void CalculateBoundsTriple( out Rect leftBounds, out Rect centerBounds, out Rect rightBounds, ref float runningY, float IconWidth, float NameWidth, float ValueWidth )
        {
            leftBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, IconWidth, rowHeight );

            centerBounds = ArcenRectangle.CreateUnityRect( leftBounds.xMax, runningY, NameWidth, rowHeight );

            rightBounds = ArcenRectangle.CreateUnityRect( centerBounds.xMax, runningY, ValueWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        public ThematicGroup CurrentCategory;

        public static Window_UnitEncyclopedia Instance;
        public Window_UnitEncyclopedia()
        {
            Instance = this;
            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.CurrentCategory = null;
        }
        public override string GetBriefName() { return "单位百科"; }

        public override void OnOpen()
        {
            if ( World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleOpeningAMenuThatMightPause();
            
            var e = Window_UnitEncyclopedia.txtTextFilter.Instance.Element as ArcenUI_Input;
            e.ReferenceInputField.onFocusSelectAll = true;
            e.Focus();
        }

        public override void OnClose()
        {
            if ( World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleClosingAMenuThatMightPause();
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "单位百科" );
                if ( World.Instance.IsLoaded )
                {
                    if ( World.Instance.IsPaused )
                        Buffer.Add( "   <size=90%><color=#ffb21c>游戏已暂停</color></size>" );
                    else
                        Buffer.Add( "   <size=90%><color=#5de2ff>游戏运行中</color></size>" );
                }
            }
        }

        #region dThematicCategory
        public class dThematicCategory : DropdownAbstractBase
        {
            public static dThematicCategory Instance;
            public dThematicCategory()
            {
                Instance = this;
            }

            public void Reset()
            {
                CurrentlyViewedThematicGroup = null;
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                elementAsType.ClearItems();
                OnUpdate();
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                ThematicGroupType ItemAsType = (ThematicGroupType)Item.GetItem();
                if ( ItemAsType == null )
                    return;

                CurrentlyViewedThematicGroup = ItemAsType;
            }

            public override void OnUpdate()
            {
                if ( ThematicGroupTypeTable.Instance.Rows.Count <= 0 )
                    return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                if ( CurrentlyViewedThematicGroup == null )
                    CurrentlyViewedThematicGroup = ThematicGroupTypeTable.Instance.GetRowByName( "All" );
                ThematicGroupType typeDataToSelect = CurrentlyViewedThematicGroup;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (ThematicGroupType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < ThematicGroupTypeTable.Instance.Rows.Count; i++ )
                    {
                        ThematicGroupType row = ThematicGroupTypeTable.Instance.Rows[i];
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        ThematicGroupType optionItemAsType = (ThematicGroupType)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < ThematicGroupTypeTable.Instance.Rows.Count; i++ )
                    {
                        ThematicGroupType row = ThematicGroupTypeTable.Instance.Rows[i];
                        DropdownOptionThematicGroupType option = new DropdownOptionThematicGroupType( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "分组使查找类别更容易。";
                if ( CurrentlyViewedThematicGroup == null )
                    CurrentlyViewedThematicGroup = ThematicGroupTypeTable.Instance.GetRowByName( "All" );
                ThematicGroupType typeDataToSelect = CurrentlyViewedThematicGroup;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Description;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                ThematicGroupType ItemAsType = (ThematicGroupType)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "分组使查找类别更容易。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>：\n" + ItemAsType.Description );
            }
        }
        #endregion

        private ArcenCachedExternalTypeDirect type_bIconDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bIconDisplay ) );
        private ArcenCachedExternalTypeDirect type_bTypeDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTypeDisplay ) );
        private ArcenCachedExternalTypeDirect type_bLoreDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bLoreDisplay ) );

        private const float MAX_VIEWPORT_SIZE = 480; //it's actualyl 420, but let's have some extra room
        private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( CurrentlyViewedThematicGroup == null )
                return;

            if ( this.CurrentCategory == null )
            {
                for ( int i = 0; i < ThematicGroupTable.Instance.Rows.Count; i++ )
                {
                    ThematicGroup group = ThematicGroupTable.Instance.Rows[i];
                    if ( group.GroupType != CurrentlyViewedThematicGroup || group.IsHidden || group.SortedUnitTypes.Count <= 0 )
                        continue;
                    this.CurrentCategory = group;
                    break;
                }
                if ( this.CurrentCategory == null )
                    return; //found nothing!
            }

            float runningY = topBuffer;
            Rect iconBounds;
            Rect leftBounds;
            Rect rightBounds;

            if ( CurrentSortStyle == null )
            {
                CurrentSortStyle = UnitEncyclopediaSortStyleTable.Instance.DefaultRow;
                if ( CurrentSortStyle == null )
                    CurrentSortStyle = UnitEncyclopediaSortStyleTable.Instance.Rows[0];
            }

            //start other thread - this can be heavy, so let's calculate it on the background
            ArcenThreading.RunTaskOnBackgroundThread( "_UI.EncyclopediaCalculations", false, false, () =>
            {
                CalculateMatchesOnBackgroundThread();

            } ); //end other thread

            // Here we check for which Category is active,
            // then display the slider if there are "enough" tips to require a slider to display
            //lock (this.CurrentCategory.ResortedUnitTypesForEncyclopedia)
            {
                var listToShow = this.CurrentCategory.ResortedUnitTypesForEncyclopedia.GetDisplayList();

                float minYToShow = bMainContentParent.ParentRT.anchoredPosition.y;
                float maxYToShow = minYToShow + MAX_VIEWPORT_SIZE + EXTRA_BUFFER;
                minYToShow -= EXTRA_BUFFER;

                for ( int i = 0; i < listToShow.Count; i++ )
                {
                    var typeData = listToShow[i];
                    
                    // doesn't match our search or filters, so skip
                    if ( !typeData.EncyclopediaOnly_DoesMatch.Display )
                        continue; 

                    this.CalculateBoundsTriple( out iconBounds, out leftBounds, out rightBounds, ref runningY, 33, 667, 100 );
                    
                    // it's scrolled up far enough we can skip it, yay!
                    if ( leftBounds.yMax < minYToShow )
                        continue; 
                    
                    // this is below where we are scrolled,
                    // so let's skip this, too!
                    // We won't break out, because we need runningY to be fully calculated
                    if ( leftBounds.yMax > maxYToShow )
                        continue; 

                    AddButton( Set, type_bIconDisplay, typeData.InternalName, typeData.RowIndexNonSim, -1, iconBounds, -1f ); //okay to use here, as it will be consistent per run
                    AddButton( Set, type_bTypeDisplay, typeData.InternalName, typeData.RowIndexNonSim, -1, leftBounds, -1f ); //okay to use here, as it will be consistent per run
                    AddButton( Set, type_bLoreDisplay, typeData.InternalName, typeData.RowIndexNonSim, -1, rightBounds, -1f ); //okay to use here, as it will be consistent per run
                }

                bMainContentParent.ParentRT.UI_SetHeight( runningY );
            }
        }

        #region CalculateMatchesOnBackgroundThread
        public void CalculateMatchesOnBackgroundThread()
        {
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                bool doSearch = CurrentTextboxFunction != null;

                FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( FactionIndex );

                debugStage = 1100;

                // clear counts
                {
                    debugStage = 1200;
                    foreach ( GameEntityTypeData typeData in GameEntityTypeDataTable.Instance.Rows )
                    {
                        debugStage = 1400;
                        if ( typeData.Category != GameEntityCategory.Ship )
                            continue;

                        debugStage = 1500;
                        typeData.EncyclopediaOnly_CountByFactionFilter.ClearConstructionValueForStartingConstruction();
                        typeData.EncyclopediaOnly_LastFactionForColor.ClearConstructionValueForStartingConstruction();
                        typeData.EncyclopediaOnly_CanHackToAcquire.ClearConstructionValueForStartingConstruction();
                        typeData.EncyclopediaOnly_CanBeBuiltByHumansNow.ClearConstructionValueForStartingConstruction();
                        typeData.EncyclopediaOnly_DoesMatch.ClearConstructionValueForStartingConstruction();
                        typeData.EncyclopediaOnly_MarkLevel.ClearConstructionValueForStartingConstruction();
                    }
                }

                debugStage = 2100;
                
                bool countunclaimed = false;
                        
                void ShouldCountUnowned()
                {
                    debugStage = 9860;
                    if ( !ShowingOnlyInGame )
                        return;
                    
                    if ( currentFilter.GetDoesFactionMatch(World_AIW2.Instance.GetNeutralFaction()) == false )
                        return;
                    
                    countunclaimed = true;
                }
                ShouldCountUnowned();

                //
                if (!InGame)
                {
                    // no counting needed
                }
                else
                {
                    foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads() )
                    {
                            debugStage = 2200;
                            if ( squad == null || !squad.GetShouldBeVisibleBasedOnPlanetIntel() )
                                continue; //don't break intel on this

                            var goesto = squad.TypeData.EncyclopediaCountingGoesTo;
                            if (goesto == null)
                                continue;

                            if (countunclaimed)
                            {
                                if ( squad.ShipGrantsList.Count > 0 )
                                {
                                    try
                                    {
                                        debugStage = 4400;
                                        foreach ( ShipLineEntry entry in squad.ShipGrantsList )
                                        {
                                            if ( entry != null && entry.TypeData != null && entry.TypeData.EncyclopediaCountingGoesTo != null)
                                            {
                                                var temp = entry.TypeData.EncyclopediaCountingGoesTo;
                                                if (temp != null)
                                                {
                                                    temp.EncyclopediaOnly_CanHackToAcquire.Construction = true;
                                                    temp.EncyclopediaOnly_CountByFactionFilter.Construction += entry.BaseNumShips;
                                                    temp.EncyclopediaOnly_LastFactionForColor.Construction = World_AIW2.Instance.GetNeutralFaction();
                                                }
                                            }
                                        }
                                    }
                                    catch { } //threading error based on the foreach, it's okay.  I didn't actually run into that, but it's only a matter of time.
                                }
                                
                                if (squad.TypeData.GetHasTag("HackAsIfCapturable"))
                                {
                                    foreach (var grant in squad.EnumerateGrantedShips() )
                                    {
                                        var temp = grant.TypeData?.EncyclopediaCountingGoesTo;
                                        if (temp != null)
                                        {
                                            temp.EncyclopediaOnly_CanHackToAcquire.Construction = true;
                                            temp.EncyclopediaOnly_CountByFactionFilter.Construction += grant.Count;
                                            temp.EncyclopediaOnly_LastFactionForColor.Construction = World_AIW2.Instance.GetNeutralFaction();
                                        }
                                    }
                                }
                            }

                            debugStage = 2300;
                            if ( !squad.GetMatchesFactionFilterSafe( currentFilter ) )
                                continue;
                            
                            goesto.EncyclopediaOnly_CountByFactionFilter.Construction += squad.ShipCount;

                            debugStage = 2500;
                            
                            //whoever is encountered first sets the color
                            // todo: improve this somehow, like show duplicates per faction owning
                            if ( goesto.EncyclopediaOnly_LastFactionForColor.Construction == null )
                            {
                                debugStage = 2600;
                                Faction fac = squad.GetFactionOrNull_Safe();
                                if ( fac != null )
                                    goesto.EncyclopediaOnly_LastFactionForColor.Construction = fac;
                            }

                            debugStage = 2700;

                            //count in AI reinforcement points, too
                            if ( squad.AIReinforcementPointContents != null )
                            {
                                debugStage = 2800;
                                try
                                {
                                    foreach ( RefPair<GameEntityTypeData, int> kv in squad.AIReinforcementPointContents )
                                    {
                                        if ( kv.RightItem > 0 )
                                        {
                                            var temp = kv.LeftItem.EncyclopediaCountingGoesTo;
                                            if (temp != null)
                                            {
                                                temp.EncyclopediaOnly_CountByFactionFilter.Construction += kv.RightItem;
                                                temp.EncyclopediaOnly_LastFactionForColor.Construction = squad.GetFactionOrNull_Safe();
                                            }
                                        }
                                    }
                                }
                                catch { } //threading error based on the foreach, it's okay.  I didn't actually run into that, but it's only a matter of time.
                            }

                            // count unbuilt members with a cap
                            debugStage = 3200;
                            if ( squad.TypeData.IsFleetLeader )
                            {
                                debugStage = 3400;
                                var fleet = squad.FleetMembership.Fleet;

                                debugStage = 3500;
                                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                {
                                        // the centerpiece has already been counted
                                        if (mem == squad.FleetMembership)
                                            continue;
                                        
                                        debugStage = 3600;
                                        int count = mem.GetMaxTotalCount_ForUIOnly(squad.GetIsPlayerUnit());
                                        if (count > 0)
                                        {
                                            debugStage = 3600;
                                            var temp = mem.TypeData.EncyclopediaCountingGoesTo;
                                            if (temp != null)
                                            {
                                                debugStage = 3700;
                                                temp.EncyclopediaOnly_CountByFactionFilter.Construction += count;
                                                debugStage = 3900;
                                                temp.EncyclopediaOnly_LastFactionForColor.Construction = squad.GetFactionOrNull_Safe();
                                            }
                                        }
                                        
                                        debugStage = 4100;
                                    }

                                debugStage = 4200;
                                continue;
                            }
                    }
                }

                debugStage = 9100;

                // find all the matches
                foreach ( var typeData in GameEntityTypeDataTable.Instance.Rows )
                {
                    debugStage = 9200;
                    if ( typeData.Category != GameEntityCategory.Ship )
                        continue;
                    
                    debugStage = 9300;

                    debugStage = 9400;
                    
                    bool matches = true;

                    debugStage = 9600;
                    if ( matches )
                    {
                        debugStage = 9700;
                        if ( ShowingOnlyInGame &&
                             typeData.EncyclopediaOnly_CountByFactionFilter.Construction == 0 )
                        {
                            matches = false;
                        }
                        // if this type is marked as hidden, then it is 
                        // only shown if we are in-game, only showing types
                        // that are in-game, and this hidden type exists in-game
                        else if ( typeData.IsHidden )
                            matches = false;
                    }

                    if ( matches )
                    {
                        if ( doSearch )
                        {
                            debugStage = 9800;
                            if ( !CurrentTextboxFunction.Implementation.CalculateDoesUnitMatchFilter( typeData ) )
                                matches = false;
                        }
                    }
                    
                    if ( matches ) 
                    {
                        debugStage = 9900;
                        if ( CurrentListFilterStyle != null && 
                             !CurrentListFilterStyle.Implementation.CalculateDoesUnitMatchFilter( typeData, CurrentListFilterStyle ) )
                        {
                            matches = false; 
                        }
                    }
                    
                    debugStage = 10000;
                    typeData.EncyclopediaOnly_DoesMatch.Construction = matches;

                    debugStage = 10500;
                    Faction facForMarkLevel = typeData.EncyclopediaOnly_LastFactionForColor.Display;
                    Planet currentPlanetForMarkLevel = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

                    debugStage = 10600;
                    byte markLevel = 0;
                    if (FactionIndex > 0 || 
                        (facForMarkLevel != null && 
                        facForMarkLevel.Type != FactionType.AI && 
                        currentPlanetForMarkLevel != null))
                    {
                        debugStage = 10700;
                        markLevel = typeData.MarkFor( currentPlanetForMarkLevel.GetPlanetFactionForFaction( facForMarkLevel ) );
                    }
                    else
                    {
                        debugStage = 10800;
                        markLevel = typeData.MaxMarkLevel;
                    }
                    
                    debugStage = 10900;
                    typeData.EncyclopediaOnly_MarkLevel.Construction = markLevel;
                }
               
                debugStage = 11000;
                
                // swap construction to display
                foreach ( var typeData in GameEntityTypeDataTable.Instance.Rows )
                {
                    debugStage = 11100;
                    if ( typeData.Category != GameEntityCategory.Ship )
                        continue;
                    
                    typeData.EncyclopediaOnly_CountByFactionFilter.SwitchConstructionToDisplay();
                    typeData.EncyclopediaOnly_LastFactionForColor.SwitchConstructionToDisplay();
                    typeData.EncyclopediaOnly_CanHackToAcquire.SwitchConstructionToDisplay();
                    typeData.EncyclopediaOnly_CanBeBuiltByHumansNow.SwitchConstructionToDisplay();
                    typeData.EncyclopediaOnly_DoesMatch.SwitchConstructionToDisplay();
                    typeData.EncyclopediaOnly_MarkLevel.SwitchConstructionToDisplay();
                }
                
                debugStage = 13100;
                
                // then update counts on any groups
                for ( int i = 0; i < ThematicGroupTable.Instance.Rows.Count; i++ )
                {
                    debugStage = 13200;
                    var group = ThematicGroupTable.Instance.Rows[i];
                    
                    debugStage = 13300;
                    if ( group.IsHidden || 
                         group.SortedUnitTypes.Count <= 0 )
                    {
                        continue;
                    }

                    debugStage = 13400;
                    
                    int countOfMatchesInGroup = 0;
                    foreach ( var typeData in group.SortedUnitTypes )
                    {
                        debugStage = 13500;
                        if ( typeData.EncyclopediaOnly_DoesMatch.Display )
                            countOfMatchesInGroup++;
                    }
                    
                    debugStage = 13600;
                    group.CurrentMatchesInGroup = countOfMatchesInGroup;
                }

                debugStage = 15100;
                
                // lastly do the custom sort if there is one
                if ( CurrentSortStyle != null )
                {
                    debugStage = 15200;
                    CurrentSortStyle.Implementation.SortAllCategories( CurrentSortStyle );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CalculateMatchesOnBackgroundThread exception at debugStage " + debugStage + "\nError: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public static GameEntityTypeData GetTypeDataForController( ElementAbstractBase controller )
        {
            string internalName = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
            return GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( internalName );
        }

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

        private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_UnitEncyclopedia.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10 );
                        }
                    }
                    #endregion

                    this.Element.Window.MaxDeltaTimeBeforeUpdates = 0; //more load, but keeps the scrolling feeling nice
                }

                this.OnUpdateCategories();
            }

            public void OnUpdateCategories()
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnCategoryPool.Clear( 10 );

                for ( int i = 0; i < ThematicGroupTable.Instance.Rows.Count; i++ )
                {
                    ThematicGroup group = ThematicGroupTable.Instance.Rows[i];
                    if ( group.GroupType != CurrentlyViewedThematicGroup || group.IsHidden || group.SortedUnitTypes.Count <= 0 )
                        continue;
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( group );
                }

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 10, ref currentY, 25, 218, 23);
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private ThematicGroup thematicGroup = null;

            public void Assign( ThematicGroup ThematicGroup )
            {
                this.thematicGroup = ThematicGroup;
            }

            public override bool GetShouldBeHidden()
            {
                return this.thematicGroup == null;
            }

            public override void Clear()
            {
                this.thematicGroup = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.thematicGroup == null )
                    return;

                if ( Instance.CurrentCategory == this.thematicGroup )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.thematicGroup.DisplayName ?? "?" );

                if ( Instance.CurrentCategory == this.thematicGroup )
                    buffer.Add( "</color>" );

                if ( this.thematicGroup.CurrentMatchesInGroup > 0 &&
                    this.thematicGroup.CurrentMatchesInGroup < this.thematicGroup.SortedUnitTypes.Count )
                    buffer.Add( "<color=#ffdd68>" );
                else
                    buffer.Add( "<color=#827e80>" );

                if ( this.thematicGroup.CurrentMatchesInGroup >= 0 )
                    buffer.Add( " (" ).Add( this.thematicGroup.CurrentMatchesInGroup ).Add( ")" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.thematicGroup == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.thematicGroup;
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                if ( this.thematicGroup == null )
                    return;
                string tooltipText = thematicGroup.DisplayName ?? "?" + "\n" + thematicGroup.Description +
                    "\n\n组内单位总数：" + thematicGroup.SortedUnitTypes.Count +
                    "\n组内匹配筛选的单位：" + thematicGroup.CurrentMatchesInGroup;

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipText );
            }
        }
        #endregion

        #region bIconDisplay
        
        public class bIconDisplay : ButtonAbstractBase
        {
            private bool _init = false;
            
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) 
                    return;

                if ( !_init )
                {
                    _init = true;
                    
                    var ctr = ((ArcenUI_Button)this.Element).ReferenceText;
                    ctr.alignment = TMPro.TextAlignmentOptions.Left;
                    ctr.overflowMode = TextOverflowModes.Overflow;
                    ctr.verticalAlignment = VerticalAlignmentOptions.Top;
                }
                
                if ( typeData.TexEmbedSprite_Icon != null )
                {
                    Faction faction = null;
                    if (ShowingOnlyInGame)
                        faction = typeData.EncyclopediaOnly_LastFactionForColor.Display;
                    
                    var e = EntityText.GetFakeEntity(typeData, faction:faction);
                    buffer.AddShipIconInline(e, TextStyle.Ship_Sprite_Ency);
                    EntityText.ReleaseFakeEntity(e);
                }
            }
            
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) return MouseHandlingResult.None;

                float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                
                EntityText.ShowDetails(
                    0.25f, 2f, 
                    typeData.DisplayName + " 的详细信息", 
                    "关闭",
                    (b)=>Window_UnitEncyclopedia.Instance.WriteDetailsOfAllShipMarks( b, typeData, centerPopupScale ));

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                bTypeDisplay.ShipMouseover( typeData, this.Element );
            }
        }

        #endregion

        #region bTypeDisplay
        
        public class bTypeDisplay : ButtonAbstractBase
        {
            private bool hasDoneAlignment = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) 
                    return;

                if ( !hasDoneAlignment )
                {
                    hasDoneAlignment = true;
                    ( (ArcenUI_Button)this.Element).ReferenceText.alignment = TMPro.TextAlignmentOptions.Left;
                }

                Faction facForMarkLevel = typeData.EncyclopediaOnly_LastFactionForColor.Display;
                Planet currentPlanetForMarkLevel = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

                buffer.Add( " " );
                
                string name = typeData.DisplayName;
                if ( CurrentSortStyle != null )
                {
                    CurrentSortStyle.Implementation.AddTextIfNeeded( CurrentSortStyle, typeData, buffer );

                    if ( CurrentSortStyle.Implementation.ShouldShowShortNameInsteadOfFullDisplayName( CurrentSortStyle ) )
                        name = typeData.DisplayNameForSidebar;
                }
                
                buffer.Add( name );
                
                // dlc/mod
                /*
                if (typeData.IsExternal)
                {
                    buffer.AddDlcMod(typeData, TextStyle.DlcMod_Abbrev_Small);
                }
                */
                
                // AddedToFleet line count if it exists
                {
                    var cap = typeData.LineCap();
                    if (cap > 0)
                    {
                        var col = QuickColors.HeaderDull;
                        buffer.StartColor( col ).Add("<sub>").Add(Text.Multiply).Add(cap).Add("</sub>").EndColor();
                    }
                }

                buffer.Pos( "360" );
                
                if ( ShowingOnlyInGame &&
                     FactionIndex > 0 || 
                     facForMarkLevel != null && 
                     facForMarkLevel.Type != FactionType.AI  && 
                     currentPlanetForMarkLevel != null )
                {
                    // only use a real mark level if we have a specific faction selected
                    // ... and don't do it for AIs, who use wildly different mark levels
                    
                    if ( typeData.EncyclopediaOnly_MarkLevel.Display > 0 )
                    {
                        Balance_MarkLevel soloMark = Balance_MarkLevelTable.Instance.RowsByOrdinal[typeData.EncyclopediaOnly_MarkLevel.Display];
                        buffer.StartColor( soloMark.ColorHex ).Add( soloMark.Abbreviation ).EndColor();
                    }
                    else
                    {
                        buffer.StartColor( "413a3d" ).Add( "无等级" ).EndColor();
                    }
                }
                else
                if ( typeData.StartingMarkLevel.Ordinal > 0 )
                {
                    if ( typeData.StartingMarkLevel.Ordinal == 1 && typeData.MaxMarkLevel >= 7 )
                    {
                        buffer.StartColor( "696466" ).Add( "全部标记" ).EndColor();
                    }
                    else 
                    if ( typeData.StartingMarkLevel.Ordinal == typeData.MaxMarkLevel )
                    {
                        buffer.StartColor( typeData.StartingMarkLevel.ColorHex ).Add( typeData.StartingMarkLevel.Abbreviation ).EndColor();
                    }
                    else
                    {
                        Balance_MarkLevel endMark = Balance_MarkLevelTable.Instance.RowsByOrdinal[typeData.MaxMarkLevel];
                        buffer.StartColor( typeData.StartingMarkLevel.ColorHex ).Add( typeData.StartingMarkLevel.Abbreviation ).EndColor()
                            .Add( " - " ).StartColor( endMark.ColorHex ).Add( endMark.Abbreviation ).EndColor();
                    }
                }
                else
                {
                    buffer.StartColor( "413a3d" ).Add( "无标记" ).EndColor();
                }

                int countCurrentlyHere = typeData.EncyclopediaOnly_CountByFactionFilter.Display;
                bool canHack = typeData.EncyclopediaOnly_CanHackToAcquire.Display;
                bool canBuild = typeData.EncyclopediaOnly_CanBeBuiltByHumansNow.Display;
                bool canClaim = !canHack && typeData.EncyclopediaOnly_LastFactionForColor.Display?.Type == FactionType.NaturalObject;
                
                if ( countCurrentlyHere > 0 || 
                     canHack || 
                     canBuild ||
                     canClaim )
                {
                    buffer.Pos( "460" ).Add("(").Add( countCurrentlyHere ).Add(")");

                    if ( canHack )
                        buffer.StartSize("60%").Add( " (可黑客入侵)" ).EndSize();
                    if ( canBuild )
                        buffer.StartSize("60%").Add( " (可建造)" ).EndSize();
                    if ( canClaim )
                        buffer.StartSize("60%").Add( " (可占领)" ).EndSize();
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) return MouseHandlingResult.None;

                float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                
                EntityText.ShowDetails(
                    0.25f, 2f, 
                    typeData.DisplayName + " 的详细信息", 
                    "关闭",
                    (b)=>Window_UnitEncyclopedia.Instance.WriteDetailsOfAllShipMarks( b, typeData, centerPopupScale ));
                
                return MouseHandlingResult.None;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer("Window_UnitEncyclopedia-bTypeDisplay-tooltipBuffer");//ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_UnitEncyclopedia-bTypeDisplay-tooltipBuffer" );
            
            public override void HandleMouseover()
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                ShipMouseover( typeData, this.Element );
            }

            public static void ShipMouseover( GameEntityTypeData typeData, IArcenUIElementForSizing MustBeAboveOrBelow )
            { 
                if ( typeData == null ) 
                    return;

                byte markToShow = typeData.StartingMarkLevel.Ordinal;
                if ( markToShow != 0)
                    markToShow = typeData.MaxMarkLevel;
                
                Faction factionToShow = null;
                if (ShowingOnlyInGame)
                    factionToShow = typeData.EncyclopediaOnly_LastFactionForColor.Display;

                /*
                int countCurrentlyHere = typeData.EncyclopediaOnly_CountByFactionFilter.Display;
                bool canHack = typeData.EncyclopediaOnly_CanHackToAcquire.Display;
                bool canBuild = typeData.EncyclopediaOnly_CanBeBuiltByHumansNow.Display;
                
                if ( countCurrentlyHere > 0 || canHack || canBuild )
                {
                    tooltipBuffer.Add( "数量：" ).Add( countCurrentlyHere );
                    if ( canHack )
                        tooltipBuffer.Add( "  （可入侵获取）" );
                    if ( canBuild )
                        tooltipBuffer.Add( "  （可立即建造）" );

                    tooltipBuffer.Add( "\n" );
                }
                */

                int countToShow = 1;
                var cap = typeData.LineCap();
                if (cap > 0)
                    countToShow = cap;

                FromSidebarType from = FromSidebarType.NonSidebar_SingleUnit;
                if (countToShow > 0)
                    from = FromSidebarType.NonSidebar_MultipleUnits;
                
                //LOG.Msg("ShipMouseover; factionToShow={0}", factionToShow.OrNull());
                
                EntityText.GetTooltip( tooltipBuffer, null, null,
                    typeData, countToShow, factionToShow, markToShow, from, ShipExtraDetailFlags.Encyclopedia, 0, false );
                
                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( MustBeAboveOrBelow, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        #endregion

        #region bLoreDisplay
        
        public class bLoreDisplay : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) return;

                if ( typeData.FullLore != null && typeData.FullLore.Length > 0 )
                    buffer.Add( "阅读传说" );
                else
                    buffer.StartColor( "696466" ).Add( "无传说" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) return MouseHandlingResult.None;

                if ( typeData.FullLore != null && typeData.FullLore.Length > 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, typeData.DisplayName, typeData.FullLore + "\n\n\n\n\n", "OK" );
                    
                    return MouseHandlingResult.None;
                }
                
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            public override void HandleMouseover()
            {
                GameEntityTypeData typeData = GetTypeDataForController( this );
                if ( typeData == null ) return;

                string loreTextToShow = typeData.FullLore;
                if ( loreTextToShow == null || loreTextToShow.Length <= 0 )
                    return;

                if ( loreTextToShow.Length > 4000 )
                    loreTextToShow = loreTextToShow.Substring( 4000 ) + "\n...";

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, loreTextToShow );
            }
        }
        
        #endregion

        #region dTextFilterType
        public class dTextFilterType : DropdownAbstractBase
        {
            public static dTextFilterType Instance;
            public dTextFilterType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                UnitEncyclopediaTextboxFunction ItemAsType = (UnitEncyclopediaTextboxFunction)Item.GetItem();
                CurrentTextboxFunction = ItemAsType;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //WorldSetup setupToViewOnly = World_AIW2.Instance.SetupWorkingForLobbyOnly;
                UnitEncyclopediaTextboxFunction typeDataToSelect = CurrentTextboxFunction;
                if ( typeDataToSelect == null )
                    typeDataToSelect = UnitEncyclopediaTextboxFunctionTable.Instance.DefaultRow;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (UnitEncyclopediaTextboxFunction)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < UnitEncyclopediaTextboxFunctionTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaTextboxFunction row = UnitEncyclopediaTextboxFunctionTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        UnitEncyclopediaTextboxFunction optionItemAsType = (UnitEncyclopediaTextboxFunction)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < UnitEncyclopediaTextboxFunctionTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaTextboxFunction row = UnitEncyclopediaTextboxFunctionTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        DropdownOptionUnitEncyclopediaTextboxFunction option = new DropdownOptionUnitEncyclopediaTextboxFunction( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择文本框如何对上方单位列表进行搜索。";
                UnitEncyclopediaTextboxFunction typeDataToSelect = CurrentTextboxFunction;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Tooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                UnitEncyclopediaTextboxFunction ItemAsType = (UnitEncyclopediaTextboxFunction)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择文本框如何对上方单位列表进行搜索。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>:\n" + ItemAsType.Tooltip );
            }
        }

        public class DropdownOptionUnitEncyclopediaTextboxFunction : RowBasedDropdownOption<UnitEncyclopediaTextboxFunction>
        {
            public DropdownOptionUnitEncyclopediaTextboxFunction( UnitEncyclopediaTextboxFunction Row ) : base( Row )
            {
            }
        }
        #endregion

        #region txtTextFilter
        public class txtTextFilter : InputAbstractBase
        {
            public static txtTextFilter Instance;
            public txtTextFilter() { Instance = this; }
            public override void HandleChangeInValue( string NewValue )
            {
                CurrentSearchText = NewValue ?? string.Empty;
            }

            private string lastPlaceholderText = string.Empty;

            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;

                //set the placeholder text based on what is set to the left
                UnitEncyclopediaTextboxFunction function = CurrentTextboxFunction;
                if ( function != null &&
                    this.lastPlaceholderText != function.PlaceholderText )
                {
                    this.lastPlaceholderText = function.PlaceholderText;
                    TextMeshProUGUI placeHolderText = elementAsType.ReferenceInputField.placeholder as TextMeshProUGUI;
                    if ( placeHolderText )
                    {
                        placeHolderText.text = function.PlaceholderText;
                    }
                }
                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    elementAsType.SetText( CurrentSearchText );
                }
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        CurrentSearchText = string.Empty; //wipe out this field, since we hit escape
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
        #endregion

        #region dFilterByList
        public class dFilterByList : DropdownAbstractBase
        {
            public static dFilterByList Instance;
            public dFilterByList()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                UnitEncyclopediaListFilterStyle ItemAsType = (UnitEncyclopediaListFilterStyle)Item.GetItem();
                CurrentListFilterStyle = ItemAsType;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //WorldSetup setupToViewOnly = World_AIW2.Instance.SetupWorkingForLobbyOnly;
                UnitEncyclopediaListFilterStyle typeDataToSelect = CurrentListFilterStyle;
                if ( typeDataToSelect == null )
                    typeDataToSelect = UnitEncyclopediaListFilterStyleTable.Instance.DefaultRow;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (UnitEncyclopediaListFilterStyle)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < UnitEncyclopediaListFilterStyleTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaListFilterStyle row = UnitEncyclopediaListFilterStyleTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        UnitEncyclopediaListFilterStyle optionItemAsType = (UnitEncyclopediaListFilterStyle)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < UnitEncyclopediaListFilterStyleTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaListFilterStyle row = UnitEncyclopediaListFilterStyleTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        DropdownOptionUnitEncyclopediaListFilterStyle option = new DropdownOptionUnitEncyclopediaListFilterStyle( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择一种分类筛选方式来过滤上方的列表，通过单位的某些特性来缩小查找范围。";
                UnitEncyclopediaListFilterStyle typeDataToSelect = CurrentListFilterStyle;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Tooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                UnitEncyclopediaListFilterStyle ItemAsType = (UnitEncyclopediaListFilterStyle)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择一种分类筛选方式来过滤上方的列表，通过单位的某些特性来缩小查找范围。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>:\n" + ItemAsType.Tooltip );
            }
        }

        public class DropdownOptionUnitEncyclopediaListFilterStyle : RowBasedDropdownOption<UnitEncyclopediaListFilterStyle>
        {
            public DropdownOptionUnitEncyclopediaListFilterStyle( UnitEncyclopediaListFilterStyle Row ) : base( Row )
            {
            }
        }
        #endregion

        #region dSort
        public class dSort : DropdownAbstractBase
        {
            public static dSort Instance;
            public dSort()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                UnitEncyclopediaSortStyle ItemAsType = (UnitEncyclopediaSortStyle)Item.GetItem();

                if ( ItemAsType != CurrentSortStyle )
                {
                    CurrentSortStyle = ItemAsType;
                    CurrentSortStyle.Implementation.SortAllCategories( CurrentSortStyle );
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //WorldSetup setupToViewOnly = World_AIW2.Instance.SetupWorkingForLobbyOnly;
                UnitEncyclopediaSortStyle typeDataToSelect = CurrentSortStyle;
                if ( typeDataToSelect == null )
                    typeDataToSelect = UnitEncyclopediaSortStyleTable.Instance.DefaultRow;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (UnitEncyclopediaSortStyle)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < UnitEncyclopediaSortStyleTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaSortStyle row = UnitEncyclopediaSortStyleTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        UnitEncyclopediaSortStyle optionItemAsType = (UnitEncyclopediaSortStyle)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < UnitEncyclopediaSortStyleTable.Instance.Rows.Count; i++ )
                    {
                        UnitEncyclopediaSortStyle row = UnitEncyclopediaSortStyleTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        DropdownOptionUnitEncyclopediaSortStyle option = new DropdownOptionUnitEncyclopediaSortStyle( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择单位的排序方式。";
                UnitEncyclopediaSortStyle typeDataToSelect = CurrentSortStyle;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Tooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                UnitEncyclopediaSortStyle ItemAsType = (UnitEncyclopediaSortStyle)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择单位的排序方式。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>:\n" + ItemAsType.Tooltip );
            }
        }

        public class DropdownOptionUnitEncyclopediaSortStyle : RowBasedDropdownOption<UnitEncyclopediaSortStyle>
        {
            public DropdownOptionUnitEncyclopediaSortStyle( UnitEncyclopediaSortStyle Row ) : base( Row )
            {
            }
        }
        #endregion

        #region dFactionDropdown
        public class dFactionDropdown : DropdownAbstractBase
        {
            public static dFactionDropdown Instance;
            public dFactionDropdown()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                FactionFilterDropdownOption ItemAsType = (FactionFilterDropdownOption)Item;
                FactionIndex = ItemAsType.Filter.GetFactionIndex();
            }

            public override bool GetShouldBeHidden()
            {
                return World_AIW2.Instance == null || World_AIW2.Instance.Factions.Count == 0;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                int factionIndexToSelect = FactionIndex;
                List<FactionFilter> factionFilters = FactionFilter.GetLatestSortedFiltersAll();

                bool foundMismatch = false;
                if ( elementAsType.CurrentlySelectedOption == null || ((FactionFilter)elementAsType.CurrentlySelectedOption.GetItem()).GetFactionIndex() != factionIndexToSelect )
                {
                    foundMismatch = true;
                }
                else
                {
                    for ( int i = 0; i < factionFilters.Count; i++ )
                    {
                        FactionFilter row = factionFilters[i];
                        //if ( row.IsHidden )
                        //    continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        FactionFilter optionItemAsType = (FactionFilter)option.GetItem();
                        if ( row.GetFactionIndex() == optionItemAsType.GetFactionIndex() )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < factionFilters.Count; i++ )
                    {
                        FactionFilter row = factionFilters[i];
                        //if ( row.IsHidden )
                        //    continue;
                        FactionFilterDropdownOption option = new FactionFilterDropdownOption( row );
                        elementAsType.AddItem( option, row.GetFactionIndex() == factionIndexToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择要用于右侧星系图显示模式中作为筛选条件的阵营或阵营类型。";
                FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( FactionIndex );
                if ( currentFilter.GetIsValid() )
                {
                    mouseoverText += "\n\n当前：<color=#" + currentFilter.GetTextColor() + ">" + currentFilter.GetDisplayName() + "</color>\n" + currentFilter.GetTooltip();
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                FactionFilter ItemAsType = (FactionFilter)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择要用于右侧星系图显示模式中作为筛选条件的阵营或阵营类型。\n\n<color=#" +
                    ItemAsType.GetTextColor() + ">" +
                    ItemAsType.GetDisplayName() + "</color>:\n" + ItemAsType.GetTooltip() );
            }
        }

        public class FactionFilterDropdownOption : IArcenUI_Dropdown_Option
        {
            public FactionFilter Filter;

            public FactionFilterDropdownOption( FactionFilter Filter )
            {
                this.Filter = Filter;
            }

            public object GetItem()
            {
                return this.Filter;
            }

            public string GetOptionNameFromVolatile()
            {
                return "<color=#" + this.Filter.GetTextColor() + ">" + this.Filter.GetDisplayName();
            }

            public Sprite GetOptionSprite()
            {
                return null;
            }
        }
        #endregion

        #region bFilterByInGameStatus
        
        public class bFilterByInGameStatus : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( FactionIndex != -1 )
                    return MouseHandlingResult.PlayClickDeniedSound;

                _ShowOnlyExistingInGame = !_ShowOnlyExistingInGame;

                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeGrayedOut()
            {
                return FactionIndex != -1;
            }

            public override bool GetShouldBeHidden()
            {
                return World_AIW2.Instance == null || World_AIW2.Instance.Factions.Count == 0;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( FactionIndex != -1  )
                    Buffer.StartColor( "9f987f" ).Add( "仅显示游戏中的单位类型" );
                else if ( FactionIndex != -1 || _ShowOnlyExistingInGame )
                    Buffer.Add( "仅显示游戏中的单位类型" );
                else
                    Buffer.Add( "显示所有单位类型" );
            }
        }

        #endregion

        #region bCancel

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        #endregion

        #region bSave

        public class bSave : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }

        #endregion

        #region bSetDefaults
        
        public class bSetDefaults : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }

        #endregion

        #region Window_UnitEncyclopedia Methods
        
        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("UnitEncyclopedia.Handle called {0}", InputActionType.InternalName), Verbosity.DoNotShow);
            var textBox = txtTextFilter.Instance.Element as ArcenUI_Input;
            
            switch ( InputActionType.InternalName )
            {
                case "FocusSearchBox":
                    textBox.ReferenceInputField.onFocusSelectAll = true;
                    textBox.Focus();
                    break;
                case "OpenSystemMenu":
                    if (Window_ErrorReportMenu.Instance.GetShouldDrawThisFrame_Subclass())
                        break;
                    if (Window_ModalSelfUpdatingTextWindow.GetCurrentInstance().GetIsOpen())
                        break;
                    if (Engine_Universal.CurrentPopups.Count > 0)
                    {
                        Engine_Universal.CurrentPopups[0].OnNoOrClose?.Invoke();
                        Engine_Universal.CurrentPopups.RemoveAt(0);
                        break;
                    }
                    
                    this.Close();
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
        
        #region WriteDetailsOfAllShipMarks
        private bool WriteDetailsOfAllShipMarks( ArcenDoubleCharacterBuffer buffer, GameEntityTypeData TypeData, float PositionScaleMultiplier )
        {
            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
            buffer.Add( "\n\n" );

            var flags = ShipExtraDetailFlags.Encyclopedia;
            
            byte min_mark = TypeData.StartingMarkLevel.Ordinal;
            byte max_mark = TypeData.MaxMarkLevel;

            var faction = TypeData.EncyclopediaOnly_LastFactionForColor.Display;

            // Experimental: Showing each mark level compared to the previous (but it makes everything a mess)
            #region unused
            #if false
            var temp_buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("Window_UnitEncyclopedia.WriteDetailsOfAllShipMarks.temp_buffer");
            Sys.List<string> prev_tokens = null;
            
            for ( var mk = min_mark; mk <= max_mark; mk++ )
            {
                var count = TypeData.LineCap(mark:mk);

                temp_buffer.Clear();
                
                EntityText.GetTooltip( 
                    temp_buffer, null, null,
                    TypeData, count, faction, mk, 
                    FromSidebarType.NonSidebar_MultipleUnits, flags, 
                    PositionScaleMultiplier, true );
                
                var txt = temp_buffer.ToString();
                var tokens = EntityText.DiffSplitRegex.Split( txt ).ToList();
                
                if (prev_tokens != null)
                    EntityText.AppendDiff(buffer, prev_tokens, tokens);
                else
                    buffer.Add(temp_buffer);
                
                buffer.Add( "\n\n" );
                
                prev_tokens = tokens;
            }
            #endif
            #endregion
            for ( var mk = min_mark; mk <= max_mark; mk++ )
            {
                var count = TypeData.LineCap(mark:mk);

                EntityText.GetTooltip( 
                    buffer, null, null,
                    TypeData, count, faction, mk, 
                    FromSidebarType.NonSidebar_MultipleUnits, flags, 
                    PositionScaleMultiplier, true );
                
                buffer.Add( "\n\n" );
            }

            #region Drone Additions
            if ( TypeData.FleetDesignTemplateIUseForDrones != null &&
                (TypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag.GetHasItems() ||
                TypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag.GetHasItems()) )
            {
                DrawBag<FleetItem> drones = TypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag;
                if ( drones != null )
                {
                    for ( int j = 0; j < drones.InternalListSize; j++ )
                    {
                        FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                        if ( droneItem == null )
                            continue;

                        int cap = droneItem.Cap;
                        if ( TypeData.MultipliedNonFrigateShipCapForDrones > FInt.Zero )
                            cap = (cap * TypeData.MultipliedNonFrigateShipCapForDrones).GetNearestIntPreferringHigher();

                        EntityText.GetTooltip( buffer, null, null,
                            droneItem.TypeData, cap, faction, min_mark, 
                            FromSidebarType.NonSidebar_SingleUnit, flags, 
                            PositionScaleMultiplier, true );

                        buffer.Add( "\n\n" );
                    }
                }
                
                drones = TypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag;
                if ( drones != null )
                {
                    for ( int j = 0; j < drones.InternalListSize; j++ )
                    {
                        FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                        if ( droneItem == null )
                            continue;

                        int cap = droneItem.Cap;
                        if ( TypeData.MultipliedFrigateShipCapForDrones > FInt.Zero )
                            cap = (cap * TypeData.MultipliedFrigateShipCapForDrones).GetNearestIntPreferringHigher();


                        EntityText.GetTooltip( buffer, null, null,
                            droneItem.TypeData, cap, faction, min_mark, 
                            FromSidebarType.NonSidebar_SingleUnit, flags,
                            PositionScaleMultiplier, true );

                        buffer.Add( "\n\n" );
                    }
                }
            }
            #endregion

            #region Spawn On Death Additions
            if ( !EntityTypeDrawingBag.IsNullOrInvalid( TypeData.SpawnOnDeath_EntityTypeDrawingBag.Value ) )
            {
                foreach ( GameEntityTypeData OnDeathSpawnType in TypeData.SpawnOnDeath_EntityTypeDrawingBag.Value.AllRelatedGameEntityTypes )
                {
                    if ( OnDeathSpawnType == null )
                        continue;

                    EntityText.GetTooltip(
                        buffer, null, null,
                        OnDeathSpawnType, -1, faction, min_mark,
                        FromSidebarType.NonSidebar_SingleUnit, flags,
                        PositionScaleMultiplier, true );

                    buffer.Add( "\n\n" );
                }
            }
            #endregion

            return true;
        }
        #endregion
        
        #endregion

        public void OnWorldClear()
        {
            //LOG.Msg("Window_UnitEncyclopedia.OnWorldClear() called.");
            
            var rows = GameEntityTypeDataTable.Instance.Rows;
            foreach (var r in rows)
            {
                //r.EncyclopediaOnly_Tooltip?.Clear();
                r.EncyclopediaOnly_CountByFactionFilter.Clear();
                r.EncyclopediaOnly_LastFactionForColor.Clear();
                r.EncyclopediaOnly_CanHackToAcquire.Clear();
                r.EncyclopediaOnly_CanBeBuiltByHumansNow.Clear();
                r.EncyclopediaOnly_DoesMatch.Clear();
                r.EncyclopediaOnly_MarkLevel.Clear();
            }
        }
    }

    #region Codehook Handler
    
    public class Encyclopedia_Codehook_Handler : IArcenExternalCodeHookHandler
    {
        void IArcenExternalCodeHookHandler.HandleExternalHook( object MainObject,
            object SecondaryObject,
            object[] AdditionalObjects,
            ArcenExternalCodeHook Hook,
            ArcenSimContextBase Context )
        {
            if (Hook.InternalName == "OnWorldClear")
            {
                //var time = UnityEngine.Time.time;
                //LOG.Msg("_UI.Encyclopedia_Codehook_Handler.OnWorldClear, called at {0}\n~~~~~~~~~~~~~~~", time);
                        
                Window_UnitEncyclopedia.Instance.OnWorldClear();
                return;
            }
            
            if (Hook.InternalName == "PostAllTableInitialize")
            {
                ArcenThreading.RunTaskOnBackgroundThread( "_UI.Encyclopedia_PreProcessing", false, false,
                    ()=>
                    {
                        // Time.time is main-thread-only; use Stopwatch for elapsed timing from a worker thread.
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        LOG.Msg("_UI.Encyclopedia_PreProcessing, begin\n~~~~~~~~~~~~~~~");
                        try
                        {
                            foreach (var type in GameEntityTypeDataTable.Instance.Rows)
                            {
                                if (type.Category != GameEntityCategory.Ship)
                                    continue;
                                
                                if (ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy())
                                {
                                    return;
                                }
                                
                                lock (type.EncyclopediaOnly_Tooltip)
                                {
                                    var buffer = type.EncyclopediaOnly_Tooltip;

                                    buffer.Clear();
                                
                                    var flags = 
                                        ShipExtraDetailFlags.Encyclopedia | 
                                        ShipExtraDetailFlags.HighestDetail |
                                        ShipExtraDetailFlags.PlainText;
                                    
                                    EntityText.GetTooltip(
                                        buffer, null, null, 
                                        type, 1, null, 0, 
                                        FromSidebarType.NonSidebar_MultipleUnits, 
                                        flags, 1.0f, false);
                                }
                            }
                        }
                        catch (System.Threading.ThreadAbortException)
                        {
                            //teardown got us mid-flight (e.g. world clear / new game starting before
                            //pre-processing finished). benign: the outer worker loop handles it.
                        }
                        catch (Exception e)
                        {
                            LOG.Err("error in _UI.Encyclopedia_PreProcessing\n{0}", e);
                        }
                        
                        sw.Stop();
                        LOG.Msg("_UI.Encyclopedia_PreProcessing, done after {0}s\n~~~~~~~~~~~~~~~", sw.Elapsed.TotalSeconds);
                    } );
                
                return;
            }
            
            LOG.Err("Unexpected codehook '{0}' calling us : Encyclopedia_Codehook_Handler", Hook.InternalName);
        }
    }
    
    #endregion

    #region DropdownOptionThematicGroupType

    public class DropdownOptionThematicGroupType : RowBasedDropdownOption<ThematicGroupType>
    {
        public DropdownOptionThematicGroupType( ThematicGroupType Row ) : base( Row )
        {
        }

        private static readonly ArcenDoubleCharacterBuffer displayNameBuffer = new ArcenDoubleCharacterBuffer( "Window_UnitEncyclopedia-DropdownOptionThematicGroupType-displayNameBuffer" );
        public override string GetOptionNameFromVolatile()
        {
            displayNameBuffer.Add( "<size=85%>" );
            if ( this.Row == null )
                displayNameBuffer.Add( "无" );
            else
            {
                displayNameBuffer.Add( this.Row.GetDisplayName() );
                displayNameBuffer.AddDlcMod(this.Row);
            }

            return displayNameBuffer.GetStringAndResetForNextUpdate();
        }
    }
    
    #endregion
}
