using System;
using System.Linq;
using System.IO;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class Window_LoadQuickStartMenu : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_LoadQuickStartMenu Instance;
        internal static customParent CustomParent;
        
        private bool IsOpen;
        
        private CampaignOrQuickstartGroup _selectedGroup;
        //private CampaignData _selectedGroupExData;
        
        private SaveGameData _selectedSave;
        //private CampaignData _selectedSaveExData;
        
        private Log _log;
        private Log GetLogger()
        {
            if ( SaveLoadMethods.Debug )
            {
                if ( _log == null ) _log = Log.Yes;
                return _log;
            }
            
            return null;
        }
        
        private CampaignOrQuickstartGroup CurrentGroup
        {
            get
            {
                return _selectedGroup;
            }
            set
            {
                _selectedGroup = value;
                //_selectedGroupExData = PlayerProfile.Local?.GetCampaignDataForCampaign( _selectedGroup?.DisplayName );
            }
        }
        
        private SaveGameData saveSelected
        {
            get
            {
                return _selectedSave;
            }
            set
            {
                _selectedSave = value;
                //_selectedSaveExData = PlayerProfile.Local?.GetCampaignDataForMission( _selectedSave?.campaignName, _selectedSave?.saveName );
            }
        }
        
        public Window_LoadQuickStartMenu()
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

        public bool GetIsOpen()
        {
            return this.IsOpen;
        }
        
        public void Open()
        {
            if ( this.IsOpen )
                return;
            
            this.IsOpen = true;
            
            SaveLoadMethods.PopulateQuickstartGroups();
            
            this.CurrentGroup = SaveLoadMethods.SortedQuickstartGroups.FirstOrDefault();
            
            if ( this.CurrentGroup != null )
            {
                if (SaveLoadMethods.Debug) LOG.Msg("CurrentGroup=\n{0}", this.CurrentGroup==null ? "null" : ObjToStr.Format(this.CurrentGroup, ObjToStr.Style.TypeAndMembersMultiLine));
                SaveLoadMethods.PopulateSavesInGroup( this.CurrentGroup );
            }

            Instance.saveSelected = null;
            iNewCampaignName.Instance.SetText( string.Empty );
            
            CustomParent.OnUpdate();
        }

        public void Close()
        {
            //if (SaveLoadMethods.Debug) LOG.Msg("{0}.Close() called from:\n{1}", this.GetType().Name, LOG.StackTrace());
            
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            this.CurrentGroup = null;
            SaveLoadMethods.ResaveNextCount = 0;
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly )
                    Buffer.Add( "快速开始单人新战役" );
                else
                    Buffer.Add( "快速开始多人新战役" );
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
                string message = "选择预制场景，而不是手动进行战役设置。\n\n警告：大多数快速开始默认只有一个玩家槽位。要进行多人游戏，只需在你的选择上点击'在大厅中编辑'，让其他玩家在你开始前连接。\n\n其他玩家可以在你将快速开始载入大厅或完全载入游戏后连接。在你还在这个菜单时，他们无法连接。";
                ArcenNetworkAuthority.GetAddedStringForMultiplayerInfo( ref message );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, message ); 
            }

            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bFolder> btnFolderPool;
        private static ButtonAbstractBase.ButtonPool<bQuickStart> btnQuickStartPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            
            public customParent()
            {
                CustomParent = this;
            }
            
            public override void OnUpdate()
            {
                if ( Window_LoadQuickStartMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bFolder.Original != null && bQuickStart.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnFolderPool = new ButtonAbstractBase.ButtonPool<bFolder>( bFolder.Original, 20 );
                            btnQuickStartPool = new ButtonAbstractBase.ButtonPool<bQuickStart>( bQuickStart.Original, 60 );
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

                btnFolderPool.Clear( 40 );

                for ( int i = 0; i < SaveLoadMethods.SortedQuickstartGroups.Count; i++ )
                {
                    CampaignOrQuickstartGroup campaign = SaveLoadMethods.SortedQuickstartGroups[i];
                    bFolder item = btnFolderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    
                    //LOG.Msg("#{0}: {1}", i+1, campaign.DisplayName);
                    
                    item.Assign( campaign );
                }
                
                btnFolderPool.ApplyItemsInRows( 5, ref currentY, 34, 278, 30 );

                RectTransform rTran = (RectTransform)bFolder.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
            }

            public void OnUpdateSavegames()
            {
                float currentY = -10; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnQuickStartPool.Clear( 40 );

                if (Instance.CurrentGroup != null)
                {
                    for ( int i = 0; i < Instance.CurrentGroup.SortedSavesInFolder.Count; i++ )
                    {
                        SaveGameData save = Instance.CurrentGroup.SortedSavesInFolder[i];
                        bQuickStart item = btnQuickStartPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( save );
                    }
                }
                
                btnQuickStartPool.ApplyItemsInRows( 5, ref currentY, 31, 673.3f, 30 );

                RectTransform rTran = (RectTransform)bQuickStart.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
            }
        }

        #region bFolder
        public class bFolder : ButtonAbstractBase
        {
            public static bFolder Original;
            public bFolder() { if ( Original == null ) Original = this; }

            private CampaignOrQuickstartGroup Campaign = null;

            public void Assign( CampaignOrQuickstartGroup CampaignName )
            {
                this.Campaign = CampaignName;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Campaign == null;
            }

            public override void Clear()
            {
                this.Campaign = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Campaign == null )
                    return;
                
                var args = FormatArgs.Alloc(buffer, AppendVar, GetStyle, EvalCond/*, logger:Instance.GetLogger()*/);
                TextVarMap.Quickstart_Category_Format.AddVarReplace(args);
                
                args.Logger?.AppendFormat("Final Text:\n{0}\n", buffer.ToString());
                args.Logger?.Flush();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Campaign == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                if ( input.RightButtonClicked )
                {
                    PlayerProfile localPlayerOrNull = PlayerProfile.Local;
                    if ( localPlayerOrNull != null )
                    {
                        CampaignData data = localPlayerOrNull.GetCampaignDataForCampaign( this.Campaign.DisplayName );
                        data.CampaignDefeated = !data.CampaignDefeated;
                        localPlayerOrNull.SaveToDisk();
                    }
                    return MouseHandlingResult.None;
                }
                
                if (this.Campaign != Instance.CurrentGroup || SaveLoadMethods.ResaveAllSaveMeta)
                {
                    Instance.saveSelected = null;
                    Instance.CurrentGroup = this.Campaign;

                    SaveLoadMethods.PopulateSavesInGroup(Instance.CurrentGroup);
                    
                    CustomParent.OnUpdateSavegames();
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LoadQuickStartMenu-bFolder-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.Campaign == null )
                    return;
                
                var buffer = tooltipBuffer;
                
                buffer
                    .StartColor( ColorMath.LightYellow )
                    .Add( this.Campaign.DisplayName )
                    .EndColor()
                    .NewLine();

                if (this.Campaign.FromExpansion != null || this.Campaign.FromMod != null)
                {
                   buffer.Add("<pos=4><size=60%>from ");

                    if (this.Campaign.FromExpansion != null)
                    {
                        buffer
                            .Add( "<size=50%>")
                            .Add( this.Campaign.FromExpansion.Abbreviation, this.Campaign.FromExpansion.ColorForDisplay )
                            .Add( "</size>" )
                            .Add( " " )
                            ;
                    }
                    
                    if (this.Campaign.FromMod != null)
                    {
                        buffer
                            .Add( "<size=50%>")
                            .Add( this.Campaign.FromMod.Abbreviation, this.Campaign.FromMod.ColorForDisplay )
                            .Add( "</size>" )
                            .Add( " " )
                            ;
                    }
                       
                    buffer.Add("</size>");
                    buffer.Add("\n");
                }

                if (!string.IsNullOrWhiteSpace(this.Campaign.Tooltip))
                {
                    buffer.Pad(TextStyle.Pad_Large).NewLine()
                          .Add( this.Campaign.Tooltip ?? " ", "eeeeee" )
                          .Pad(TextStyle.Pad_Large).NewLine();
                }
                
                TextVarMap.Get("Quickstart_Tooltip_Footer").AddVarReplace(FormatArgs.Alloc(buffer));

                int width = ExternalConstants.Instance.GetCustomInt32_Slow("quickstart_tooltip_width");
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, buffer.GetStringAndResetForNextUpdate(), width);
            }
            
            private TextStyle GetStyle(string name, TextStyle style, object args)
            {
                if (name == "NameStyle")
                {
                    if (Instance._selectedGroup == this.Campaign)
                        return TextStyle.Color_Quickstart_Selected;
                    
                    var data = PlayerProfile.Local?.GetCampaignDataForCampaign( this.Campaign.DisplayName );
                    if (data?.CampaignDefeated == true)
                        return TextStyle.Color_Quickstart_Cleared;
                    
                    return TextStyle.Color_Quickstart;
                }
                
                return style;
            }
            
            private void AppendVar(string name, TextStyle style, ArcenCharacterBufferBase buffer, object args)
            {
                if (name == "Name")
                {
                    buffer.Add(this.Campaign.DisplayName??"???");
                    return;
                }

                if (name == "From")
                {
                    buffer.AddDlcMod(this.Campaign);
                    return;
                }
                
                buffer.Add(name);
            }

            private bool EvalCond(string cond, object args)
            {
                if (cond == "IsCleared")
                {
                    var dataOrNull = PlayerProfile.Local?.GetCampaignDataForCampaign( this.Campaign.DisplayName );
                    return (dataOrNull?.CampaignDefeated == true);
                }
                
                return false;
            }
        }
        #endregion

        #region bQuickStart
        public class bQuickStart : ButtonAbstractBase
        {
            public static bQuickStart Original;
            public bQuickStart() { if ( Original == null ) Original = this; }

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

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LoadQuickStartMenu-bQuickStart-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugstage = 0;
                try
                {
                    var buffer = tooltipBuffer;
                    
                    debugstage = 100;
                    
                    if (this.save == null)
                        return;
                    
                    debugstage = 200;
                    
                    buffer
                        //.Add( "<size=60%>QS:</size> " )
                        .StartColor( ColorMath.LightYellow )
                        .Add( this.save.saveName )
                        .EndColor()
                        .NewLine();

                    debugstage = 300;
                    
                    if (!string.IsNullOrEmpty(this.save.author))
                        buffer.Add("<pos=4><size=80%>by " ).Add( this.save.author ).Add( "\n</size>" );

                    debugstage = 400;
                    
                    if (this.save.DlcInUse.Count > 0 || this.save.ModInUse.Count > 0)
                    {
                        buffer.Add("<pos=4><size=60%>uses ");

                       debugstage = 500;
                        if ( this.save.DlcInUse.Count > 0)
                        {
                            for (int i = 0; i < this.save.DlcInUse.Count; i++)
                            {
                                var other = this.save.DlcInUse[i];
                         
                                debugstage = 600;
                                
                                buffer
                                    .Add( "<size=50%>")
                                    .Add( other.Abbreviation, other.ColorForDisplay )
                                    .Add( "</size>" )
                                    .Add( " " )
                                    ;
                            }
                            
                            debugstage = 700;
                        }
                        
                        debugstage = 800;
                        if (this.save.ModInUse.Count > 0)
                        {
                            var numShown = Math.Min(this.save.ModInUse.Count, 3);
                            var numAdditional = Math.Max(this.save.ModInUse.Count-numShown, 0);
                            
                            debugstage = 810;
                            for (int i = 0; i < numShown; i++)
                            {
                                var other = this.save.ModInUse[i];
                             
                                debugstage = 820;
                                buffer.Add( "<size=50%>");
                                if (other == null)
                                    buffer.Add("NULL");
                                else
                                    buffer.Add( other.Abbreviation, other.ColorForDisplay );

                                buffer.Add( "</size>" ).Add( " " );
                                
                                debugstage = 830;
                            }
                            
                            debugstage = 840;
                            if (numAdditional > 0)
                            {
                                buffer.Add("<size=50%>(+").Add(numAdditional).Add(")</size>");
                            }
                            
                            debugstage = 900;
                        }

                        buffer.Add("</size>");
                        buffer.Add("\n");
                    }
                    
                    if (this.save.isscenario)
                    {
                        debugstage = 1000;
                        TextVarMap.Get("Scenario_Tooltip_Header")?.AddVarReplace(FormatArgs.Alloc(buffer));
                    }
                    
                    if (!string.IsNullOrWhiteSpace(save.TooltipData))
                    {
                        debugstage = 1100;
                        buffer
                            .Add( "\n" )
                            .Add( save.TooltipData ?? " ", "eeeeee" )
                            .Add( "\n" )
                            ;
                    }
                    else if (this.save.metaExists)
                    {
                        debugstage = 1200;
                        buffer
                            //.Add( "\n" )
                            .WriteTooltip( save )
                            .Add( "\n" )
                            ;
                    }
                    
                    //if ( SaveLoadMethods.Debug && InputCaching.CalculateNumberOfTooltipDetailKeysHeld() > 0 )
                    //{
                    //    buffer
                    //        .Add( "DEBUG:\n" )
                    //        .WriteTooltip( save )
                    //        .Add( "\n" )
                    //        ;
                    //}
                    //else
                    //{
                    //    buffer
                    //        .Add( "\n" )
                    //        .Add( save.TooltipData ?? " ", "eeeeee" )
                    //        .Add( "\n" )
                    //        ;
                    //}

                    debugstage = 1300;
                    TextVarMap.Get("Quickstart_Tooltip_Footer").AddVarReplace(FormatArgs.Alloc(buffer));

                    debugstage = 1400;
                    int width = ExternalConstants.Instance.GetCustomInt32_Slow("quickstart_tooltip_width");
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, buffer.GetStringAndResetForNextUpdate(), width );
                      
                    debugstage = 1500;
                    /*
                    string hasmore = null;
                    if (mod.GetShortDescription() != mod.GetDescription())
                        hasmore = "IN FULL";

                    EntityText.Write_Tooltip_Hotkeys_Footer(buffer, false, false, hasmore);
                    */
                }
                catch (Exception e)
                {
                    LOG.Err("Exception happened in {0}() at debugstage {1}:\n{2}\n", this.TypeNameAndMethod(), debugstage, e);
                }
            }
            
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.save == null || this.save.saveName == null )
                    return;

                var args = FormatArgs.Alloc(buffer, AppendVar, GetStyle, EvalCond/*, logger:Instance.GetLogger()*/);
                TextVarMap.Quickstart_Format.AddVarReplace(args);
                
                args.Logger?.AppendFormat("Final Text:\n{0}\n", buffer.ToString());
                args.Logger?.Flush();
            }
            
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.save == null || 
                     string.IsNullOrEmpty(this.save.saveName) || 
                     Instance.CurrentGroup == null )
                {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                
                if ( input.RightButtonClicked )
                {
                    var data = PlayerProfile.Local?.GetCampaignDataForMission( this.save.campaignName, this.save.saveName );
                    
                    if (data != null)
                    {
                        data.MissionDefeated = !data.MissionDefeated;
                        PlayerProfile.Local.SaveToDisk();
                    }

                    return MouseHandlingResult.None;
                }
                
                Instance.saveSelected = this.save;
                var name_field = iNewCampaignName.Instance;
                var name_input = (ArcenUI_Input)name_field.Element;
                
                string str = "";
                for (int i = 0; i < this.save.saveName.Length; i++)
                { 
                    var c = this.save.saveName[i];
                    c = name_field.ValidateInput(str, i, c);
                    if (c != '\0')
                        str += c;
                }

                name_input.SetText(str);
                name_input.Focus();
                name_input.ReferenceInputField.MoveTextStart(false);
                name_input.ReferenceInputField.MoveTextEnd(true);
                name_input.ReferenceInputField.textComponent.overflowMode = TMPro.TextOverflowModes.Masking;
                
                // if you double click the save, load it
                if ( input.LeftButtonDoubleClicked ) 
                {
                    string campaignName = iNewCampaignName.Instance.GetText();
                    if ( string.IsNullOrEmpty(campaignName) )
                    {
                        ModalPopupData.CreateAndLogOKStyle( 
                            PopupSizeStyle.Normal, null, 
                            "需要战役名称！", 
                            "请为你的战役选择一个名称。这可以让你的存档更加有序。", "返回" );
                        
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }

                    SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                    
                    Action<bool> PostAction = 
                        (success)=>
                        {
                            if (!success)
                                return;
                            
                            World.Instance.CampaignName = campaignName;
                    
                            World_AIW2.Instance.IsFromQuickLoad = true;
                            World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;
                            
                            Mapgen.GenerateMap( null, 
                                ()=>
                                {
                                    World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall = false;
                                    World.Instance.IsPaused = !ArcenNetworkAuthority.IsClient && !GameSettings.Current.GetBoolBySetting("StartPaused");
                                    World.Instance.CampaignName = campaignName;
                                });
                            
                            Instance.Close();
                        };
                    
                    Engine_AIW2.LoadGameNoCampaignNameSet( Instance.saveSelected.saveFullFilename, StartWorldSource1.AnythingElse, StartWorldSource2.LoadingQuickStart, false, PostAction);
                }
                
                return MouseHandlingResult.None;
            }
            
            [Flags]
            private enum QuickstartStyle : int
            {
                None        = 0,
                Selected    = 1 << 0,
                Cleared     = 1 << 1,
            }
            
            private QuickstartStyle GetStyle()
            {
                var res = QuickstartStyle.None;
                
                if (Instance.saveSelected == this.save)
                    res |= QuickstartStyle.Selected;

                var data = PlayerProfile.Local?.GetCampaignDataForMission( this.save.campaignName, this.save.saveName );
                if (data?.MissionDefeated == true)
                    res |= QuickstartStyle.Cleared;

                return res;
            }
            
            private TextStyle GetNameStyle()
            {
                if (Instance.saveSelected == this.save)
                    return TextStyle.Color_Quickstart_Selected;
                
                var data = PlayerProfile.Local?.GetCampaignDataForMission( this.save.campaignName, this.save.saveName );
                if (data != null)
                {
                    if (data.MissionDefeated == true)
                        return TextStyle.Color_Quickstart_Cleared;
                }

                return TextStyle.Color_Quickstart;
            }
            
            private QuickstartStyle Style_Flags 
            {
                get
                {
                    var res = QuickstartStyle.None;
                    
                    if (Instance.saveSelected == this.save)
                        res |= QuickstartStyle.Selected;

                    var data = PlayerProfile.Local?.GetCampaignDataForMission( this.save.campaignName, this.save.saveName );
                    if (data?.MissionDefeated == true)
                        res |= QuickstartStyle.Cleared;

                    return res;
                }
            }
                
            private TextStyle GetStyle(string name, TextStyle style, object args)
            {
                var flags = this.Style_Flags;

                if (name == "NameStyle")
                {
                    if (flags.HasFlag(QuickstartStyle.Selected))
                        return TextStyle.Color_Quickstart_Selected;
                    
                    if (flags.HasFlag(QuickstartStyle.Cleared))
                        return TextStyle.Color_Quickstart_Cleared;
                    
                    return TextStyle.Color_Quickstart;
                }
                
                return style;
            }
            
            private void AppendVar(string name, TextStyle style, ArcenCharacterBufferBase buffer, object args)
            {
                if (name == "Name")
                {
                    buffer.Add(this.save.saveName??"???");
                    return;
                }
                
                if (name == "Order")
                {
                    buffer.Add(this.save.sortOrder);
                    return;
                }
                
                if (name == "Difficulty")
                {
                    buffer.Add(this.save.difficulty??"???");
                    return;
                }
                
                if (name == "Author")
                {
                    buffer.Add(this.save.author??"???");
                    return;
                }
                
                if (name == "ModsInUse")
                {
                    var modsShown = this.save.ModInUse.AsEnumerable();
                    var numMods = this.save.ModInUse.Count;
                    var numAdditional = Math.Max(numMods - 3, 0);
                    if (numMods > 3)
                        modsShown = modsShown.Take(3);

                    buffer.AddDlcMod(null, modsShown, TextStyle.Empty);
                    
                    if (numAdditional > 0)
                        buffer.Add("+").Add(numAdditional).Add("");

                    return;
                }
                
                if (name == "DlcInUse")
                {
                    buffer.AddDlcMod(this.save.DlcInUse, null, TextStyle.Empty);
                    return;
                }
                
                if (name == "PlayerType")
                {
                    buffer.Add(this.save.playerType);
                    return;
                }
                
                if (name == "Community")
                {
                    buffer.Add(this.save.community);
                    return;
                }
                
                if (name == "OtherRemarks")
                {
                    int counter = 0;
                    
                    if (this.save.ironman)
                    {
                        if (counter > 0)
                            buffer.Add(" | ", TextStyle.Color_Gray);
                        buffer.Add("ironman", TextStyle.Get("OptIronman"));
                        counter++;
                    }
                    
                    //if (this.save.isscenario)
                    //{
                    //    if (counter > 0)
                    //        buffer.Add(" | ", TextStyle.Color_Gray);
                    //    buffer.Add("scenario", TextStyle.Get("OptScenario"));
                    //    counter++;
                    //}
                    
                    return;
                }

                buffer.Add(name);
            }

            private bool EvalCond(string cond, object args)
            {
                if (cond == "IsIronman")
                {
                    if (this.save.saveFullFilename.Contains( SaveLoadMethods.IronmanPrefix ))
                        return true;
                    
                    if (this.save.ironman)
                        return true;
                    
                    return false;
                }
                
                if (cond == "HasDifficulty")
                {
                    if (Instance?.CurrentGroup.DisplayName == "Basic" )
                        return false;
                    if (Instance?.CurrentGroup.DisplayName == "Moderate" )
                        return false;
                    if (Instance?.CurrentGroup.DisplayName == "Harder" )
                        return false;
                    if (Instance?.CurrentGroup.DisplayName == "Necromancer Intro" )
                        return false;
                    if (Instance?.CurrentGroup.DisplayName == "Expansions Intro" )
                        return false;
                    return !string.IsNullOrEmpty(this.save.difficulty) && this.save.difficulty != "nullDiff";
                }
                
                if (cond == "HasAuthor")
                {
                    if (!this.save.community)
                        return false;
                    
                    return !string.IsNullOrEmpty(this.save.author);
                }
                
                if (cond == "HasReq")
                {
                    return this.save.DlcInUse.Count > 0 || this.save.ModInUse.Count > 0;
                }
                
                if (cond == "HasDlc")
                {
                    return this.save.DlcInUse.Count > 0;
                }
                
                if (cond == "HasMod")
                {
                    return this.save.ModInUse.Count > 0;
                }
                
                if (cond == "HasPlayerType")
                {
                    if (this.save.playerType?.Contains("Human Empire") == true)
                        return false;
                    
                    return !string.IsNullOrEmpty(this.save.playerType);
                }
                
                if (cond == "IsCleared")
                {
                    var dataOrNull = PlayerProfile.Local?.GetCampaignDataForMission( this.save.campaignName, this.save.saveName );
                    return (dataOrNull?.MissionDefeated == true);
                }
                
                if (cond == "IsCommunity")
                {
                    var sfolder = Instance.CurrentGroup.GetSubfolderContainingSave(this.save.saveFullFilename);
                    if (sfolder?.IsCommunity == true)
                        return false;
                    
                    return this.save.community;
                }
                
                if (cond == "IsScenario")
                {
                    return this.save.isscenario;
                }
                
                return false;
            }
            
        }
        #endregion

        public class iNewCampaignName : InputAbstractBase
        {
            //This is the box where someone types in the name for their new campaign
            public int maxSaveLen = 35;
            public static iNewCampaignName Instance;
            public iNewCampaignName() { Instance = this; }

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
                        if ( bStartGame.StartInstance != null )
                            bStartGame.StartInstance.HandleClick_Subclass( new MouseHandlingInput() );
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
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
        public class bEditSetup : ButtonAbstractBase
        {
            public static bEditSetup Original;
            public bEditSetup() { if ( Original == null ) Original = this; }

            private SaveGameData save = null;

            public void Assign( SaveGameData save )
            {
                this.save = save;
            }
            public override void Clear()
            {
                this.save = null;
            }
            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "编辑设置" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return Instance.HandleRequest(UserRequest.Edit);
            }

            public override void HandleMouseover()
            {
                    if (Instance.saveSelected == null || Instance.saveSelected.saveName == null || Instance.saveSelected.campaignName == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "打开大厅并编辑游戏");
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "打开大厅并编辑游戏：" + Instance.saveSelected.saveName );
            }
            public override void OnUpdate() { }
        }
        #endregion

        #region dCampaignType
        public class dCampaignType : DropdownAbstractBase
        {
            public static dCampaignType Instance;
            public dCampaignType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                CampaignTypeData ItemAsType = (CampaignTypeData)Item.GetItem();
                //don't set this again if it was already the same
                if ( Engine_AIW2.LastQuickStartCampaignType == ItemAsType )
                    return;

                Engine_AIW2.LastQuickStartCampaignType = ItemAsType;
            }

            public override void OnUpdate()
            {
                if ( CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count <= 0 )
                    return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                if ( Engine_AIW2.LastQuickStartCampaignType == null )
                    Engine_AIW2.LastQuickStartCampaignType = CampaignTypeDataTable.EasiestNonSandboxType;
                CampaignTypeData typeDataToSelect = Engine_AIW2.LastQuickStartCampaignType;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (CampaignTypeData)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count; i++ )
                    {
                        CampaignTypeData row = CampaignTypeDataTable.Instance.AvailableCampaignTypes[i];
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        CampaignTypeData optionItemAsType = (CampaignTypeData)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count; i++ )
                    {
                        CampaignTypeData row = CampaignTypeDataTable.Instance.AvailableCampaignTypes[i];
                        DropdownOptionCampaignTypeData option = new DropdownOptionCampaignTypeData( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "战役类型决定了游戏的通用规则集。";
                if ( Engine_AIW2.LastQuickStartCampaignType == null )
                    Engine_AIW2.LastQuickStartCampaignType = CampaignTypeDataTable.EasiestNonSandboxType;
                CampaignTypeData typeDataToSelect = Engine_AIW2.LastQuickStartCampaignType;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Description;
                }
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                CampaignTypeData ItemAsType = (CampaignTypeData)Item.GetItem();
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "战役类型决定了游戏的通用规则集。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>:\n" + ItemAsType.Description );
            }
        }
        #endregion

        public class bStartGame : ButtonAbstractBase
        {
            public static bStartGame StartInstance;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                StartInstance = this;
                buffer.Add( "开始游戏" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return Instance.HandleRequest(UserRequest.Start);
            }

            public override void HandleMouseover()
            {
                if ( Instance.saveSelected == null || Instance.saveSelected.saveName == null || Instance.saveSelected.campaignName == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "开始游戏" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "开始游戏：" + Instance.saveSelected.saveName );
            }
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
        
        public enum UserRequest
        {
            Start,
            Edit
        }
        
        private MouseHandlingResult HandleRequest(UserRequest action)
        {
            if (string.IsNullOrEmpty(Instance.saveSelected?.saveFullFilename))
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, 
                    "选择一个快速开始", 
                    "请选择一个快速开始来游玩，然后重试。",
                    "返回" );
                
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            
            string campaignName = iNewCampaignName.Instance.GetText();
            if ( string.IsNullOrEmpty(campaignName) )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, 
                    "需要战役名称！", 
                    "请为你的战役选择一个名称。" +
                    "这可以让你的存档更加有序。", 
                    "返回" );
                
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            var scenario = Instance.saveSelected.isscenario;
            var forlobby = action == UserRequest.Edit;
            
            var source1 = StartWorldSource1.AnythingElse;
            if (forlobby)
                source1 = StartWorldSource1.StartingTheLobbyFromPrior;
            
            var source2 = StartWorldSource2.LoadingQuickStart;
            if (scenario)
                source2 = StartWorldSource2.LoadingSaveGame;

            void LoadAndStart()
            {
                SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                
                Action<bool> PostAction =
                    (success)=>
                    {
                        if (success)
                        {
                            World.Instance.CampaignName = campaignName;
                            SaveLoadMethods.RestartCampaignData( forlobby, campaignName, scenario );
                            
                            Instance.Close();
                        }
                    };
                
                Engine_AIW2.LoadGameNoCampaignNameSet( Instance.saveSelected.saveFullFilename, source1, source2, false, PostAction );
            };
            
            if ( scenario && forlobby )
            {
                ModalPopupData.CreateAndLogYesNoStyle(
                    ()=>LoadAndStart(),
                    null,
                    "这是一个场景", 
                    "\n这个快速开始实际上是作者选择的完整战役开始存档，旨在预生成的银河系中进行游玩。\n\n"+
                    "你<u>可以</u>继续到大厅并编辑内容。\n\n"+
                    "但请注意，从大厅开始（即使不做任何更改）将不会是与作者相同的银河系。",
                    "打开大厅", "取消");
                
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            
            // regular quickstart, not a scenario
            LoadAndStart();
            
            return MouseHandlingResult.None;
        }
    }
}
