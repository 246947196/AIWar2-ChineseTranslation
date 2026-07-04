using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_TeamColorPickerTrimOnly : ToggleableWindowController
    {
        public TeamColorDefinition CurrentFactionCenterColorViewOnly = null;
        public TeamColorDefinition CurrentFactionTrimColor = null;
        public TeamColorTrimOnlyClickHandler OnOkClick;
        private bool hasThisTimeInitialized = false;
        private bool hasInitializedColors = false;
        //yes this is from the lobby, it is ALWAYS from the lobby

        public void Open( TeamColorDefinition CenterColorViewOnly, TeamColorDefinition TrimColor, TeamColorTrimOnlyClickHandler OnOk )
        {
            hasThisTimeInitialized = true;
            CurrentFactionCenterColorViewOnly = CenterColorViewOnly;
            CurrentFactionTrimColor = TrimColor;
            OnOkClick = OnOk;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !this.hasThisTimeInitialized )
                return false;
            return true;
        }

        public static Window_TeamColorPickerTrimOnly Instance;
        public Window_TeamColorPickerTrimOnly()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            TeamColorDefinitionTable.Instance.OnReloading += ()=> this.hasInitializedColors = false;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                if ( Window_TeamColorPickerTrimOnly.Instance != null )
                {
                    if ( Window_TeamColorPickerTrimOnly.Instance.hasInitializedColors == false )
                    {
                        Window_TeamColorPickerTrimOnly.Instance.hasInitializedColors = true;
                        
                        bTrimColor.Original.TeamColor = TeamColorDefinitionTable.Instance.Rows[0];
                        
                        //Trim options
                        for ( int i = 1; i < TeamColorDefinitionTable.Instance.Rows.Count; i++ )
                        {
                            var row = TeamColorDefinitionTable.Instance.Rows[i];
                            
                            if (//!showhidden &&
                                 row.IsHidden)
                                continue;

                            ArcenUI_ImageButton button = (ArcenUI_ImageButton)bTrimColor.Original.Element.DuplicateSelf();
                            bTrimColor newButton = (bTrimColor)button.Controller;
                            newButton.TeamColor = row;
                        }
                        
                        //Prefab options
                        if ( bPrefabColor.Original == null )
                            ArcenDebugging.ArcenDebugLog( "Null bPrefabColor.Original!", Verbosity.ShowAsError );
                        else
                        {
                            for ( int i = 1; i < TeamColorPrefabsTable.Instance.Rows.Count; i++ )
                            {
                                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bPrefabColor.Original.Element.DuplicateSelf();
                                bPrefabColor newButton = (bPrefabColor)button.Controller;
                                newButton.TeamColorPrefab = TeamColorPrefabsTable.Instance.Rows[i];
                            }
                            bPrefabColor.Original.TeamColorPrefab = TeamColorPrefabsTable.Instance.Rows[0];
                        }
                    }                 
                }
            }
        }

        public class bOK : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择装饰颜色！", "请在上方区域进行选择以设置装饰颜色。", "确定" );
                    return MouseHandlingResult.None;
                }

                Window_TeamColorPickerTrimOnly.Instance.OnOkClick?.Invoke( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor );
                Window_TeamColorPickerTrimOnly.Instance.hasThisTimeInitialized = false;

                //UnityEngine.Debug.Log( "Trim Color:  " + Window_TeamColorPickerTrimOnly.Instance.CurrentTrimColor.InternalName );

                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "确定" );
            }
        }
        
        /// <summary>
        /// For changing the trim color
        /// </summary>
        public class bTrimColor : ImageButtonAbstractBase
        {
            public static bTrimColor Original;
            public bTrimColor() { if ( Original == null ) Original = this; }

            public TeamColorDefinition TeamColor;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor = this.TeamColor;
                return MouseHandlingResult.None;
            }
            private float timeUntilSwitch = 0;
            private bool isShowingAlt = true;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPickerTrimOnly.Instance == null || this.TeamColor == null )
                    return;

                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor == this.TeamColor )
                {
                    SubImages[0].SetActiveIfNeeded( isShowingAlt );

                    this.timeUntilSwitch -= Engine_Universal.UnscaledDeltaTime;
                    if ( this.timeUntilSwitch <= 0 )
                    {
                        this.timeUntilSwitch = 0.15f;
                        this.isShowingAlt = !this.isShowingAlt;
                    }
                }
                else
                    SubImages[0].SetActiveIfNeeded( false );
                Image.SetColor( this.TeamColor.TeamColor );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, this.TeamColor.GetDisplayName() );
            }
            
            public override bool GetShouldBeHidden()
            {
                return TeamColor?.IsHidden ?? true;
            }
        }

        /// <summary>
        /// For setting colors from a prefab
        /// </summary>
        public class bPrefabColor : ImageButtonAbstractBase
        {
            public static bPrefabColor Original;
            public bPrefabColor() { if ( Original == null ) Original = this; }

            public TeamColorPrefabs TeamColorPrefab;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor = this.TeamColorPrefab.FactionTrimColor;
                return MouseHandlingResult.None;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPickerTrimOnly.Instance == null || this.TeamColorPrefab == null )
                    return;

                Image.SetColor( this.TeamColorPrefab.FactionTrimColor.TeamColor );
            }
        }

        /// <summary>
        /// For SHOWING the team color, not changing it.
        /// </summary>
        public class bTeamColorInverted : ImageButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                //not really using this as a button at all.
                return MouseHandlingResult.None;
            }
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor != null )
                    Image.SetColor( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor.TeamColor );
                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionCenterColorViewOnly != null )
                    SubImages[0].WrapperedImage.SetColor( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionCenterColorViewOnly.TeamColor );
            }
        }

        /// <summary>
        /// For SHOWING the team color, not changing it.
        /// </summary>
        public class bTeamColor : ImageButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                //not really using this as a button at all.
                return MouseHandlingResult.None;
            }
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionCenterColorViewOnly != null )
                    Image.SetColor( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionCenterColorViewOnly.TeamColor );
                if ( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor != null )
                    SubImages[0].WrapperedImage.SetColor( Window_TeamColorPickerTrimOnly.Instance.CurrentFactionTrimColor.TeamColor );
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_TeamColorPickerTrimOnly.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }        
    }

    public delegate void TeamColorTrimOnlyClickHandler( TeamColorDefinition TrimColor );
}
