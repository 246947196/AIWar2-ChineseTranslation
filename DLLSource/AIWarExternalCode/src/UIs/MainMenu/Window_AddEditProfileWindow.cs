using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_AddEditProfileWindow : ToggleableWindowController
    {
        public static bool IsInForcedCreateMode = false;
        
        public bool hasThisTimeInitialized = false;

        public static Window_AddEditProfileWindow Instance;
        public Window_AddEditProfileWindow()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_AddEditProfileWindow.Instance != null )
                {
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                        
                    }

                    if ( !Window_AddEditProfileWindow.Instance.hasThisTimeInitialized )
                    {
                        Window_AddEditProfileWindow.Instance.hasThisTimeInitialized = true;

                        bool isInCreateMode = IsInForcedCreateMode || PlayerProfile.Local == null || !PlayerProfile.Local.WasCreatedByPlayer;
                        if ( isInCreateMode )
                        {
                            iProfileName.Instance.SetText( string.Empty );
                            bTeamColor.Instance.CurrentFactionCenterColor = TeamColorDefinitionTable.Instance.DefaultFactionCenterColor;
                            bTeamColor.Instance.CurrentFactionTrimColor = TeamColorDefinitionTable.Instance.DefaultFactionTrimColor;
                        }
                        else
                        {
                            iProfileName.Instance.SetText( PlayerProfile.Local.DisplayName );
                            if ( PlayerProfile_AIW2.Local != null )
                            {
                                bTeamColor.Instance.CurrentFactionCenterColor = PlayerProfile_AIW2.Local.DefaultFactionCenterColor;
                                bTeamColor.Instance.CurrentFactionTrimColor = PlayerProfile_AIW2.Local.DefaultFactionTrimColor;
                            }
                            else
                            {
                                bTeamColor.Instance.CurrentFactionCenterColor = TeamColorDefinitionTable.Instance.DefaultFactionCenterColor;
                                bTeamColor.Instance.CurrentFactionTrimColor = TeamColorDefinitionTable.Instance.DefaultFactionTrimColor;
                            }
                        }
                    }
                }
            }
        }

        public class bOK : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                string profileName = iProfileName.Instance.GetText();
                if ( profileName == null || profileName.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "请输入档案名称！", "请为自己选择一个名称。你会在游戏中看到这个名字，与你一起多人游戏的玩家也会看到。", "返回" );
                    return MouseHandlingResult.None;
                }

                bool isInCreateMode = IsInForcedCreateMode || PlayerProfile.Local == null || !PlayerProfile.Local.WasCreatedByPlayer;
                if ( isInCreateMode )
                {
                    PlayerProfile.Local = PlayerProfile.CreateBrandNew( profileName );
                    PlayerProfile_AIW2.Local = new PlayerProfile_AIW2();
                    PlayerProfile.Local.WasCreatedByPlayer = true;
                    
                    PlayerProfile_AIW2.Local.DefaultFactionCenterColor = bTeamColor.Instance.CurrentFactionCenterColor;
                    PlayerProfile_AIW2.Local.DefaultFactionTrimColor = bTeamColor.Instance.CurrentFactionTrimColor;
                    PlayerProfile.Local.SaveToDisk();
                }
                else
                {
                    PlayerProfile.Local.DisplayName = profileName;
                    PlayerProfile_AIW2.Local.DefaultFactionCenterColor = bTeamColor.Instance.CurrentFactionCenterColor;
                    PlayerProfile_AIW2.Local.DefaultFactionTrimColor = bTeamColor.Instance.CurrentFactionTrimColor;
                    PlayerProfile.Local.SaveToDisk();
                }

                Window_AddEditProfileWindow.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "确定" );
            }
        }

        public class bTeamColor : ImageButtonAbstractBase
        {
            public TeamColorDefinition CurrentFactionCenterColor = null;
            public TeamColorDefinition CurrentFactionTrimColor = null;
            public static bTeamColor Instance;
            public bTeamColor() { Instance = this; }

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Window_TeamColorPicker.Instance.Open( this.CurrentFactionCenterColor, this.CurrentFactionTrimColor,
                    delegate ( TeamColorDefinition CenterColor, TeamColorDefinition TrimColor )
                    {
                        this.CurrentFactionCenterColor = CenterColor;
                        this.CurrentFactionTrimColor = TrimColor;
                    }, false );
                return MouseHandlingResult.None;
            }
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( CurrentFactionCenterColor != null )
                    Image.SetColor( CurrentFactionCenterColor.TeamColor );
                if ( CurrentFactionTrimColor != null )
                    SubImages[0].WrapperedImage.SetColor( CurrentFactionTrimColor.TeamColor );
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( PlayerProfile.Local == null || !PlayerProfile.Local.WasCreatedByPlayer )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须创建档案！", "请创建一个档案才能继续。\n\n你可以稍后更改，所以没有压力。但我们确实需要一个！", "确定" );
                    return MouseHandlingResult.None;
                }
                Window_AddEditProfileWindow.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( PlayerProfile.Local == null || !PlayerProfile.Local.WasCreatedByPlayer || IsInForcedCreateMode )
                    Buffer.Add( "创建新的玩家档案" );
                else
                    Buffer.Add( "编辑玩家档案" );
            }
            public override void OnUpdate() { }
        }

        public class tPurposeText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile(ArcenDoubleCharacterBuffer Buffer)
            {
                Buffer.Add("选择你想在主力舰上担任顾问的角色。这只会影响你听到的声音，\n 你可以在下方预览。同时请输入你的名称，并选择队伍颜色。");
            }
        }

        public class iProfileName : InputAbstractBase
        {
            public int maxProfileNameLen = 30;
            public static iProfileName Instance;
            public iProfileName() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxProfileNameLen )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' || addedChar == ' ' || addedChar == '-' )
                    return addedChar;
                //block everything except alphanumerics and _ and -
                //for right now
                return '\0';
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
    }
}
