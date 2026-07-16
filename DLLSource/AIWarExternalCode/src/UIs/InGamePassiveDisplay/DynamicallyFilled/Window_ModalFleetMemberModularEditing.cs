using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_ModalFleetMemberModularEditing : Window_DynamicallyFilledAbstractBase
    {
        public static Window_ModalFleetMemberModularEditing Instance;
        
        private FleetMembership _member;
        private bool _open;
        
        public Window_ModalFleetMemberModularEditing()
        {
            Instance = this;
            
            this.topBuffer = -3;
            this.leftBuffer = 5;
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.rowBuffer = 1.5f;
        }

        #region Open/Close Stuff
        
        public override void Close()
        {
            _open = false;
        }
        
        public void Open( FleetMembership member )
        {
            _open = true;
            _member = member;
        }
        
        public bool GetIsOpen()
        {
            return _open;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return _open;
        }

        public override void OnHideAfterShowing()
        {
            _open = false;
            base.OnHideAfterShowing();
        }

        public void Toggle_Show( FleetMembership member )
        {
            if (_open &&
                _member == member)
            {
                this.Close();
                return;
            }
            
            _open = true;
            _member = member;
        }

        public bool Is_Showing( FleetMembership member )
        {
            return _open &&
                   _member == member;
        }

        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=95%>编辑配置：" ).StartColor( "a1ffa1" );

                if ( Instance._member.TypeData.IsModular && Instance._member != null )
                {
                    foreach ( EntitySystemTypeData systemData in Instance._member.TypeData.SystemTypes )
                    {
                        if ( !systemData.IsModule || systemData.ModularFullNamePrefix == null || systemData.ModularFullNamePrefix.Length <= 0 )
                            continue; //skip any non-modules, or things that don't add to the prefix
                        if ( !systemData.IsModuleOn( Instance._member ) )
                            continue; //skip any that are not enabled
                        if ( !systemData.ForMark[Instance._member.EffectiveMark].IsFunctionalAtThisMarkLevel )
                            continue;

                        Buffer.Add( systemData.ModularFullNamePrefix );
                    }
                }
                GameEntity_Squad singleEntity = null;
                if ( Instance._member.EntitiesOfFMem.Count == 1 )
                    singleEntity = Instance._member.EntitiesOfFMem.GetFirst().Contained;

                Buffer.Add( Instance._member.TypeData.GetDisplayName() );
                if ( singleEntity != null )
                    Buffer.Add("</color> - " ).Add( singleEntity.Planet.Name, "a1a1ff" );
                Buffer.Add( "</size>" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalFleetMemberModularEditing-btnTextWithIcon-tooltipBuffer" );

            public override void HandleMouseover()
            {
                GameEntity_Squad singleEntity = null;
                if ( Instance._member.EntitiesOfFMem.Count == 1 )
                    singleEntity = Instance._member.EntitiesOfFMem.GetFirst().Contained;

                EntityText.GetTooltip( tooltipBuffer, singleEntity, Instance._member, null, -1, null, 0, FromSidebarType.Sidebar_MultipleUnits, ShipExtraDetailFlags.BuildInfo, 1f, false );
                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
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

        private static ButtonAbstractBase.ButtonPool<btnTextWithIcon> btnTextWithIconPool = null;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ModalFleetMemberModularEditing.Instance != null )
                {   
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnTextWithIcon.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnTextWithIconPool = new ButtonAbstractBase.ButtonPool<btnTextWithIcon>( btnTextWithIcon.Original, 5 );
                        }
                    }
                    #endregion
                }

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );
            }
        }

        private static readonly List<EntitySystemTypeData> modulesICanConfigure = List<EntitySystemTypeData>.Create_WillNeverBeGCed( 3000, "Window_ModalFleetMemberModularEditing-modulesICanConfigure" );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( btnTextWithIconPool == null )
                return;

            btnTextWithIconPool.Clear( 10 );

            #region Calculate modulesICanConfigure
            modulesICanConfigure.Clear();

            if ( _member != null )
            {
                foreach ( EntitySystemTypeData systemData in _member.TypeData.SystemTypes )
                {
                    if ( systemData.IsModule &&
                         !systemData.SystemIsHiddenForUI )
                    {
                        modulesICanConfigure.Add( systemData );
                    }
                }
            }

            //now sort the modulesICanConfigure so that their order makes some sort of sense!
            modulesICanConfigure.Sort( static delegate ( EntitySystemTypeData left, EntitySystemTypeData right )
            {
                int val = left.ModuleScreenSortGroup.CompareTo( right.ModuleScreenSortGroup ); //ascending by group
                if ( val != 0 )
                    return val;

                val = left.ModulePointCost.CompareTo( right.ModulePointCost );
                if ( val != 0 )
                    return val;

                val = left.DisplayName.CompareTo( right.DisplayName );
                if ( val != 0 )
                    return val;

                return left.UniqueModuleIndex.CompareTo( right.UniqueModuleIndex );
            } );
            #endregion

            float runningY = -3;

            {
                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item != null )
                {
                    item.Assign( "当前未使用点数: <color=#a1a1ff>" + ( Instance._member.FreeModulePoints() ) + "</color>  (+" +
                        Instance._member.TypeData.ModulePointsAddedPerMarkLevel + " 每等级)" );
                }
            }

            {
                EntitySystemTypeData systemData;

                for ( int j = 0; j < modulesICanConfigure.Count; j++ )
                {
                    systemData = modulesICanConfigure[j];

                    btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( systemData );
                }
            }

            if ( modulesICanConfigure.Count <= 0 )
            {
                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item != null )
                {
                    item.Assign( "非模块化船只，或当前无可配置项" );
                }
            }
            if ( Instance._member.CalculateCurrentModuleCost() > 0 )
            {
                //only display the "Suppress notification" if we've spent at least one module point;
                //we want to encourage players to spend them
                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item != null )
                {
                    //For IgnoreUnspentPoints
                    item.Assign( true );
                }
            }

            btnTextWithIconPool.ApplyItemsInRows( 0, ref runningY, 25f, 640f, 24 ); //todo

            #region Positioning Logic
            //Now size the parent, called Content, to get scrollbars to appear if needed.
            RectTransform rTran = (RectTransform)btnTextWithIcon.Original.Element.RelevantRect.parent;
            Vector2 sizeDelta = rTran.sizeDelta;
            sizeDelta.y = Mat.Abs( runningY ) + 10f;
            rTran.sizeDelta = sizeDelta;
            #endregion
        }

        #region btnTextWithIcon
        public class btnTextWithIcon : ButtonAbstractBase
        {
            public static btnTextWithIcon Original;
            public btnTextWithIcon() { if ( Original == null ) Original = this; }

            private EntitySystemTypeData systemData = null;
            private string TextToDisplay;
            private bool IgnoreUnspentPoints;

            private ArcenUIImageArray relatedImages = null;
            private Sprite checkboxOff = null;
            private Sprite checkboxOn = null;

            public void Assign( EntitySystemTypeData systemData )
            {
                this.systemData = systemData;

                InitIfPossible();
                if ( relatedImages != null )
                    relatedImages.SetAllToBlank();

                if ( systemData.IsModuleOn( Instance._member ) )
                    relatedImages.SetSpriteAndColor( 3, this.checkboxOn, Color.white );
                else
                {
                    if ( systemData.MinMarkLevelToFunction > 0 )
                    {
                        if ( Instance._member.EffectiveMark < systemData.MinMarkLevelToFunction )
                            return; //don't draw a checkbox
                    }

                    if ( systemData.MaxMarkLevelToFunction > 0 && systemData.MaxMarkLevelToFunction < 7 )
                    {
                        if ( Instance._member.EffectiveMark > systemData.MaxMarkLevelToFunction )
                            return; //don't draw a checkbox
                    }

                    //we want to enable this, but can only do so if we meet the requirements
                    int moduleCost = systemData.ModulePointCost;
                    if ( moduleCost > Instance._member.FreeModulePoints() )
                        return; //don't draw a checkbox
                    relatedImages.SetSpriteAndColor( 3, this.checkboxOff, Color.white );
                }
            }

            public void Assign( string text )
            {
                this.TextToDisplay = text;

                InitIfPossible();
                if ( relatedImages != null )
                    relatedImages.SetAllToBlank();
            }

            public void Assign( bool ignorePoints )
            {
                this.TextToDisplay = "<color=#00ffff>Ignore Unspent Points</color>";

                InitIfPossible();
                if ( relatedImages != null )
                    relatedImages.SetAllToBlank();
                this.IgnoreUnspentPoints = true;
            }

            private void InitIfPossible()
            {
                if ( relatedImages == null && this.Element != null )
                {
                    relatedImages = new ArcenUIImageArray( ExternalConstants.Instance.BlankGUISprite );
                    relatedImages.InitializeFrom_ArcenUI_Button( this.Element );

                    this.checkboxOff = this.Element.RelatedSprites[0];
                    this.checkboxOn = this.Element.RelatedSprites[1];
                }
            }

            public override bool GetShouldBeHidden()
            {
                return Instance._member == null || ( systemData == null && String.IsNullOrEmpty( this.TextToDisplay ) );
            }

            public override void Clear()
            {
                this.systemData = null;
                this.TextToDisplay = string.Empty;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                //Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                //if ( playerFaction == null )
                //    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                if ( this.systemData != null )
                {
                    Buffer.Add( "<align=left>" );
                    Buffer.Add( "<pos=5>" );

                    Buffer.Add( systemData.DisplayName );

                    Buffer.Add( "<pos=210>" );
                    bool isEnabled = systemData.IsModuleOn( Instance._member );
                    if ( isEnabled )
                    {
                        Buffer.StartColor( "40ff7a" );
                        Buffer.Add( systemData.ModulePointCost );
                    }
                    else
                    {
                        if ( systemData.ModulePointCost + Instance._member.CalculateCurrentModuleCost() > Instance._member.ForMark.ModulePointsAvailable )
                            Buffer.StartColor( "dd40ff" );
                        else
                            Buffer.StartColor( "ff9e40" );
                        Buffer.Add( "0" );
                    }
                    Buffer.Add( "/" );
                    Buffer.Add( systemData.ModulePointCost );
                    Buffer.EndColor();

                    if ( systemData.MinMarkLevelToFunction > 0 )
                    {
                        Buffer.Add( "<pos=280>" );
                        if ( Instance._member.EffectiveMark >= systemData.MinMarkLevelToFunction )
                            Buffer.StartColor( "827971" );
                        else
                            Buffer.StartColor( "fa4491" );

                        Buffer.Add( "Mk" );
                        Buffer.Add( systemData.MinMarkLevelToFunction );
                    }
                }
                else if ( !String.IsNullOrEmpty( this.TextToDisplay ) )
                {
                    Buffer.Add( this.TextToDisplay );
                }
                else
                    Buffer.Add( "空？" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EntitySystemTypeData sysDat = this.systemData;
                if ( sysDat != null )
                {
                    bool isEnabled = sysDat.IsModuleOn( Instance._member );
                    if ( isEnabled )
                    {
                        //we want to disable that, and that's always fine
                    }
                    else
                    {
                        if ( systemData.MinMarkLevelToFunction > 0 )
                        {
                            if ( Instance._member.EffectiveMark < systemData.MinMarkLevelToFunction )
                            {
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "等级过低！", "你的舰线需要更高等级才能使用此模块。", "确定" );
                                return MouseHandlingResult.PlayClickDeniedSound;
                            }
                        }

                        if ( systemData.MaxMarkLevelToFunction > 0 && systemData.MaxMarkLevelToFunction < 7 )
                        {
                            if ( Instance._member.EffectiveMark > systemData.MaxMarkLevelToFunction )
                            {
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "等级过高！", "此模块无法用于如此高等级的单位，因此无法启用。", "确定" );
                                return MouseHandlingResult.PlayClickDeniedSound;
                            }
                        }

                        //we want to enable this, but can only do so if we meet the requirements
                        int moduleCost = sysDat.ModulePointCost;
                        if ( moduleCost > Instance._member.FreeModulePoints() )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "模块点数不足！", "你此舰线的模块点数不能低于零。请禁用一些模块或提升此舰线等级以获得更多点数。", "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                    }

                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditShipModule], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedMagnitude = Instance._member.Fleet.FleetID;
                    command.RelatedBool = !isEnabled; //invert
                    command.RelatedString = Instance._member.TypeData.InternalName;
                    command.RelatedIntegers.Add( Instance._member.UniqueTypeDataDifferentiatorForDuplicates );
                    command.RelatedIntegers.Add( sysDat.UniqueModuleIndex );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
                if ( IgnoreUnspentPoints )
                {
                    //note this uses an EditFleetData GameCommand instead of an EditShipModule
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditShipModule], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedMagnitude = Instance._member.Fleet.FleetID;
                    command.RelatedString = Instance._member.TypeData.InternalName;
                    command.RelatedIntegers2.Add( Instance._member.UniqueTypeDataDifferentiatorForDuplicates );
                    command.RelatedIntegers2.Add( Instance._member.FreeModulePoints() ); //the current number of module points we have avaliable
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    Instance.Close();
                }
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalFleetMemberModularEditing-btnTextWithIcon-tooltipBuffer" );
            public override void HandleMouseover()
            {
                EntitySystemTypeData sysDat = this.systemData;
                if ( sysDat != null )
                {
                    GameEntity_Squad e = null;
                    try
                    {
                        e = EntityText.GetFakeEntity(Instance._member);
                        EntityText.WriteSystem(tooltipBuffer, e.GetSystemById(sysDat.InternalName_Original), EntityText.Setup);
                    }
                    finally
                    {
                        EntityText.ReleaseFakeEntity(e);
                    }
                    
                    tooltipBuffer.Add( "\n当前状态: " );
                    bool isEnabled = systemData.IsModuleOn( Instance._member );
                    if ( isEnabled )
                    {
                        tooltipBuffer.StartColor( "40ff7a" );
                        tooltipBuffer.Add( "已启用" );
                        tooltipBuffer.EndColor();
                    }
                    else
                    {
                        tooltipBuffer.StartColor( "ff9e40" );
                        tooltipBuffer.Add( "已禁用" );
                        tooltipBuffer.EndColor();
                    }

                    tooltipBuffer.Add( "\n模块点数消耗: " );
                    tooltipBuffer.Add( systemData.ModulePointCost );

                    if ( systemData.MinMarkLevelToFunction > 0 )
                    {
                        tooltipBuffer.Add( "\n运行所需的最低舰线等级: " );
                        if ( Instance._member.EffectiveMark >= systemData.MinMarkLevelToFunction )
                            tooltipBuffer.StartColor( "827971" );
                        else
                            tooltipBuffer.StartColor( "fa4491" );

                        tooltipBuffer.Add( "Mk" );
                        tooltipBuffer.Add( systemData.MinMarkLevelToFunction );
                    }

                    if ( systemData.MaxMarkLevelToFunction > 0 && systemData.MaxMarkLevelToFunction < 7 )
                    {
                        tooltipBuffer.Add( "\n运行的最大舰线等级: " );
                        if ( Instance._member.EffectiveMark <= systemData.MaxMarkLevelToFunction )
                            tooltipBuffer.StartColor( "827971" );
                        else
                            tooltipBuffer.StartColor( "fa4491" );

                        tooltipBuffer.Add( "Mk" );
                        tooltipBuffer.Add( systemData.MaxMarkLevelToFunction );
                    }

                    {
                        tooltipBuffer.Add( "\n船只可用模块点数: " );
                        tooltipBuffer.Add( Instance._member.ForMark.ModulePointsAvailable - Instance._member.CalculateCurrentModuleCost() );
                        tooltipBuffer.Add( " / " );
                        tooltipBuffer.Add( Instance._member.ForMark.ModulePointsAvailable );
                        tooltipBuffer.Add( " (+" );
                        tooltipBuffer.Add( systemData.ParentEntityTypeData.ModulePointsAddedPerMarkLevel );
                        tooltipBuffer.Add( " 每等级点数)" );
                    }

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
                else if ( IgnoreUnspentPoints )
                {
                    tooltipBuffer.Add( "此旗舰的未使用模块通知将不再显示，直到你获得更多模块点数或更改模块。" ).Add("\n\n");
                    tooltipBuffer.Add("你仍然可以通过舰队侧边栏更改模块。");
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
                else
                {
                    GameEntity_Squad singleEntity = null;
                    if ( Instance._member.EntitiesOfFMem.Count == 1 )
                        singleEntity = Instance._member.EntitiesOfFMem.GetFirst().Contained;

                    EntityText.GetTooltip( tooltipBuffer, singleEntity, Instance._member, null, -1, null, 0, FromSidebarType.Sidebar_MultipleUnits, ShipExtraDetailFlags.BuildInfo, 1f, false );
                    EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }
        #endregion

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                //do not show the cancel button
                return true;
            }
        }

        public class bOk : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }
    }
}
