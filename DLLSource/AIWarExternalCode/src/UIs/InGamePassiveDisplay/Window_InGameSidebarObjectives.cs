using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarObjectives : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarObjectives Instance;
        public Window_InGameSidebarObjectives()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Objectives;
        }
        
        private static PoolableGUIGroup.GroupPool<ObjectiveCategoryData> objectiveCategoryDataPool;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarObjectives.CustomParentInstance = this;
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

                if ( Window_InGameSidebarObjectives.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnObjective.Original != null && btnObjectivesHeader.Original != null )
                        {
                            hasGlobalInitialized = true;
                            objectiveCategoryDataPool = new PoolableGUIGroup.GroupPool<ObjectiveCategoryData>( new ObjectiveCategoryData( btnObjectivesHeader.Original ), 5 );
                        }
                    }
                    #endregion
                }

                float currentY = 0f; //start bumped down a bit

                this.OnUpdateObjectives( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnObjectivesHeader.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
            #region OnUpdateObjectives
            public void OnUpdateObjectives( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;
                objectiveCategoryDataPool.Clear( 5 );
                int remainingSubItemsCanAdd = 10;

                bool IsFirstObjectiveCategory = false; 
                for ( int i = 0; i < ObjectiveCategoryTable.Instance.SortedCategories.Count; i++ )
                {
                    ObjectiveCategoryData item = objectiveCategoryDataPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Category = ObjectiveCategoryTable.Instance.SortedCategories[i];
                    item.Update( ref currentY,ref IsFirstObjectiveCategory, ref remainingSubItemsCanAdd );
                }                

                #region Positioning Logic
                //RectTransform rTran = null;
                //{
                //    rTran = btnObjectivesHeader.Instance.Element.RelevantRect;
                //    currentY -= TEXT_ROW_HEIGHTS;

                //    if ( ObjectiveIconsAreOpen )
                //        btnObjectivePool.ApplyItemsInRows( ref currentY, 41f, 180, 40f );
                //    else
                //        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                //}
                #endregion
            }
            #endregion
        }

        public const float TEXT_ROW_HEIGHTS = 25f;
        public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

        #region btnObjectivesHeader
        public class btnObjectivesHeader : ButtonAbstractBase
        {
            public static btnObjectivesHeader Original;
            public btnObjectivesHeader() { if ( Original == null ) Original = this; }

            public ObjectiveCategoryData ParentCategory;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.ParentCategory != null )
                    this.ParentCategory.ToggleOpen();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( ParentCategory == null || ParentCategory.Category == null )
                {
                    Buffer.Add( "空分类！" );
                    return;
                }
                int count = ParentCategory.Category.ActualObjectives_Display.Count;
                if ( count < 1 )
                    Buffer.Add( "<color=#444444>" );

                if ( ParentCategory.GetIsOpen() )
                    Buffer.Add( "(-)  " );
                else
                    Buffer.Add( "(+)  " );

                Buffer.Add( ParentCategory.Category.DisplayName );
                if ( count > 0 )
                    Buffer.Add( "  (" ).Add( count ).Add( ")" );
            }
            public override void HandleMouseover()
            {
                if ( ParentCategory == null || ParentCategory.Category == null )
                    return;
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, ParentCategory.Category.Description );
            }
            public override bool GetShouldBeHidden()
            {
                return this.ParentCategory == null || this.ParentCategory.Category == null || this.ParentCategory.ItemsAvailableInCategory <= 0;
            }
        }
        #endregion

        #region btnObjective
        public class btnObjective : ButtonAbstractBase
        {
            public static btnObjective Original;
            public btnObjective() { if ( Original == null ) Original = this; }

            private int ActualObjectiveID = -1;
            private ActualObjective Objective = null;
            private ObjectiveCategoryData ParentCategory = null;

            public void Assign( ActualObjective Objective, ObjectiveCategoryData ParentCategory )
            {
                this.Objective = Objective;
                if ( this.Objective != null )
                    this.ActualObjectiveID = Objective.ObjectiveInstanceID;
                else
                    this.ActualObjectiveID = -1;
                this.ParentCategory = ParentCategory;
            }

            public override void OnUpdate()
            {
                base.OnUpdate();
                ArcenUI_Button button = this.Element as ArcenUI_Button;
                if ( button != null && button.ReferenceText != null )
                {
                    button.ReferenceText.enableAutoSizing = false;
                    button.ReferenceText.fontSize = 9f;
                }
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int debugCode = 0;
                try
                {
                    debugCode = 100;
                    if ( this.Objective == null )
                    {
                        Buffer.Add( "缺失任务" );
                        return;
                    }
                    
                    debugCode = 200;
                    
                    if ( string.IsNullOrEmpty(Objective.CalculatedDisplayName) )
                    {
                        Buffer.Add( "任务名称为空！" );
                        return;
                    }
                
                    debugCode = 300;
                    Buffer.AddColor( Objective.CalculatedDisplayName, Objective.Hook.DisplayNameColor );
                    
                    debugCode = 310;
                    
                    //play with the text size a bit for longer objective names, so as to not crowd the box
                    {
                        string size = "65%";
                        int len = Objective.CalculatedDisplayName.Length;
                        if (  len < 30 )
                            size = "85%";
                        else 
                        if (  len < 40 )
                            size = "80%";
                        else 
                        if (  len < 50 )
                            size = "70%";
                        
                        Buffer.StartSize(size);
                    }
                    
                    debugCode = 400;

                    if( Objective.DifficultyEstimate > 0 )
                    {
                        debugCode = 500;
                        
                        //Add the difficulty estimate (and colour when appropriate)
                        Buffer.Add("\n").Add("敌方防御估计：" );
                        Buffer.AddColor(Objective.DifficultyEstimate, Objective.ColorByDifficulty);
                    }
                    
                    debugCode = 600;
                    if ( Objective.Importance == ObjectiveImportance.Lower )
                        Buffer.Add(" 重要性：").AddColor(Extensions.ToString(Objective.Importance), "00eeee");
                    
                    debugCode = 700;
                    if ( Objective.Importance == ObjectiveImportance.Higher )
                        Buffer.Add(" 重要性：").AddColor(Extensions.ToString(Objective.Importance), "ff0011");
                    
                    debugCode = 800;
                    Buffer.Add("</size>");
                } 
                catch ( Exception e )
                {
                    string objStr = "";
                    if ( this.Objective != null )
                        objStr = Objective.CalculatedDisplayName;
                    
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetTextToShowFromVolatile for " + objStr + " debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Objective == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( input.RightButtonClicked )
                {
                    //Right click updates the importance, left click does whatever the Objective's defined behaviour is
                    switch ( this.Objective.Importance )
                    {
                        case ObjectiveImportance.Lower:
                            if ( this.Objective.RelatedEntity1 != null )
                                SendImportanceUpdateMessage_Entity( this.Objective.RelatedEntity1, ObjectiveImportance.Normal );
                            else if ( this.Objective.RelatedPlanet1 != null )
                                SendImportanceUpdateMessage_Planet( this.Objective.RelatedPlanet1, ObjectiveImportance.Normal );
                            else
                                World_AIW2.Instance.QueueChatMessageOrCommand( "此任务未关联实体或行星，无法更改其优先级...",
                                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ObjectiveImportance.Normal:
                            if ( this.Objective.RelatedEntity1 != null )
                                SendImportanceUpdateMessage_Entity( this.Objective.RelatedEntity1, ObjectiveImportance.Higher );
                            else if ( this.Objective.RelatedPlanet1 != null )
                                SendImportanceUpdateMessage_Planet( this.Objective.RelatedPlanet1, ObjectiveImportance.Higher );
                            else
                                World_AIW2.Instance.QueueChatMessageOrCommand( "此任务未关联实体或行星，无法更改其优先级...",
                                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ObjectiveImportance.Higher:
                            if ( this.Objective.RelatedEntity1 != null )
                                SendImportanceUpdateMessage_Entity( this.Objective.RelatedEntity1, ObjectiveImportance.Lower );
                            else if ( this.Objective.RelatedPlanet1 != null )
                                SendImportanceUpdateMessage_Planet( this.Objective.RelatedPlanet1, ObjectiveImportance.Lower );
                            else
                                World_AIW2.Instance.QueueChatMessageOrCommand( "此任务未关联实体或行星，无法更改其优先级...",
                                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                    }
                    return MouseHandlingResult.None;
                }

                switch ( this.Objective.Hook.ClickBehavior )
                {
                    case ObjectiveClickBehavior.DoNothing:
                        break;
                    case ObjectiveClickBehavior.UseClickHandler:
                        return this.Objective.Hook.HookManager.ClickHandler( this.Objective );
                    case ObjectiveClickBehavior.CenterOnRelatedEntity1:
                        ObjectiveGenerator.CenteringHelper( this.Objective.RelatedEntity1.Planet, this.Objective.RelatedEntity1 );
                        break;
                    case ObjectiveClickBehavior.CenterOnRelatedEntity1Planet:
                        ObjectiveGenerator.CenteringHelper( this.Objective.RelatedEntity1.Planet, null );
                        break;
                    case ObjectiveClickBehavior.CenterOnRelatedPlanet1:
                        ObjectiveGenerator.CenteringHelper( this.Objective.RelatedPlanet1, null );
                        break;
                    default:
                        ArcenDebugging.ArcenDebugLog( "Undefined click behavior: " + this.Objective.Hook.ClickBehavior, Verbosity.ShowAsError );
                        break;
                }

                return MouseHandlingResult.None;
            }

            private static void SendImportanceUpdateMessage_Entity( GameEntity_Squad Squad, ObjectiveImportance Importance )
            {
                if ( Squad == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "此任务关联的单位为空，设置重要性失败！",
                        ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }

                //this is the part that matters for MP
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetUnit_UIImportance], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Squad.PrimaryKeyID );
                command.RelatedIntegers2.Add( (int)Importance );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                //this part makes things responsive on the local machine
                Squad.ImportanceUIOnly = Importance;
            }

            private static void SendImportanceUpdateMessage_Planet( Planet Plan, ObjectiveImportance Importance )
            {
                if ( Plan == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "此任务关联的行星为空，设置重要性失败！",
                        ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }

                //this is the part that matters for MP
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetPlanet_UIImportance], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Plan.Index );
                command.RelatedIntegers2.Add( (int)Importance );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                //this part makes things responsive on the local machine
                Plan.ImportanceUIOnly = Importance;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarObjectives-btnObjective-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.Objective == null )
                    return;
                
                try
                {
                    Objective.Hook.HookManager.TooltipHandler( tooltipBuffer, Objective );
                    
                    if ( this.Objective.DifficultyEstimate > 0 )
                    {
                        tooltipBuffer.Add("\n\n敌方防御估计（");
                        tooltipBuffer.AddColor(Objective.DifficultyEstimate, Objective.ColorByDifficulty);
                        tooltipBuffer.Add("）基于你所在行星与 ");
                        
                        if ( Objective.RelatedPlanet1 != null )
                            tooltipBuffer.Add( Objective.RelatedPlanet1.Name, Objective.RelatedPlanet1.GetControllingFaction().FactionCenterColor.ColorHexBrighter);
                        else 
                        if ( Objective.RelatedEntity1 != null )
                            tooltipBuffer.Add( Objective.RelatedEntity1.GetPlanetName_Safe(), Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter);
                        else
                            tooltipBuffer.Add("此任务所在行星");
                        
                        tooltipBuffer.Add("之间敌方部队的战力。数值越高意味着完成此任务可能越困难，但你通常可以绕过敌方防御而非全部摧毁。");
                    }
                    
                    //tooltipBuffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "<color=#996f4c>Hold </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTooltips" ) ).Add( "</color> <color=#996f4c>to hide all tooltips.  </color>" );
                    
                    if ( this.Objective.Hook.ImportanceCalculation == ObjectiveImportanceCalculationType.NotSettable )
                        tooltipBuffer.Add("\n\n").Add("此任务未关联行星或实体，因此无法更改其重要性。此情报的当前重要性相当于 ").Add( this.Objective.Importance.ToString(), "fffea1" ).Add("。");
                    else
                        tooltipBuffer.Add( "\n\n" ).Add( "你可以通过右键点击情报项来优先/降低与特定行星或实体相关的情报优先级。\n此情报的当前重要性为 " ).Add( this.Objective.Importance.ToString(), "a1ffa1" ).Add( "。" );
                    
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "GeneralTooltipScale" );
                }
                catch ( Exception e )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "错误：" + e, "GeneralTooltipScale" );
                }

                try
                {
                    switch ( this.Objective.Hook.FocusType )
                    {
                        case ObjectiveHoverFocusType.None:
                            break;
                        case ObjectiveHoverFocusType.FocusOnRelatedEntity1:
                            World_AIW2.Instance.FocusedSquadForMapDarkening = this.Objective.RelatedEntity1;
                            break;
                        case ObjectiveHoverFocusType.FocusOnRelatedPlanet1:
                            World_AIW2.Instance.FocusedPlanetForMapDarkening = this.Objective.RelatedPlanet1;
                            break;
                        case ObjectiveHoverFocusType.DarkenAllPlanet:
                            Planet.SetCurrentlyExplicitlyNoPlanetsHoveredOver();
                            break;
                        case ObjectiveHoverFocusType.FocusUnexploredPlanets:
                            Planet.SetCurrentlyAllUnexploredPlanetsHoveredOver();
                            break;
                        default:
                            ArcenDebugging.ArcenDebugLog( "Undefined ObjectiveHoverFocusType: " + this.Objective.Hook.FocusType, Verbosity.ShowAsError );
                            break;
                    }
                }
                catch { }
            }

            public override bool GetShouldBeHidden()
            {
                if ( this.Objective == null || 
                     this.ParentCategory == null || 
                     !this.ParentCategory.GetIsOpen() ||
                     this.Objective.CalculatedDisplayName == null || 
                     this.Objective.CalculatedDisplayName.Length == 0 ||
                     this.Objective.ObjectiveInstanceID != this.ActualObjectiveID )
                {
                    //make sure this also gets blanked out so it does not flip back in unexpectedly
                    this.Objective = null; 
                    
                    return true;
                }
                
                return false;
            }
        }
        #endregion

        public class ObjectiveCategoryData : PoolableGUIGroup
        {
            private bool? _ThisCategoryIsOpen; // starts as null so the default can be set based on the category (some start open, some start closed)

            public btnObjectivesHeader Button;
            public ObjectiveCategory Category;
            public int ItemsAvailableInCategory = 0;

            private ButtonAbstractBase.ButtonPool<btnObjective> btnObjectivePool;

            public ObjectiveCategoryData( btnObjectivesHeader But )
            {
                this.Button = But;
                this.Button.ParentCategory = this;
            }

            public bool GetIsOpen()
            {
                if ( this._ThisCategoryIsOpen == null )
                {
                    if ( this.Category == null )
                        return true;
                    this._ThisCategoryIsOpen = !this.Category.DefaultCollapsed;
                }
                return this._ThisCategoryIsOpen.Value;
            }

            public void ToggleOpen()
            {
                this._ThisCategoryIsOpen = !this.GetIsOpen();
            }

            public override void PostInit()
            {
                btnObjectivePool = new ButtonAbstractBase.ButtonPool<btnObjective>( btnObjective.Original, 1 );
            }

            public override void Update()
            {
                throw new Exception( "Not really using this update method..." );
            }

            public const float BUTTON_ROW_HEIGHTS = 34f;
            public const float HEADER_ROW_HEIGHTS = 21f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            public void Update( ref float currentY, ref bool IsFirstObjectiveCategory, ref int RemainingSubItemsCanAdd )
            {
                btnObjectivePool.Clear( RemainingSubItemsCanAdd );
                this.ItemsAvailableInCategory = 0;

                ProtectedList<ActualObjective> items = this.Category.ActualObjectives_Display;
                ActualObjective item;
                if ( items != null && RemainingSubItemsCanAdd > 0)
                {
                    for ( int j = 0; j < items.Count; j++ )
                    {
                        item = items[j];
                        btnObjective itemButton = btnObjectivePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( itemButton == null )
                            break; //time slicing, too many added right now
                        itemButton.Assign( item, this );
                        this.ItemsAvailableInCategory++;
                    }
                }

                RemainingSubItemsCanAdd = btnObjectivePool.GetRemainingAllowedToAddBeforeNextClear();

                if ( this.ItemsAvailableInCategory <= 0 )
                    return;

                #region Positioning Logic
                RectTransform rTran = null;
                {
                    if ( IsFirstObjectiveCategory )
                        IsFirstObjectiveCategory = false;
                    else
                    {
                        //spacing between each category, too
                        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                    }

                    rTran = this.Button.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    currentY -= HEADER_ROW_HEIGHTS;

                    if (this.GetIsOpen())
                    {
                        btnObjectivePool.ApplyItemsInRows(0, ref currentY, BUTTON_ROW_HEIGHTS, 180, BUTTON_ROW_HEIGHTS);
                    }
                    else
                        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                }
                #endregion
            }

            public override void Clear()
            {
                this.Category = null;
                btnObjectivePool.Clear( 10 );
            }
            public override PoolableGUIGroup DuplicateSelf()
            {
                btnObjectivesHeader newCategoryBut = (btnObjectivesHeader)this.Button.DuplicateSelf();
                //int siblingIndex = btnHackingHeader.Instance.Element.transform.GetSiblingIndex() - 1;                
                //newCategoryBut.Element.GO.transform.SetSiblingIndex( siblingIndex );

                return new ObjectiveCategoryData( newCategoryBut );
            }
        }
    }
}
