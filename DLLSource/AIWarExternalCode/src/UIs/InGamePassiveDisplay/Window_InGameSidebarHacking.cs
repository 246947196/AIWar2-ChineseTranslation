using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarHacking : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarHacking Instance;
        public Window_InGameSidebarHacking()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Hacking;
        }

        private static ButtonAbstractBase.ButtonPool<btnHackingItem> btnHackingItemPool;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarHacking.CustomParentInstance = this;
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

                if ( Window_InGameSidebarHacking.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnHackingItem.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnHackingItemPool = new ButtonAbstractBase.ButtonPool<btnHackingItem>( btnHackingItem.Original, 8 );
                        }
                    }
                    #endregion
                }

                float currentY = 0; //the position of the first entry
                
                this.OnUpdateHacking( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnHackingHeader.Instance.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public const float TEXT_ROW_HEIGHTS = 25f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            #region OnUpdateHacking
            private readonly static List<HackingType> hackingTypesAddedThatAreSingleOnly = List<HackingType>.Create_WillNeverBeGCed( 60, "Window_InGameSidebarHacking-hackingTypesAddedThatAreSingleOnly" );
            public void OnUpdateHacking( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;

                btnHackingItemPool.Clear( 5 );

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                int addedHacks = 0;
                hackingTypesAddedThatAreSingleOnly.Clear();

                string unusedString = string.Empty;
                //first look at hacks on specific units
                foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.Hackable ) )
                {
                    List<HackingType> hackTypes = entity.TypeData.GetListOfHacks();
                    foreach ( HackingType type in hackTypes )
                    {
                        if ( HackingUtils.GetShouldSkipHackOnSidebar( type, localFaction, planet ) )
                            continue;
                        if ( !type.GetIsHackValidAgainst( entity, false ) )
                            continue;
                        if ( type.OnlyShowOncePerPlanetWithMultipleEntitiesGrantingThis )
                        {
                            if ( !hackingTypesAddedThatAreSingleOnly.Contains( type ) )
                            {
                                //we can always add this the first time
                                hackingTypesAddedThatAreSingleOnly.Add( type );
                            }
                            else //if we already added this, then make sure not to show it again
                            {
                                continue;
                            }
                        }
                        GameEntity_Squad hacker = null;
                        if ( type.UseClosestHackerForCheck ||
                             (type.LocalHackerRequired && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) ) )
                        {
                            //Badger note 4/19/22: I suspect the above check should check for LocalHackerRequired
                            //for human empire factions as well, but I don't have time to pursue this further so I'm making the smallest valid change
                            hacker = HackingUtils.GetPreferredHacker( entity, type, planet, false );
                        }
                        switch ( type.Implementation.GetCanBeHacked( entity, hacker, entity.Planet, localFaction, type, string.Empty, -1, out unusedString ) )
                        {
                            case Hackable.AlreadyHasBeenHacked_Hide:
                            case Hackable.NeverCanBeHacked_Hide:
                            case Hackable.NotSureIfHasBeenHackedHacked_Hide:
                              continue;
                        }

                        addedHacks++;
                        btnHackingItem item = btnHackingItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( type, entity );
                    }
                }

                //second look at any hacks that are for the planet itself
                for ( int i = 0; i < HackingTypeTable.Instance.Rows.Count; i++ )
                {
                    HackingType type = HackingTypeTable.Instance.Rows[i];                    
                    if (type.HackIsAgainstPlanet)
                    {
                        if ( HackingUtils.GetShouldSkipHackOnSidebar( type, localFaction, planet ) )
                            continue;
                        if ( type.OnlyShowOncePerPlanetWithMultipleEntitiesGrantingThis )
                        {
                            if ( !hackingTypesAddedThatAreSingleOnly.Contains( type ) )
                            {
                                //we can always add this the first time
                                hackingTypesAddedThatAreSingleOnly.Add( type );
                            }
                            else //if we already added this, then make sure not to show it again
                                continue;
                        }
                        GameEntity_Squad hackerOrNull = null;
                        if ( type.UseClosestHackerForCheck )
                        {
                            hackerOrNull = HackingUtils.GetPreferredHacker( null, type, planet, false );
                        }

                        switch ( type.Implementation.GetCanBeHacked( null, hackerOrNull, planet, localFaction, type, string.Empty, -1, out unusedString ) )
                        {
                            case Hackable.AlreadyHasBeenHacked_Hide:
                            case Hackable.NeverCanBeHacked_Hide:
                            case Hackable.NotSureIfHasBeenHackedHacked_Hide:
                                continue;
                        }

                        addedHacks++;
                        btnHackingItem item = btnHackingItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( type, planet );
                    }
                }

                if ( unusedString != null ) { } //prevent compiler warnings

                if ( addedHacks <= 0 )
                {
                    {
                        btnHackingItem item = btnHackingItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item != null )
                        {
                            item.Assign( HackingExcuse.NothingHackableHere );
                        }
                    }
                }

                #region Positioning Logic
                RectTransform rTran = null;
                {
                    rTran = btnHackingHeader.Instance.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    currentY -= TEXT_ROW_HEIGHTS;
                    btnHackingItemPool.ApplyItemsInRows( 0, ref currentY, 58f, 180, 56f );
                }
                #endregion
            }
            #endregion            
        }
        
        #region btnHackingHeader
        public class btnHackingHeader : ButtonAbstractBase
        {
            public static btnHackingHeader Instance;
            public btnHackingHeader() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //do something
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                Buffer.AddNumberMoreReadable( localFaction.StoredHacking.IntValue );
                localFaction.BaseInfo.WriteAddedHackingHeaderInfo( Buffer );
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "入侵：通用纳米机器人，可用于强化己方单位、破坏敌人或窃取敌人资源。注意你处于目标丰富的环境中，很可能连三分之一的入侵都负担不起。请明智选择入侵目标，占领更多星球或摧毁分配节点以获取更多入侵点数。另外请注意：你对 AI 使用的入侵点数越多，AI 对你入侵的反应就越强。只有针对 AI 拥有的结构所花费的点数才算。你可以在下方查看此星球的可用机会。" );
            }
        }
        #endregion

        #region btnHackingItem
        public class btnHackingItem : ButtonAbstractBase
        {
            public static btnHackingItem Original;
            public btnHackingItem() { if ( Original == null ) Original = this; }

            public HackingExcuse Excuse = HackingExcuse.NoExcuse;
            
            private HackingType _type = null;
            private GameEntity_Squad _target = null;
            private Planet _planet = null;
            private string debugEstimateLog;
            
            public void Assign( HackingType Type, GameEntity_Squad Target )
            {
                this._type = Type;
                this._target = Target;
                this._planet = Target.Planet;
                this.Excuse = HackingExcuse.NoExcuse;
            }

            public void Assign( HackingType Type, Planet planet )
            {
                this._type = Type;
                this._planet = planet;
                this.Excuse = HackingExcuse.NoExcuse;
            }

            public void Assign( HackingExcuse Excuse )
            {
                this._type = null;
                this._target = null;
                this.Excuse = Excuse;
            }

            public override bool GetShouldBeHidden()
            {
                return ( _type == null || (_target == null && _planet == null) ) && this.Excuse == HackingExcuse.NoExcuse;
            }

            public override void Clear()
            {
                this._type = null;
                this._target = null;
                this._planet = null;
                this.Excuse = HackingExcuse.NoExcuse;
            }

            #region GetPlanetName_Safe
            public string GetPlanetName_Safe()
            {
                Planet plan = this._planet;
                if ( plan == null )
                    return "[null planet]";
                return plan.Name;
            }
            #endregion

            // Renders a human-readable, instance-unique identity for an epistyle hacking button.
            // Multiple epistyles of the same type can share a planet, so the type+planet name is not
            // unique on its own; we lead with that meaningful name and append the PrimaryKeyID small at
            // the end as a guaranteed tiebreaker, rather than leading with the raw ID as the older code did.
            private void AddEpistyleNamePrefix( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( this._target.TypeData.GetDisplayName(), "a1a1ff" );
                if ( this._target.Planet != null )
                    buffer.Add( " (" + this._target.Planet.Name + ")", "a1a1ff" );
                buffer.Add( " <size=70%>#" + this._target.PrimaryKeyID.ToString() + "</size>", "8a8aff" );
            }

            public override int CompareSelfWithOther( ButtonAbstractBase O )
            {
                try
                {
                    btnHackingItem other = (btnHackingItem)O;
                    if ( this._type == null || this._target == null )
                        return PREFER_RIGHT;
                    if ( other._type == null || other._target == null )
                        return PREFER_LEFT;
                    if ( this._target == null && other._target != null )
                        return PREFER_LEFT;
                    if ( this._target != null && other._target == null )
                        return PREFER_RIGHT;

                    int val = this._type.InternalName.CompareTo( other._type.InternalName );
                    if ( val != 0 )
                        return val;
                    if ( this._target != null )
                        return this._target.TypeData.GetDisplayName().CompareTo( other._target.TypeData.GetDisplayName() );
                    else
                        return this.GetPlanetName_Safe().CompareTo( other.GetPlanetName_Safe() );
                }
                catch
                {
                    return PREFER_NEITHER;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                switch ( this.Excuse )
                {
                    case HackingExcuse.NoHackerHere:
                        buffer.Add( "你在这个星球上没有入侵" );
                        return;
                    case HackingExcuse.NothingHackableHere:
                        buffer.Add( "此处无可入侵目标" );
                        return;
                }
                try
                {
                    base.GetTextToShowFromVolatile( buffer );
                    
                    if ( this._type == null || (this._target == null && this._planet == null))
                        return;
                    
                    bool canDo = HackingUtils.CalculateCanDoThisHack( ref this.lastNoHackReason, this._target, this._planet, this._type );

                    bool isOpenFleetManagementWindow = Window_HackChoicesSidebarPopout.Instance.Is_Showing( this._target, this._planet, this._type );
                    
                    if ( isOpenFleetManagementWindow )
                        buffer.StartColor( ColorMath.BrightLeaf );
                    else 
                    if ( !canDo )
                        buffer.Add( "<color=#f24f1c>" );
                    
                    if ( this._type.ShowFleetNameInHackingButton && this._target.FleetMembership.Fleet != null )
                    {
                        //modify the output text size for
                        int nameLength = this._target.FleetMembership.Fleet.GetName().Length;
                        int transformSize = 85;
                        int flagshipSize = 80;
                        if ( nameLength < 10 )
                        {
                            transformSize = 90;
                            flagshipSize = 85;
                        }
                        else if ( nameLength > 20 )
                        {
                            transformSize = 80;
                            flagshipSize = 75;
                        }
                        buffer.Add( "<size=" + transformSize + "%>" + this._type.GetDisplayName( this._target?.TypeData, true ) + ":</size>");
                        buffer.Add(" <size=" + flagshipSize + "%>" + this._target.FleetMembership.Fleet.GetName() + "</size>");
                    }
                    else if ( this._type.ShowEpistyleDepositInformation && this._target.TypeData.GetHasTag("DZEpistyle"))
                    {
                        AddEpistyleNamePrefix( buffer );
                        buffer.Add(" 主教：存入资源");
                        DarkZenithPerUnitBaseInfo depositData = this._target.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                        if ( depositData != null )
                        {
                            buffer.Add(" (需要：");
                            depositData.FillEpistyleNeedsBuffer( buffer );
                            buffer.Add(")");
                        }
                    }
                    else if ( this._type.ShowEpistyleConversionInformation && this._target.TypeData.GetHasTag("DZEpistyle"))
                    {
                        AddEpistyleNamePrefix( buffer );
                        buffer.Add(" 主教：");
                        DarkZenithPerUnitBaseInfo data = this._target.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                        if ( data != null )
                        {
                            data.FillHackingBuffer( buffer );
                        }
                        if ( data.KeepConversion )
                        {
                            buffer.Add(" (已锁定)", "ffa1a1");
                        }
                        if ( data.HighPriority )
                        {
                            buffer.Add(" (优先)", "a1a1ff");
                        }
                    }
                    else if ( this._type.ShowEpistyleRallyInformation && this._target.TypeData.GetHasTag("DZEpistyle"))
                    {
                        AddEpistyleNamePrefix( buffer );
                        buffer.Add(" 主教：");
                        DarkZenithPerUnitBaseInfo data = this._target.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                        if ( data != null )
                        {
                            GameEntity_Squad flagship = data.PreferredFlagship.GetSquad();
                            if ( flagship == null )
                            {
                                buffer.Add("最近旗舰");
                            }
                            else
                            {
                                buffer.Add(flagship.TypeData.GetDisplayName(), "ffa1a1").Add( " - ").Add( flagship.Planet.Name, "a1a1ff");
                            }
                        }
                    }

                    else
                        buffer.Add( this._type.GetDisplayName( this._target?.TypeData, true ) ); //normal code path

                    if ( this._type.NumberOfTimesIndividualUnitCanBeHacked > 1 && this._target != null && this._target.FleetMembership != null )
                    {
                        int timesDoneSoFar = this._target.GetNumberOfTimesHacked( this._type );
                        int remain = this._type.NumberOfTimesIndividualUnitCanBeHacked - timesDoneSoFar;
                        buffer.Add( " (" ).Add( remain ).Add( "/" ).Add( this._type.NumberOfTimesIndividualUnitCanBeHacked ).Add( ")" );
                    }

                    if ( !canDo || isOpenFleetManagementWindow )
                        buffer.Add( "</color>" );
                    
                    buffer.Add( "\n" );

                    //Draw hacking costs. Necromancer hacks can cost either Hacking, Essence or both
                    //Human empire  hacks only cost Hacking. It's also possible for hacks to cost metal
                    //for mods
                    bool showHackingCost = true;
                    if ( this._type.BaseCostInMetal > 0 )
                    {
                        buffer.Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                        buffer.AddNumberMoreReadable( _type.BaseCostInMetal.IntValue ).Add(" ");
                        if (  !this._type.DoesHackUseHackingCost() )
                            showHackingCost = false; //this is an Essence-only hack
                    }
                    if ( this._type.BaseCostInResourceOne > 0 )
                    {
                        buffer.AddResourceOne_MoreReadable( _type.GetResourceOneCostForTarget( this._target ).IntValue, true ).Add( " " );
                        if (  !this._type.DoesHackUseHackingCost() )
                            showHackingCost = false; //this is an Essence-only hack
                    }
                    bool isCostPerSecond = false;
                    if ( showHackingCost )
                    {
                        buffer.Add( ArcenExternalUIUtilities.HackingTextColorAndIcon );
                        if ( this._type.HaveSidebarRequestHackingPointCostRange )
                        {
                            FInt minCost, maxCost;
                            this._type.Implementation.GetMinAndMaxCostToHackForSidebar( this._target, this._planet, 
                                                                                       World_AIW2.Instance.GetLocalPlayerFactionOrNull(), this._type, out minCost, out maxCost );

                            if ( minCost.IntValue == maxCost.IntValue )
                                buffer.AddNumberMoreReadable( minCost.IntValue );
                            else
                            {
                                buffer.AddNumberMoreReadable( minCost.IntValue );
                                buffer.Add( "-" );
                                buffer.AddNumberMoreReadable( maxCost.IntValue );
                            }
                        }
                        else
                        {
                            if ( this._type.GetIsPerSecondStyleCost() )
                            {
                                FInt Cost = this._type.GetPerSecondCostToHack( this._target, this._planet );
                                buffer.Add( Cost.ToFloatNonSim().ToString( "#,##0.##/s" ) );
                            }
                            else
                            {
                                FInt Cost = this._type.GetLumpSumCostToHack( this._target, this._planet );
                                //Hacking points are whole numbers; show the integer value so the formatter
                                //doesn't render a spurious decimal like "100.00" (matches the cost-range path above).
                                buffer.AddNumberMoreReadable( Cost.IntValue );
                            }
                        }
                    }

                    //draw TIME to hack

                    int strength;
                    ArcenCharacterBuffer debugBuffer;
                    debugEstimateLog = String.Empty;
                    int difficulty = this._type.Implementation.EstimateTotalDifficulty( this._type, this._target, this._planet, out strength, out debugBuffer );
                    if ( debugBuffer != null )
                        debugEstimateLog = debugBuffer.ToStringAndReturnToPool();
                    buffer.Add( "<pos=50>" );
                    if ( strength != -1 )
                    {
                        //not everything has an estimate
                        strength /= 1000;
                        if ( strength < 1 )
                            strength = 1;
                        buffer.Add("<size=75%> ").Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon).Add(strength).Add(" </size>");
                    }
                    buffer.Add( "<pos=90>" );
                    string color = HackingTypeTable.Instance.TranslateHackingDifficultyToColor(difficulty);
                    buffer.Add("<color=#"+color+">");
                    if ( this._type.HackCompletesInstantly )
                    {
                        buffer.Add( "瞬间完成" );
                    }
                    else
                    {
                        if ( isCostPerSecond )
                        {
                            buffer.Add( "可变持续时间" );
                        }
                        else
                        {
                            int totalSeconds = this._type.Implementation.GetTotalSecondsToHack( this._target, this._planet, null, this._type );
                            buffer.Add( totalSeconds ).Add( "s" ).Add( " 持续时间" );
                        }
                    }
                    buffer.Add("</color>");
                }
                catch { } //sometimes bad things happen to good threads.  In seriousness, threads can compete here and cause nullrefs
            }

            private string lastNoHackReason;
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this._type == null || (this._target == null && this._planet == null) )
                    return MouseHandlingResult.None;

                //even if can't do the hack, allow opening the sub-window if that's what happens.  There's info in there!
                if ( this._type.ChooseASpecificShipLineToGrant || 
                     this._type.ChooseASpecificShipLineToIncreaseCount ||
                     this._type.ChooseASpecificPlanetToTarget || 
                     this._type.ChooseASpecificNecropolisToTarget || 
                     this._type.Implementation.GetDoesHackRequireASinglularChoiceFromASubmenu() )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker(this._target, this._type, _planet, false );
                    Window_HackChoicesSidebarPopout.Instance.Toggle_Show( this._target, this._planet, this._type, hacker );
                    
                    return MouseHandlingResult.None;
                }

                if ( !HackingUtils.CalculateCanDoThisHack( ref this.lastNoHackReason, this._target, this._planet, this._type ) )
                {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( this._target, this._type, _planet, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "你确定吗", "你确定要执行入侵 " + this._type.DisplayName + " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 以跳过本次提示。</color>", "是，入侵", "不，取消" );
                }
                else
                {
                    DoHack();
                }

                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( this._target, this._type, _planet, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                HackingUtils.TryDoHack( ref lastNoHackReason, this._target, this._planet, this._type, null, -1, null );
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarHacking-btnHackingItem-tooltipBuffer" );
            public override void HandleMouseover()
            {
                // Don't show the tooltip for the currently open one
                // otherwise we can't even see its choices.
                if (Window_HackChoicesSidebarPopout.Instance.Is_Showing(_target, _planet, _type))
                    return;

                switch ( this.Excuse )
                {
                    case HackingExcuse.NoHackerHere:
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "要执行大多数入侵，你需要在该星球上拥有舰队。", "GeneralTooltipScale" );
                        return;
                    case HackingExcuse.NothingHackableHere:
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "此面板显示当前星球上可入侵的目标。目前这里没有可入侵的目标。没关系！要找到入侵目标，请查看目标标签页和/或进行更多侦察。", "GeneralTooltipScale" );
                        return;
                }

                if ( this._type == null || (this._target == null && this._planet == null) )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "空类型或目标！", "GeneralTooltipScale" );
                    return;
                }
                
                //GameEntity.SetCurrentlyHoveredOver( this.Target, FromSidebarType.NotFromSidebar );
                tooltipBuffer.Add( this._type.GetDescription( this._target?.TypeData ) );

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                string dynamicDescription = this._type.Implementation.GetDynamicDescription(this._target, null, this._planet, localFaction, this._type);
                if ( !string.IsNullOrWhiteSpace( dynamicDescription ) )
                {
                    tooltipBuffer.Add( "\n" ).Add( dynamicDescription );
                }
                Planet relatedPlanet = this._target == null ? this._planet : this._target.Planet;

                if ( this._target != null && !this._type.SkipWritingWhoThisHackIsAgainst )
                    tooltipBuffer.Add( "\n此入侵针对 " ).Add( this._target.TypeData.GetDisplayName() ).Add( "。</color>" );

                    //if ( this.Type.LocalHackerRequired )
                    //{
                    //    GameEntity_Squad hacker = Window_InGameSidebarHacking.GetClosestHacker(this.Target, this.Type, planet, false );
                    //    if ( hacker != null )
                    //        tooltipBuffer.Add( " 此入侵由舰队执行 " ).Add( hacker.GetFleetName_Safe() ).Add( ".";
                    //}

                if ( this._type.MultiplierToHackingPointCostWhenOnHostileWorld > FInt.One )
                {
                    if ( relatedPlanet != null )
                    {
                        Faction ownerFaction = relatedPlanet.GetControllingFaction();
                        if ( ownerFaction == null || !ownerFaction.GetIsHostileToLocalFaction() )
                        { } //if not owned, or not hostile to the local faction (any local faction is a human), skip
                        else
                        {
                            tooltipBuffer.Add( "\n因为这是敌对世界，入侵费用将 <color=#ff705d> " ).AddFixedDecimalThousands( this._type.MultiplierToHackingPointCostWhenOnHostileWorld.ToDouble(), 2 ).Add( " 倍于正常费用</color>。" );
                        }
                    }
                    else
                    {
                        tooltipBuffer.Add( "\n如果在敌对世界执行此入侵，费用将 <color=#ff705d> " ).AddFixedDecimalThousands( this._type.MultiplierToHackingPointCostWhenOnHostileWorld.ToDouble(), 2 ).Add( " 倍于正常费用</color>。" );
                    }
                }
                if ( this._type.MultiplierToHackingTimeWhenOnNeutralOrFriendlyWorld < FInt.One &&
                    this._type.MultiplierToHackingTimeWhenOnNeutralOrFriendlyWorld > FInt.Zero )
                {
                    if ( relatedPlanet != null )
                    {
                        Faction ownerFaction = relatedPlanet.GetControllingOrInfluencingFaction();
                        if ( ownerFaction != null && ownerFaction.GetIsHostileToLocalFaction() )
                        { } //if owned by hostile faction, skip
                        else
                        {
                            tooltipBuffer.Add( "\n因为这是非敌对世界，入侵时间将为 <color=#ff705d> " ).AddFixedDecimalThousands( this._type.MultiplierToHackingTimeWhenOnNeutralOrFriendlyWorld.ToDouble(), 2 ).Add( " 倍于正常时间</color>。这通常也意味着敌人对此入侵的整体反应强度会大幅降低。" );
                        }
                    }
                    else
                    {
                        tooltipBuffer.Add( "\n如果在非敌对世界执行此入侵，时间将为 <color=#ff705d> " ).AddFixedDecimalThousands( this._type.MultiplierToHackingTimeWhenOnNeutralOrFriendlyWorld.ToDouble(), 2 ).Add( " 倍于正常时间</color>。这通常也意味着敌人对此入侵的整体反应强度会大幅降低。" );
                    }
                }
                if ( relatedPlanet != null && relatedPlanet.IsEligibleForDeepStrike )
                {
                    if ( this._type.HackCostMultiplierOnDeepstrikeWorlds > FInt.One )
                    {
                        if ( this._type.HackTimeMultiplierOnDeepstrikeWorlds > FInt.One )
                            tooltipBuffer.Add( "\n因为此入侵位于纵深打击区域（详见银河地图过滤器），费用将为 " ).AddFixedDecimal( this._type.HackCostMultiplierOnDeepstrikeWorlds.ToDouble(), 2 ).Add( " 倍于正常费用，耗时将为 " ).AddFixedDecimal( this._type.HackTimeMultiplierOnDeepstrikeWorlds.ToDouble(), 2 ).Add( " 倍于正常时间。" );
                        else
                            tooltipBuffer.Add( "\n因为此入侵位于纵深打击区域（详见银河地图过滤器），费用将为 " ).AddFixedDecimal( this._type.HackCostMultiplierOnDeepstrikeWorlds.ToDouble(), 2 ).Add( " 倍于正常费用。" );
                    }
                    else
                    {
                        if ( this._type.HackTimeMultiplierOnDeepstrikeWorlds > FInt.One )
                            tooltipBuffer.Add( "\n因为此入侵位于纵深打击区域（详见银河地图过滤器），耗时将为 " ).AddFixedDecimal( this._type.HackTimeMultiplierOnDeepstrikeWorlds.ToDouble(), 2 ).Add( " 倍于正常时间。" );
                    }
                }
                if ( this._type.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                {
                    if ( this._target != null && this._target.FleetMembership != null )
                    {
                        int timesDoneSoFar = this._target.GetNumberOfTimesHacked( this._type );
                        int remain = this._type.NumberOfTimesIndividualUnitCanBeHacked - timesDoneSoFar;
                        tooltipBuffer.Add( "\n此入侵还可对特定目标执行 <color=#ffe04d>" ).Add( remain ).Add( " 次（共 " ).Add( this._type.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 次）</color>。" );
                    }
                    else
                        tooltipBuffer.Add( "\n此入侵只能对特定目标执行 <color=#ffe04d>" ).Add( this._type.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 次</color>。" );
                }

                if ( this._type.CostMultiplierPerAttempt > FInt.One )
                    tooltipBuffer.Add( "\n每次 " ).Add( this._type.GetDisplayName( this._target?.TypeData, false ) ).Add( " 入侵都会使下一次同类入侵的费用永久增加 " ).Add( this._type.CostMultiplierPerAttempt.ReadableString ).Add( " 倍。" );
                if ( this._type.ScienceToGrantOnCompletion > 0 )
                    tooltipBuffer.Add( "\n完成此入侵将获得 <color=#7CE9FF>" ).Add( this._type.ScienceToGrantOnCompletion ).Add( "</color> 科技。" );
                if ( this._type.AIPOnCompletion > FInt.Zero )
                    tooltipBuffer.Add( "\n此入侵完成后，AIP 将增加 <color=#ffbca1> " ).Add( this._type.AIPOnCompletion ).Add( "</color>。" );
                if ( this._type.AIPOnFailedHack > FInt.Zero )
                    tooltipBuffer.Add( "\n如果此入侵失败或取消，AIP 将增加 <color=#ffbca1> " ).Add( this._type.AIPOnFailedHack ).Add( "</color>。" );
                if ( this._type.ExoOnCompletion )
                    tooltipBuffer.Add( "\n此入侵完成后，AI 将对你发动反击。" );
                if ( this._type.PeriodicExoInterval > 0)
                    tooltipBuffer.Add( "\n在此入侵期间，AI 将定期对你发动大规模反击。" );
                if ( !this._type.HackIsAgainstPlanet && this._target != null && this._target.GetFactionTypeSafe() != FactionType.Player && this._target.GetFactionTypeSafe() != FactionType.NaturalObject )
                {
                    if ( this._type.HackingPointsAgainstThisFactionPercentageRecordedModifer <= FInt.Zero )
                        tooltipBuffer.Add( "\n这是一个中等隐蔽性的入侵。虽然对此特定目标的后续入侵可能会增加强度，但目标阵营的整体入侵反应强度不会增加。" );
                    else if ( this._type.HackingPointsAgainstThisFactionPercentageRecordedModifer < FInt.One )
                        tooltipBuffer.Add( "\n这是一个有一定隐蔽性的入侵。虽然对此特定目标的后续入侵可能会增加强度，但目标阵营的整体入侵反应强度只会增加正常值的 ").AddFixedDecimal( this._type.HackingPointsAgainstThisFactionPercentageRecordedModifer.ToDouble(), 2 ).Add( " 倍。花费的入侵点数越多，增幅越大。" );
                    else if ( this._type.HackingPointsAgainstThisFactionPercentageRecordedModifer > FInt.One )
                        tooltipBuffer.Add( "\n这是一个令敌人非常头疼的入侵。目标阵营的整体入侵反应强度将增加正常值的 " ).AddFixedDecimal( this._type.HackingPointsAgainstThisFactionPercentageRecordedModifer.ToDouble(), 2 ).Add( " 倍。花费的入侵点数越多，增幅越大。" );
                    else
                        tooltipBuffer.Add( "\n此入侵不会被忽视。目标阵营的整体入侵反应强度将在此次入侵后增加。花费的入侵点数越多，增幅越大。" );
                }

                tooltipBuffer.Add("\n\n");

                if ( this._type.LocalHackerRequired )
                {
                    if ( this._type.HackerMustBeBattlestation )
                        tooltipBuffer.Add( "<color=#59ffa3>入侵必须是此星球上的战斗阵地或堡垒。</color>" );
                    else
                    {
                        if ( this._type.HackerCanBeBattlestation )
                        {
                            if ( this._type.HackerCanBeSupportFleet )
                            {
                                tooltipBuffer.Add( "<color=#59ffa3>入侵可以是此星球上的运输船、军官、支援机动兵工厂或战斗阵地。</color>" );
                            }
                            else
                            {
                                tooltipBuffer.Add( "<color=#59ffa3>入侵可以是此星球上的运输船、军官或战斗阵地。</color>" );
                            }
                        }
                        else
                        {
                            if ( this._type.HackerCanBeSupportFleet )
                            {
                                tooltipBuffer.Add( "<color=#59ffa3>入侵可以是此星球上的运输船、军官或支援机动兵工厂。</color>" );
                            }
                            else
                            {
                                tooltipBuffer.Add( "<color=#59ffa3>入侵可以是此星球上的运输船或军官。</color>" );
                            }
                        }
                    }

                    tooltipBuffer.Add("\n");
                }

                if ( !HackingUtils.CalculateCanDoThisHack( ref this.lastNoHackReason, this._target, this._planet, this._type ) )
                    tooltipBuffer.Add( "<color=#f25e1c>" ).Add( this.lastNoHackReason ).Add( "</color>\n\n" );
                else
                    tooltipBuffer.Add( "<color=#59ffa3>你可以执行此入侵。</color>\n\n");

                tooltipBuffer.Add( "<color=#a1ff1a>战力估算更像是 guidelines,只是一个粗略的近似值。</color>\n<color=#ffa1a1>并非所有入侵都有战力估算</color>。" );

                if ( !string.IsNullOrEmpty( debugEstimateLog ) )
                    tooltipBuffer.Add( "\nDebug estimate log: " + debugEstimateLog + "." );

                if ( this._type.ChooseASpecificShipLineToGrant || this._type.ChooseASpecificShipLineToIncreaseCount ||
                     this._type.ChooseASpecificPlanetToTarget || this._type.Implementation.GetDoesHackRequireASinglularChoiceFromASubmenu() )
                {
                    tooltipBuffer.Add( "\n<color=#3f6c9e>此入侵将在执行前打开一个子窗口，提供更多选项。</color>  " );
                }
                else
                {
                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过特定入侵的'你确定吗'提示。</color>  " );
                    }
                }

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion
    }
}
