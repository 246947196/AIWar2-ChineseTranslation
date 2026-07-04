using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Linq;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_LoadGameMenu : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_LoadGameMenu Instance;

        public Window_LoadGameMenu()
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
        private static bool ForceMultiplayerHost = false;
        private CampaignOrQuickstartGroup Campaign;
        private SaveGameData saveSelected = null;
        private string LastCampaignName;
        //private DateTime LastCampaignName_AssignedAt = SaveLoadMethods.NullDate;
        //private string LastSaveName;
        //private DateTime LastSaveName_AssignedAt = SaveLoadMethods.NullDate;
        
        public void Open( bool ForceMultiplayerHst )
        {
            if ( this.IsOpen )
                return;
            
            IsOpen = true;
            ForceMultiplayerHost = ForceMultiplayerHst;
            
            SaveLoadMethods.PopulateCampaignFolders();
            
            if (!SaveLoadMethods.GetCampaign(LastCampaignName, false, out Campaign))
                Campaign = SaveLoadMethods.SortedCampaignNames.FirstOrDefault();
            
            if (Campaign != null)
                SaveLoadMethods.PopulateSavesInGroup(Campaign);

            LastCampaignName = Campaign?.DisplayName ?? string.Empty;
            saveSelected = Campaign?.SortedSavesInFolder.FirstOrDefault();
            
            ArcenNetworkAuthority.ChangeStatusToHostOrSinglePlayerIfNeeded( ForceMultiplayerHost );
        }

        public bool GetIsOpen()
        {
            return this.IsOpen;
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            
            this.IsOpen = false;
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly )
                    Buffer.Add( "加载单人游戏存档" );
                else
                    Buffer.Add( "加载多人游戏存档" );
            }
        }
        #endregion

        #region tQuestionMarkInfo
        public class tQuestionMarkInfo : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "[多人游戏问题？]" );
            }
            public override void HandleMouseover() 
            {
                string message = "其他玩家可以加入，前提是游戏中存在与其档案名匹配的档案。\n\n当您将存档加载到大厅（作为新游戏的起点）或加载存档后，其他玩家即可连接。在您仍在此菜单时，他们无法连接。";
                ArcenNetworkAuthority.GetAddedStringForMultiplayerInfo( ref message );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, message );
            }

            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bCampaign> btnCampaignPool;
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
                        if ( bCampaign.Original != null && bSavegame.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCampaignPool = new ButtonAbstractBase.ButtonPool<bCampaign>( bCampaign.Original, 20 );
                            btnSavegamePool = new ButtonAbstractBase.ButtonPool<bSavegame>( bSavegame.Original, 60 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCampaigns();
                this.OnUpdateSavegames();
            }

            public void OnUpdateCampaigns()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                ArcenNetworkAuthority.ChangeStatusToHostOrSinglePlayerIfNeeded( ForceMultiplayerHost );

                btnCampaignPool.Clear( 40 );

                for ( int i = 0; i < SaveLoadMethods.SortedCampaignNames.Count; i++ )
                {
                    CampaignOrQuickstartGroup campaign = SaveLoadMethods.SortedCampaignNames[i];
                    bCampaign item = btnCampaignPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( i, campaign );
                }
                
                btnCampaignPool.ApplyItemsInRows( 5, ref currentY, 36, 278, 30 );

                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCampaign.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
            }

            public void OnUpdateSavegames()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnSavegamePool.Clear( 40 );

                if (Instance.Campaign != null)
                {
                    for ( int i = 0; i < Instance.Campaign.SortedSavesInFolder.Count; i++ )
                    {
                        SaveGameData save = Instance.Campaign.SortedSavesInFolder[i];
                        //if ( save.campaignName != Instance.cachedCurrentCampaignName?.CampaignName )
                        //{
                        //    //we have ourselves a mixup!
                        //    Instance.PopulateListOfSavegamesInCampaign();
                        //    return;
                        //}
                        
                        bSavegame item = btnSavegamePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        
                        item.Assign( save );
                    }
                }
                
                btnSavegamePool.ApplyItemsInRows( 5, ref currentY, 26, 673.3f, 24 );

                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bSavegame.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
            }
        }

        #region bCampaign
        public class bCampaign : ButtonAbstractBase
        {
            public static bCampaign Original;
            public bCampaign() { if ( Original == null ) Original = this; }

            private CampaignOrQuickstartGroup Campaign = null;
            private int CampaignIndex = 0;

            public void Assign( int CampaignIndex, CampaignOrQuickstartGroup CampaignName )
            {
                this.CampaignIndex = CampaignIndex;
                this.Campaign = CampaignName;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Campaign == null;
            }

            public override void Clear()
            {
                this.Campaign = null;
                this.CampaignIndex = -1;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Campaign == null )
                    return;

                if ( this.Campaign == Instance.Campaign )
                    buffer.Add( "<color=#fff36f>" );
                
                buffer.Add( this.Campaign.DisplayName );
                
                if ( this.Campaign == Instance.Campaign )
                    buffer.Add( "</color>" );
                
                //if ( this.Campaign.Type == CampaignOrQuickstartGroup.Types.IronmanSave )
                    //buffer.Add( "<size=50%><color=#ff3050>  Ironman</color></size>");
                
                //this.CampaignName.AppendSecondLineWithExpansionAndOrModThisIsFrom( buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Campaign == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                if ( this.Campaign != Instance.Campaign )
                {
                    Instance.Campaign = this.Campaign;
                    Instance.LastCampaignName = this.Campaign.DisplayName;
                    //Instance.LastCampaignName_AssignedAt = DateTime.Now;
                    
                    Instance.saveSelected = null;
                    
                    SaveLoadMethods.PopulateSavesInGroup(Instance.Campaign);
                }
                
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                //Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "Choose a campaign from which to load saves." );
            }
        }
        #endregion
        #region bSavegame
        /// <summary>
        /// NOTE: This is in fact the LOAD game button in the LOAD game menu.
        /// </summary>
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

                if ( Instance.saveSelected == this.save )
                    buffer.StartColor( ColorMath.LightYellow );

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

            private static List<string> metaDataList = List<string>.Create_WillNeverBeGCed( 12, "Window_LoadGameMenu-bSavegame-metaDataList" );
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.save == null )
                {
                    LOG.Err("Error: the current save is null.");
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                string saveName = this.save.saveName;
                if ( string.IsNullOrEmpty(saveName) )
                {
                    LOG.Err("Error: the current save name is empty.");
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                
                string campaignName = Instance.Campaign?.DisplayName;
                if ( string.IsNullOrEmpty(campaignName) )
                {
                    LOG.Err("Error: the current campaign name is empty.");
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                
                string saveFullFilename = this.save.saveFullFilename;
                if ( string.IsNullOrEmpty(saveFullFilename) )
                {
                    LOG.Err("Error: the current save full-file-path is empty.");
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                
                Instance.saveSelected = this.save;
                
                //Instance.LastSaveName = this.save?.saveName;
                //Instance.LastSaveName_AssignedAt = DateTime.Now;
                
                if ( input.LeftButtonDoubleClicked )
                {
                    SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                    
                    Action<bool> PostAction =
                        (success)=>
                        {
                            if (!success)
                                return;
                            
                            World.Instance.CampaignName = campaignName;
                            this.save.campaignName = campaignName;
                            this.save.numTimesLoaded++;
                            
                            Instance.Close();

                            this.save.SaveMetaData();
                            
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Savegame path: '" + this.SaveGame.saveName + "' '" + this.SaveGame.campaignName + "'", Verbosity.Chat );
                            if ( (GameSettings.Current.LastSavegameFile != saveName || 
                                  GameSettings.Current.LastSavegameCampaign != campaignName) )
                            {
                                GameSettings.Current.LastSavegameFile = saveName;
                                GameSettings.Current.LastSavegameCampaign = campaignName;
                                GameSettings.SaveToDisk();
                            }
                        };
                    
                    Engine_AIW2.LoadGameNoCampaignNameSet( saveFullFilename, StartWorldSource1.AnythingElse, StartWorldSource2.LoadingSaveGame, false, PostAction);
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LoadGameMenu-bSavegame-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.save == null || this.save.saveName == null )
                    return;

                this.save.WriteTooltip(tooltipBuffer);

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion
        public class bDeleteGame : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile(ArcenDoubleCharacterBuffer buffer)
            {
                buffer.Add("删除");
            }
            private void deleteSave()
            {
                World.Instance.DeleteSaveGame( Instance.saveSelected.saveFullFilename, false, false );
                Instance.saveSelected = null;
                SaveLoadMethods.PopulateSavesInGroup(Instance.Campaign);
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance.saveSelected != null )
                    ModalPopupData.CreateAndLogYesNoStyle( deleteSave, null, "删除存档",
                        "确定要删除存档'" + Instance.saveSelected.saveName + "'吗？", "删除存档", "保留存档" );
                else
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要选择存档！", "当前未选择存档。请选择一个存档后再试。", "返回" );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
        }
        public class bDeleteCampaign : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile(ArcenDoubleCharacterBuffer buffer)
            {
                buffer.Add("删除战役");
            }
            private void deleteCampaign()
            {
                World.Instance.DeleteCampaign(Instance.Campaign.Directories.First().Directory);
                SaveLoadMethods.PopulateCampaignFolders();
                Instance.Campaign = SaveLoadMethods.SortedCampaignNames.FirstOrDefault();
                if (Instance.Campaign != null)
                    SaveLoadMethods.PopulateSavesInGroup(Instance.Campaign);
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                var name = Instance.Campaign?.DisplayName;
                if (string.IsNullOrEmpty(name))
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要选择战役！", "当前未选择战役。请选择一个战役后再试。", "返回" );
                    return MouseHandlingResult.None;
                }
                
                ModalPopupData.CreateAndLogYesNoStyle(deleteCampaign, null, "<color=#ff0000>删除战役</color>", 
                    "确定要删除此战役" + name + "及其所有关联存档吗？\n\n<color=#ff0000>此操作无法撤销。</color>", "删除战役", "保留战役");

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
        }
        public class bSort : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int sortType = GameSettings_AIW2.Current.SaveLoadSortType;
                Buffer.Add( "排序：" );
                switch ( sortType )
                {
                    case 1:
                        Buffer.Add( "战役时间" );
                        break;
                    case 2:
                        Buffer.Add( "日期" );
                        break;
                    default:
                        Buffer.Add( "名称" );
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
                
                SaveLoadMethods.PopulateSavesInGroup(Instance.Campaign);

                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

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

        #region Edit Game In Lobby
        public class bStartLobby : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "大厅" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance.saveSelected == null || 
                     Instance.saveSelected.saveName == null || 
                     Instance.Campaign == null ||
                     string.IsNullOrEmpty(Instance.Campaign.DisplayName) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要选择存档！", "未选择存档！请选择一个存档后再试。", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );

                Action<bool> PostAction =
                    (success)=>
                    {
                        if (success)
                        {
                            SaveLoadMethods.RestartCampaignData( true, Instance.Campaign.DisplayName, false );
                            Instance.Close();
                        }
                    };
                
                Engine_AIW2.LoadGameNoCampaignNameSet( Instance.saveSelected.saveFullFilename, StartWorldSource1.StartingTheLobbyFromPrior, StartWorldSource2.LoadingSaveGame, false, PostAction );
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( Instance.saveSelected == null || 
                     Instance.saveSelected.saveName == null || 
                     Instance.saveSelected.campaignName == null )
                {
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "打开大厅并编辑游戏" );
                }
                else
                {
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "打开大厅并编辑游戏：" + Instance.saveSelected.saveName );
                }
            }
            public override void OnUpdate() { }
        }
        #endregion

        public class bStartGame : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "开始" );
            }
            private static List<string> metaDataList = List<string>.Create_WillNeverBeGCed( 12, "Window_LoadGameMenu-bStartGame-metaDataList" );
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance.saveSelected == null || 
                     Instance.saveSelected.saveName == null || 
                     Instance.Campaign == null ||
                     string.IsNullOrEmpty(Instance.Campaign.DisplayName) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要选择存档！", "未选择存档！请选择一个存档后再试。", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                
                var campaign_name = Instance.Campaign.DisplayName;
                
                Action<bool> PostAction = 
                    (success)=>
                    {
                        if (!success)
                            return;
                        
                        World.Instance.CampaignName = campaign_name;
                        
                        Instance.saveSelected.numTimesLoaded++;
                        Instance.saveSelected.campaignName = campaign_name;
                        Instance.saveSelected.SaveMetaData();
                        
                        if ( (GameSettings.Current.LastSavegameFile != Instance.saveSelected.saveName || 
                              GameSettings.Current.LastSavegameCampaign != Instance.saveSelected.campaignName) )
                        {
                            GameSettings.Current.LastSavegameFile = Instance.saveSelected.saveName;
                            GameSettings.Current.LastSavegameCampaign = Instance.saveSelected.campaignName;
                            GameSettings.SaveToDisk();
                        }
                        
                        Instance.Close();
                    };
            
                Engine_AIW2.LoadGameNoCampaignNameSet( Instance.saveSelected.saveFullFilename, StartWorldSource1.AnythingElse, StartWorldSource2.LoadingSaveGame, false, PostAction );

                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                if ( Instance.saveSelected == null || Instance.saveSelected.saveName == null || Instance.saveSelected.campaignName == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "加载游戏" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "加载游戏：" + Instance.saveSelected.saveName + "（从上次存档）" );
            }
            
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
