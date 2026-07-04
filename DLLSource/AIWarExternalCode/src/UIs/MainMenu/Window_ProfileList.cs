using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.IO;
using System.Linq.Dynamic;

namespace Arcen.AIW2.External
{
    public class Window_ProfileList : ToggleableWindowController, IInputActionHandler
    {
        public override void OnOpen() 
        {
            this.PopulateListOfProfiles();
        }

        private static readonly List<PlayerProfile> profiles = List<PlayerProfile>.Create_WillNeverBeGCed( 500, "Window_ProfileList-profiles" );

        public void PopulateListOfProfiles()
        {
            profiles.Clear();

            string saveFolder = Engine_Universal.CurrentPlayerDataDirectory + "Profiles/";

            if ( !Directory.Exists( saveFolder ) )
                Directory.CreateDirectory( saveFolder );
            
            var allProfiles = Directory.EnumerateFiles( saveFolder, "*.aiwprof" );

            foreach ( string fileName in allProfiles )
            {
                string profileName = Path.GetFileNameWithoutExtension( fileName );
                
                //don't reload the existing profile, just set it now
                if ( PlayerProfile.Local?.Filename == profileName )
                {
                    profiles.Add( PlayerProfile.Local );
                    continue;
                }

                try
                {
                    var prof = PlayerProfile.LoadFromDisk( profileName, PlayerProfile.Complaints.None, PlayerProfile.SaveAsLastProfileSelected.No, PlayerProfile.ErrorStyle.HideAll );
                    
                    if ( prof == null && File.Exists( fileName ) )
                    {
                        File.Delete( fileName );
                        continue;
                    }
                    
                    profiles.Add( prof );
                }
                catch (Exception)
                {
                    if ( File.Exists( fileName ) )
                        File.Delete( fileName );
                }
            }
        }

        public static Window_ProfileList Instance;
        public Window_ProfileList()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            private static ButtonAbstractBase.ButtonPool<bColumnButton> btnColumnButtonPool;

            public override void OnUpdate()
            {
                if ( Window_ProfileList.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bColumnButton.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnColumnButtonPool = new ButtonAbstractBase.ButtonPool<bColumnButton>( bColumnButton.Original, 60 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCategories();
            }

            public void OnUpdateCategories()
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnColumnButtonPool.Clear( 40 );

                if ( profiles.Count > 0 )
                {
                    for ( int i = 0; i < profiles.Count; i++ )
                    {
                        PlayerProfile profile = profiles[i];
                        bColumnButton item = btnColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( profile );
                    }
                }
                
                #region Positioning Logic 1
                btnColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 30, 463.7f, 28 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bColumnButton.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bColumnButton
        public class bColumnButton : ButtonAbstractBase
        {
            public static bColumnButton Original;
            public bColumnButton() { if ( Original == null ) Original = this; }

            private PlayerProfile Profile = null;

            public void Assign( PlayerProfile Profile )
            {
                this.Profile = Profile;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Profile == null;
            }

            public override void Clear()
            {
                this.Profile = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Profile == null )
                    return;

                if ( PlayerProfile.Local == this.Profile )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.Profile.DisplayName );

                if ( PlayerProfile.Local == this.Profile )
                    buffer.EndColor();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Profile == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                //select the clicked on
                PlayerProfile.Local = this.Profile;

                if ( input.LeftButtonDoubleClicked )
                {
                    //edit the selected one if double-clicked
                    Window_AddEditProfileWindow.IsInForcedCreateMode = false;
                    Window_AddEditProfileWindow.Instance.Open();
                    Instance.Close();
                }

                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "选择档案" );
            }
        }
        #endregion

        public class btnNew : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_AddEditProfileWindow.IsInForcedCreateMode = true;
                Window_AddEditProfileWindow.Instance.Open();
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "新建" );
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "拥有多个档案可以让你以不同的名字加入多人游戏。" );
            }
        }

        public class btnOk : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( PlayerProfile.Local == null )
                {
                    if ( profiles.Count > 0 )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择一个档案！", "请选择一个档案进行编辑。", "确定" );
                        return MouseHandlingResult.None;
                    }
                    else //nothing is here, so we'd better create a new one
                    {
                        Window_AddEditProfileWindow.IsInForcedCreateMode = true;
                        Window_AddEditProfileWindow.Instance.Open();
                        Instance.Close();
                        return MouseHandlingResult.None;
                    }
                }

                //edit the selected one
                Window_AddEditProfileWindow.IsInForcedCreateMode = false;
                Window_AddEditProfileWindow.Instance.Open();
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "编辑" );
            }
        }

        public class btnDelete : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( PlayerProfile.Local == null )
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择一个档案！", "请选择一个档案进行删除。", "确定" );
                else
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        string saveFolder = Engine_Universal.CurrentPlayerDataDirectory + "Profiles/";
                        string newFileName = saveFolder + PlayerProfile.Local.Filename + ".aiwprof";
                        if ( File.Exists( newFileName ) )
                            File.Delete( newFileName );

                        Instance.PopulateListOfProfiles();
                        if ( profiles.Count > 0 )
                            PlayerProfile.Local = profiles[0];
                        else
                        {
                            PlayerProfile.Local = null;
                            Window_AddEditProfileWindow.IsInForcedCreateMode = true;
                            Window_AddEditProfileWindow.Instance.Open();
                        }

                    }, null, "删除此档案？", "确定要删除档案 '" + PlayerProfile.Local.DisplayName + "' 吗？", "是，删除", "不，等等！" );
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "删除" );
            }
        }

        public class btnCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "返回" );
            }
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    Instance.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }

    public delegate void Window_ProfileListClickHandler( RefThreeTuple<string, string, string> SelectedOption );
}
