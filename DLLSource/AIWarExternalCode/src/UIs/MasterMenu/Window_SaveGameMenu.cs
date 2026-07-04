using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public class Window_SaveGameMenu : ToggleableWindowController, IInputActionHandler
    {
        public static Window_SaveGameMenu Instance;

        public bool debug;
        public Window_SaveGameMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.debug = false;
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

        private CampaignOrQuickstartGroup CurrentCampaign;
        
        public override void OnOpen()
        {
            if (string.IsNullOrEmpty(World.Instance.CampaignName))
            {
                if ( Engine_AIW2.Instance.IsTestChamber )
                    World.Instance.CampaignName = "Test Chamber";
                else if ( World_AIW2.Instance.TutorialOrNull != null )
                    World.Instance.CampaignName = "Tutorial";
                else
                    World.Instance.CampaignName = "General";
            }
            
            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Overall campaign is: " + World.Instance.CampaignName, Verbosity.DoNotShow );
            
            var name = World.Instance.CampaignName;
            SaveLoadMethods.GetCampaign( name, true, out CurrentCampaign);
            SaveLoadMethods.PopulateSavesInGroup(CurrentCampaign);
        }

        //public static List<SaveGameData> SortedSavegamesInCampaign = List<SaveGameData>.Create_WillNeverBeGCed( 600, "Window_SaveGameMenu-SortedSavegamesInCampaign" );

        /*
        public void PopulateListOfSavegamesInCampaign()
        {
            if ( World.Instance.CampaignName == null || World.Instance.CampaignName.Length <= 0 )
                return;

            CampaignOrQuickstartGroup campaign = CampaignOrQuickstartGroup.Create( World.Instance.CampaignName, CampaignOrQuickstartGroup.Types.Save );
            campaign.Directories.Add( CampaignOrQuickstart_SubFolder.Create( Engine_Universal.CurrentPlayerDataDirectory + "Save/" + World.Instance.CampaignName + "/", null, null ) );

            SaveLoadMethods.PopulateSavesInGroup( campaign );
        }
        */

        public bool GetDoesAlreadyHaveSaveGameName( string Name )
        {
            for ( int i = 0; i < CurrentCampaign.SortedSavesInFolder.Count; i++ )
            {
                if ( CurrentCampaign.SortedSavesInFolder[i].saveName == Name )
                    return true;
            }
            
            return false;
        }

        private static ButtonAbstractBase.ButtonPool<bSavegame> btnSavegamePool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LoadGameMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bSavegame.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnSavegamePool = new ButtonAbstractBase.ButtonPool<bSavegame>( bSavegame.Original, 60 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateSavegames();
            }

            public void OnUpdateSavegames()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnSavegamePool.Clear( 60 );

                for ( int i = 0; i < Instance.CurrentCampaign.SortedSavesInFolder.Count; i++ )
                {
                    SaveGameData save = Instance.CurrentCampaign.SortedSavesInFolder[i];
                    bSavegame item = btnSavegamePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; 
                    item.Assign( save );
                }

                RectTransform rTran = (RectTransform)bSavegame.Original.Element.RelevantRect.parent;
                
                btnSavegamePool.ApplyItemsInRows( 5, ref currentY, 26, rTran.rect.width-10, 24 );

                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
            }
        }

        #region bSavegame
        public class bSavegame : ButtonAbstractBase
        {
            public static bSavegame Original;
            public bSavegame() { if ( Original == null ) Original = this; }

            private SaveGameData save = null;

            public void Assign( SaveGameData save )
            {
                this.save = save;
            }

            public override bool GetShouldBeHidden()
            {
                return this.save == null;
            }

            public override void Clear()
            {
                this.save = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.save == null || this.save.saveName == null )
                    return;

                buffer.Add( "<size=13>" );
                buffer.Add( this.save.saveName );
                buffer.Add( "<size=12>" );
                buffer.Add( "<pos=320>" );
                buffer.Add( this.save.lastModified.ToShortDateString() );
                buffer.Add( "  " );
                buffer.Add( this.save.lastModified.ToShortTimeString() );
                buffer.Add( "<pos=530>" );
                buffer.AddHoursAndMinutes( this.save.secondsSinceGameStart );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.save == null || this.save.saveName == null || iSaveGameName.Instance == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                iSaveGameName.Instance.SetText( this.save.saveName );
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SaveGameMenu-bSavegame-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.save == null || this.save.saveName == null )
                    return;

                this.save.WriteTooltip(tooltipBuffer);
                
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        public class bSort : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int sortType = GameSettings_AIW2.Current.SaveLoadSortType;
                Buffer.Add( "当前排序：" );
                switch ( sortType )
                {
                    case 1:
                        Buffer.Add( "按战役时间" );
                        break;
                    case 2:
                        Buffer.Add( "按日期" );
                        break;
                    default:
                        Buffer.Add( "按名称" );
                        break;
                }
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int sortType = GameSettings_AIW2.Current.SaveLoadSortType;
                
                sortType++;
                if ( sortType > 2 )
                    sortType = 0;
                
                GameSettings_AIW2.Current.SaveLoadSortType = sortType;
                GameSettings.SaveToDisk();
                
                SaveLoadMethods.PopulateSavesInGroup(Instance.CurrentCampaign);

                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class iSaveGameName : InputAbstractBase
        {
            //This is the box where someone types in the name for their new campaign
            public int maxSaveLen = 35;
            public static iSaveGameName Instance;
            public iSaveGameName() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxSaveLen )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' || addedChar == ' ' )
                    return addedChar;
                //Other things could be allowed later I suppose, but I went with
                //"simple" for now
                return '\0';
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        if ( bSave.SaveInstance != null )
                            bSave.SaveInstance.HandleClick_Subclass( new MouseHandlingInput() );
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        public class tCampaignName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( World.Instance.CampaignName );
            }
            public override void OnUpdate()
            {
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
        public class bDelete : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "删除" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                string saveName = iSaveGameName.Instance.GetText();
                if ( saveName == null || saveName.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要存档名称！", "请输入你要删除的游戏存档名称", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                
                if ( Window_SaveGameMenu.Instance.GetDoesAlreadyHaveSaveGameName( saveName ) )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( deleteSave, null, "删除游戏",
                        "你确定要删除此存档吗？", "删除存档", "返回" );
                    
                    return MouseHandlingResult.DoNotPlayClickSound;
                }
                
                return MouseHandlingResult.None;
            }

            private void deleteSave()
            {
                string saveName = iSaveGameName.Instance.GetText();
                string fullSaveName = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + World.Instance.CampaignName +
                    "/" + saveName + Engine_Universal.SaveMainExtension;

                World.Instance.DeleteSaveGame( fullSaveName, false, false );
                
                SaveLoadMethods.PopulateSavesInGroup(Instance.CurrentCampaign);
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }
        
        public class bSave : ButtonAbstractBase
        {
            public static bSave SaveInstance;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                SaveInstance = this;
                Buffer.Add( "保存" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "铁人模式", "无法手动保存；游戏会自动保存，并在你退出时保存。", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                string saveName = iSaveGameName.Instance.GetText();
                if ( saveName == null || saveName.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要存档名称！", "请输入你的存档名称", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( Window_SaveGameMenu.Instance.GetDoesAlreadyHaveSaveGameName( saveName ) )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( DoSave, null, "存档已存在！",
                        "此战役中已存在同名存档：" + saveName, "覆盖它", "返回" );
                    return MouseHandlingResult.DoNotPlayClickSound;
                }

                this.DoSave();

                return MouseHandlingResult.None;
            }

            private void DoSave()
            {
                string campaignName = World.Instance.CampaignName;
                if ( ArcenStrings.IsEmpty( campaignName ) )
                {
                    ArcenDebugging.ArcenDebugLog( "Could not create save, because the campaign name is empty!", Verbosity.ShowAsError );
                    return;
                }

                string saveName = iSaveGameName.Instance.GetText();
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SaveGame], GameCommandSource.AnythingElse );
                //generate the saveGame entry from the name and state of the game
                //DateTime dt = ArcenTime.Now;
                //Generate a SaveGameData from the saveGame and campaignName boxes,
                //along with game metadata
                command.RelatedString = saveName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                if ( (GameSettings.Current.LastSavegameFile != saveName || GameSettings.Current.LastSavegameCampaign != campaignName) )
                {
                    GameSettings.Current.LastSavegameFile = saveName;
                    GameSettings.Current.LastSavegameCampaign = campaignName;
                    GameSettings.SaveToDisk();
                }

                Instance.Close();
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bSaveAndQuit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "保存并退出" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( DoSave, null, "你确定吗？",
                        "你确定要保存并退出吗？注意铁人模式不允许你输入存档名称。", "保存并退出", "返回" );
                    return MouseHandlingResult.None;
                }
                string saveName = iSaveGameName.Instance.GetText();
                if ( saveName == null || saveName.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要存档名称！", "请输入你的存档名称", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( Window_SaveGameMenu.Instance.GetDoesAlreadyHaveSaveGameName( saveName ) )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( DoSave, null, "存档已存在！",
                        "此战役中已存在同名存档：" + saveName, "覆盖它", "返回" );
                    return MouseHandlingResult.DoNotPlayClickSound;
                }
                else
                {
                    ModalPopupData.CreateAndLogYesNoStyle( DoSave, null, "你确定吗？",
                        "你确定要保存并退出吗？", "保存并退出", "返回" );
                }
                return MouseHandlingResult.None;
            }

            private void DoSave()
            {
                if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                {
                    SaveLoadMethods.TakeIronmanSave( true, false );
                    return;
                }
                string saveName = iSaveGameName.Instance.GetText();
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SaveGame], GameCommandSource.AnythingElse );
                //generate the saveGame entry from the name and state of the game
                //DateTime dt = ArcenTime.Now;
                //Generate a SaveGameData from the saveGame and campaignName boxes,
                //along with game metadata
                command.RelatedString = saveName;
                command.RelatedBool = true; // tells it to quit after completing the save
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                if ( (GameSettings.Current.LastSavegameFile != saveName || GameSettings.Current.LastSavegameCampaign != World.Instance.CampaignName) )
                {
                    GameSettings.Current.LastSavegameFile = saveName;
                    GameSettings.Current.LastSavegameCampaign = World.Instance.CampaignName;
                    GameSettings.SaveToDisk();
                }

                Instance.Close();
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public override void Close()
        {
            base.Close();
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
