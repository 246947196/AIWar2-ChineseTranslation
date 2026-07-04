using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_LoadTutorialsMenu : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_LoadTutorialsMenu Instance;
        public Window_LoadTutorialsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( World_AIW2.Instance == null )
                return false;
            if ( World_AIW2.Instance.InSetupPhase )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        private bool IsOpen;
        public TutorialCategory CurrentTutorialCategory = null;
        public void Open()
        {
            if ( this.IsOpen )
                return;
            this.IsOpen = true;
            this.PopulateListOfTutorialCategories();
            if ( SortedTutorialCategoryNames.Count == 0 )
            {
                //make sure they get added to categories if not already there
                TutorialTable.Instance.DoPostInitializationPreSortingLogic_BackgroundThreads();
                //make sure they get sorted and added to the central sorted list
                TutorialCategoryTable.Instance.DoPostAllLoadingFinalLinkingLogic();
                //now try again
                this.PopulateListOfTutorialCategories();
            }
            if ( SortedTutorialCategoryNames.Count == 0 )
                this.CurrentTutorialCategory = null;
            else
                this.CurrentTutorialCategory = SortedTutorialCategoryNames[0];
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            this.CurrentTutorialCategory = null;
        }

        public static readonly List<TutorialCategory> SortedTutorialCategoryNames = List<TutorialCategory>.Create_WillNeverBeGCed( 60, "Window_LoadTutorialsMenu-SortedTutorialCategoryNames" );

        public void PopulateListOfTutorialCategories()
        {
            SortedTutorialCategoryNames.Clear();
            TutorialCategory cat;
            for ( int i= 0; i < TutorialCategoryTable.SortedCategories.Count; i++ )
            {
                cat = TutorialCategoryTable.SortedCategories[i];
                if ( cat != null && cat.Tutorials.Count > 0 )
                    SortedTutorialCategoryNames.Add( cat );
            }
        }

        private static ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;
        private static ButtonAbstractBase.ButtonPool<bTutorial> bTutorialPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LoadTutorialsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null && bTutorial.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 20 );
                            bTutorialPool = new ButtonAbstractBase.ButtonPool<bTutorial>( bTutorial.Original, 60 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateTutorialCategories();
                this.OnUpdateTutorials();
            }

            public void OnUpdateTutorialCategories()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                bCategoryPool.Clear( 30 );

                for ( int i = 0; i < SortedTutorialCategoryNames.Count; i++ )
                {
                    TutorialCategory cat = SortedTutorialCategoryNames[i];
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( cat );
                }

                #region Positioning Logic 1
                bCategoryPool.ApplyItemsInRows( 5, ref currentY, 36, 278, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public void OnUpdateTutorials()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                bTutorialPool.Clear( 40 );

                if ( Instance.CurrentTutorialCategory == null )
                {
                    if ( SortedTutorialCategoryNames.Count == 0 )
                        Instance.PopulateListOfTutorialCategories();
                    if ( SortedTutorialCategoryNames.Count > 0 )
                        Instance.CurrentTutorialCategory = SortedTutorialCategoryNames[0];
                    if ( Instance.CurrentTutorialCategory == null )
                        return;
                }

                for ( int i = 0; i < Instance.CurrentTutorialCategory.Tutorials.Count; i++ )
                {
                    Tutorial tut = Instance.CurrentTutorialCategory.Tutorials[i];
                    bTutorial item = bTutorialPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( tut );
                }

                #region Positioning Logic 1
                bTutorialPool.ApplyItemsInRows( 5, ref currentY, 36, 673.3f, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bTutorial.Original.Element.RelevantRect.parent;
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

            private TutorialCategory Category = null;

            public void Assign( TutorialCategory Cat )
            {
                this.Category = Cat;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Category == null;
            }

            public override void Clear()
            {
                this.Category = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Category == null )
                    return;
                if ( this.Category == Instance.CurrentTutorialCategory )
                    buffer.Add( "<color=#fff36f>" );
                buffer.Add( this.Category.DisplayName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentTutorialCategory = this.Category;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                if ( this.Category == null )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, this.Category.Description );
            }
        }
        #endregion

        #region bTutorial
        public class bTutorial : ButtonAbstractBase
        {
            public static bTutorial Original;
            public bTutorial() { if ( Original == null ) Original = this; }

            private Tutorial tutorial = null;

            public void Assign( Tutorial tut )
            {
                this.tutorial = tut;
            }

            public override bool GetShouldBeHidden()
            {
                return this.tutorial == null;
            }

            public override void Clear()
            {
                this.tutorial = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.tutorial == null )
                    return;

                buffer.Add( this.tutorial.DisplayName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.tutorial == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                if ( World.Instance.IsLoaded )
                    return MouseHandlingResult.None;
                                
                SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                Instance.Close();

                Engine_Universal.ClearAllTraceOfExistingGame();

                World.Instance.AllPlayerAccounts.Clear();
                PlayerAccount playerAccount = PlayerAccount.CreateBrandNew_AndAddToListOfAccounts( PlayerProfile.Local.DisplayName,
                    PlayerProfile.Local.GameSpecificSubObject.GetFactionCenterColorLookupName(),
                    PlayerProfile.Local.GameSpecificSubObject.GetFactionTrimColorLookupName(), true );
                PlayerAccount.SetLocalPlayerAccount( playerAccount );

                //We are basically setting up the few things like the lobby would do, and then having it
                //load from there.
                World.Instance.CampaignName = "Tutorial";
                World_AIW2.Instance.TutorialOrNull = this.tutorial;
                World_AIW2.Instance.Setup.ScenarioDoNotCallDirectly = this.tutorial.Scenario;
                World_AIW2.Instance.Setup.SetUpDefaultFactionConfigurations( false );
                World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;

                World_AIW2.Instance.AssignPlayersToFactionsAfterClearingAllPlayerFactionLinks( false, true, true );

                Engine_AIW2.Instance.GenerateNewWorldThatIsBlankForLobbyOrIsPopulatedByTutorialOrTestChamber( this.tutorial.Scenario, this.tutorial, StartWorldSource1.StartATutorial, StartWorldSource2.NotLoadingAnything );
                
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LoadTutorialsMenu-bTutorial-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.tutorial == null )
                    return;

                tooltipBuffer.Add( "<b>" ).Add( this.tutorial.DisplayName ).Add( "</b>" );
                if ( this.tutorial.Description != null && this.tutorial.Description.Length > 0 )
                {
                    tooltipBuffer.Add( "\n" ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING );
                    tooltipBuffer.Add( this.tutorial.Description );
                }

                tooltipBuffer.Add("\n\n").Add(FontSizes.MUCH_SMALLER_SIZE_STRING).Add("<color=#999999>点击此处开始此教程。");

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region tHeaderTextTop
        public class tHeaderTextTop : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "开始新教程战役" );
            }
        }
        #endregion

        #region tHeaderText2
        public class tHeaderText2 : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "类别" );
            }
        }
        #endregion

        #region tHeaderText3
        public class tHeaderText3 : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "可用教程场景 - " ).Add( Instance.CurrentTutorialCategory == null ? "无" : Instance.CurrentTutorialCategory.DisplayName );
            }
        }
        #endregion

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "返回" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    if ( Engine_Universal.CurrentPopups.Count <= 0 )
                    {
                        this.Close();
                        //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                        ArcenInput.BlockForAJustPartOfOneSecond();
                    }
                    break;
            }
        }
    }
}
