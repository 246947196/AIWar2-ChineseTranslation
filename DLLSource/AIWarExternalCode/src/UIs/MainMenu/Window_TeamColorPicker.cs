using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_TeamColorPicker : ToggleableWindowController
    {
        public ConfigurationForFaction FactionConfig;
        public TeamColorDefinition CurrentFactionCenterColor = null;
        public TeamColorDefinition CurrentFactionTrimColor = null;
        public TeamColorClickHandler OnOkClick;
        private bool hasThisTimeInitialized = false;
        private bool IsFromLobby = false;
        private bool hasInitializedColors = false;

        public void Open( ConfigurationForFaction factionConfig, TeamColorClickHandler OnOk, bool FromLobby )
        {
            hasThisTimeInitialized = true;
            FactionConfig = factionConfig;
            CurrentFactionCenterColor = FactionConfig.FactionCenterColor;
            CurrentFactionTrimColor = FactionConfig.FactionTrimColor;
            OnOkClick = OnOk;
            IsFromLobby = FromLobby;
        }

        public void Open( TeamColorDefinition center, TeamColorDefinition trim, TeamColorClickHandler OnOk, bool FromLobby )
        {
            hasThisTimeInitialized = true;
            FactionConfig = null;
            CurrentFactionCenterColor = center;
            CurrentFactionTrimColor = trim;
            OnOkClick = OnOk;
            IsFromLobby = FromLobby;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !this.hasThisTimeInitialized )
                return false;
            return true;
        }

        public static Window_TeamColorPicker Instance;
        public Window_TeamColorPicker()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            
            TeamColorDefinitionTable.Instance.OnReloading += ()=> this.hasInitializedColors = false;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                if ( Window_TeamColorPicker.Instance != null )
                {
                    if ( Window_TeamColorPicker.Instance.hasInitializedColors == false )
                    {
                        Window_TeamColorPicker.Instance.hasInitializedColors = true;
                        
                        //var showhidden_setting = ArcenSettingTable.Instance.GetRowByNameOrNullIfNotFound( "ShowHiddenColors" );
                        //bool showhidden = GameSettings.Current.GetBoolBySetting( showhidden_setting );

                        bBodyColor.Original.TeamColor = TeamColorDefinitionTable.Instance.Rows[0];
                        bTrimColor.Original.TeamColor = TeamColorDefinitionTable.Instance.Rows[0];
                        
                        //Body and Trim options
                        for ( int i = 1; i < TeamColorDefinitionTable.Instance.Rows.Count; i++ )
                        {
                            var row = TeamColorDefinitionTable.Instance.Rows[i];
                            
                            if (//!showhidden &&
                                 row.IsHidden)
                                continue;

                            {
                                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bBodyColor.Original.Element.DuplicateSelf();
                                bBodyColor newButton = (bBodyColor)button.Controller;
                                newButton.TeamColor = row;
                            }
                            
                            {
                                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bTrimColor.Original.Element.DuplicateSelf();
                                bTrimColor newButton = (bTrimColor)button.Controller;
                                newButton.TeamColor = row;
                            }
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

                        //Random options
                        // not yet working well enough to be useful
                        /*
                        if ( bPrefabColor.Original == null )
                            ArcenDebugging.ArcenDebugLog( "Null bPrefabColor.Original!", Verbosity.ShowAsError );
                        else
                        {
                            ThrowawayListCanMemLeak<TeamColors> colors = new ThrowawayListCanMemLeak<TeamColors>();

                            for ( int i = 0; i < TeamColorPrefabsTable.Instance.Rows.Count; i++ )
                            {
                                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bPrefabColor.Original.Element.DuplicateSelf();
                                bPrefabColor newButton = (bPrefabColor)button.Controller;
                                newButton.TeamColorPrefab = TeamColorPrefabs.AllocateAndLeakIDontCare();
                                newButton.TeamColorPrefab.FactionCenterColor = TeamColorDefinition.AllocateAndLeakIDontCare();
                                newButton.TeamColorPrefab.FactionTrimColor = TeamColorDefinition.AllocateAndLeakIDontCare();
                                
                                for ( int j = 0; j < World_AIW2.Instance.Setup.FactionConfigurations.Count; j++ )
                                {
                                    ConfigurationForFaction factionConfig = World_AIW2.Instance.Setup.FactionConfigurations[j];
                                    if ( factionConfig == null )
                                        continue;

                                    var center = factionConfig.FactionCenterColor.TeamColor;
                                    var trim = factionConfig.FactionTrimColor.TeamColor;

                                    var col = new TeamColors()
                                    {
                                        Center = new HSV(center),
                                        Border = new HSV(trim),
                                    };
                                    colors.Add(col);
                                }
                                
                                PickUniqueColors(colors, newButton.TeamColorPrefab);
                            }
                        }
                        */
                    }                 
                }
            }

            public struct HSV
            {
                public float Hue;
                public float Saturation;
                public float Value;

                public HSV(Color rgb)
                {
                    Color.RGBToHSV(rgb, out Hue, out Saturation, out Value);
                }
            }

            public struct TeamColors
            {
                public HSV Center;
                public HSV Border;
            }

            private void PickUniqueColors( ThrowawayListCanMemLeak<TeamColors> colors, TeamColorPrefabs teamColorPrefab )
            {
                // Get unique center color.
                
                float hue = 0.0f;
                float saturation = 0.0f;
                float value = 0.0f;


                int maxtries = 100;
                for ( int i = 0; i < maxtries; i++ )
                { 
                    hue = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);
                    bool fail = false;
                    foreach (var c in colors)
                    {
                        float dif = Mathf.Abs(c.Center.Hue - hue);
                        if (dif < 0.2f)
                        {
                            fail = true;
                            break;
                        }
                    }

                    if (!fail)
                        break;
                }

                // pick a saturation and value that is equivalent to other faction center colors

                float avgSat = 0.0f;
                float avgVal = 0.0f;
                foreach (var c in colors)
                {
                    avgSat += c.Center.Saturation;
                    avgVal += c.Center.Value;
                }
                avgSat /= colors.Count;
                avgVal /= colors.Count;

                maxtries = 100;
                for ( int i = 0; i < maxtries; i++ )
                { 
                    saturation = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);
                    float dif = Mathf.Abs(saturation - avgSat);
                    if (dif < 0.3f)
                        break;
                }

                maxtries = 100;
                for ( int i = 0; i < maxtries; i++ )
                { 
                    value = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);
                    float dif = Mathf.Abs(value - avgVal);
                    if (dif < 0.3f)
                        break;
                }

                var newCenterColor = Color.HSVToRGB(hue, saturation, value);

                // pick a random border color that is ideally higher value and saturation, while not being the same hue

                float bhue = 0.0f;
                float bsaturation = 0.0f;
                float bvalue = 0.0f;

                maxtries = 100;
                for ( int i = 0; i < maxtries; i++ )
                { 
                    bhue = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);

                    float dif = Mathf.Abs(hue - bhue);
                    if (dif < 0.2f)
                        continue;
                }

                maxtries = 100;
                if (saturation >= 0.9f)
                {
                    bsaturation = 1.0f;
                }
                else
                {
                    for ( int i = 0; i < maxtries; i++ )
                    { 
                        bsaturation = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);

                        float dif = bsaturation - saturation;
                        if (dif < 0.1f)
                            continue;
                    }
                }

                maxtries = 100;
                if (value >= 0.9f)
                {
                    bvalue = 1.0f;
                }
                else
                {
                    for ( int i = 0; i < maxtries; i++ )
                    { 
                        bvalue = Engine_Universal.PermanentQualityRandom.NextFloat(1.0f);

                        float dif = bvalue - value;
                        if (dif < 0.1f)
                            continue;
                    }
                }

                var newBorderColor = Color.HSVToRGB(bhue, bsaturation, bvalue);

                teamColorPrefab.FactionCenterColor.TeamColor = newCenterColor;
                teamColorPrefab.FactionTrimColor.TeamColor = newBorderColor;
            }
        }

        public class bOK : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_TeamColorPicker.Instance.CurrentFactionCenterColor == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择基础颜色！", "请在左侧进行选择以设置基础颜色。", "确定" );
                    return MouseHandlingResult.None;
                }
                if ( Window_TeamColorPicker.Instance.CurrentFactionTrimColor == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择装饰颜色！", "请在右侧进行选择以设置装饰颜色。", "确定" );
                    return MouseHandlingResult.None;
                }

                Window_TeamColorPicker.Instance.OnOkClick?.Invoke( Window_TeamColorPicker.Instance.CurrentFactionCenterColor, Window_TeamColorPicker.Instance.CurrentFactionTrimColor );
                Window_TeamColorPicker.Instance.hasThisTimeInitialized = false;

                if ( !Instance.IsFromLobby)
                    ArcenDebugging.ArcenDebugLogSingleLine( "Base Color:  " + Window_TeamColorPicker.Instance.CurrentFactionCenterColor.InternalName + "    Trim Color:  " +
                        Window_TeamColorPicker.Instance.CurrentFactionTrimColor.InternalName, Verbosity.DoNotShow );

                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "确定" );
            }
        }

        /// <summary>
        /// For changing the body color
        /// </summary>
        public class bBodyColor : ImageButtonAbstractBase
        {
            public static bBodyColor Original;
            public bBodyColor() { if ( Original == null ) Original = this; }

            public TeamColorDefinition TeamColor;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Window_TeamColorPicker.Instance.CurrentFactionCenterColor = this.TeamColor;
                return MouseHandlingResult.None;
            }
            private float timeUntilSwitch = 0;
            private bool isShowingAlt = true;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPicker.Instance == null || this.TeamColor == null )
                    return;

                if ( Window_TeamColorPicker.Instance.CurrentFactionCenterColor == this.TeamColor )
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
        /// For changing the trim color
        /// </summary>
        public class bTrimColor : ImageButtonAbstractBase
        {
            public static bTrimColor Original;
            public bTrimColor() { if ( Original == null ) Original = this; }

            public TeamColorDefinition TeamColor;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Window_TeamColorPicker.Instance.CurrentFactionTrimColor = this.TeamColor;
                return MouseHandlingResult.None;
            }
            private float timeUntilSwitch = 0;
            private bool isShowingAlt = true;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPicker.Instance == null || this.TeamColor == null )
                    return;

                if ( Window_TeamColorPicker.Instance.CurrentFactionTrimColor == this.TeamColor )
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
                Window_TeamColorPicker.Instance.CurrentFactionCenterColor = this.TeamColorPrefab.FactionCenterColor;
                Window_TeamColorPicker.Instance.CurrentFactionTrimColor = this.TeamColorPrefab.FactionTrimColor;
                return MouseHandlingResult.None;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Window_TeamColorPicker.Instance == null || this.TeamColorPrefab == null )
                    return;

                Image.SetColor( this.TeamColorPrefab.FactionTrimColor.TeamColor );
                SubImages[0].WrapperedImage.SetColor( this.TeamColorPrefab.FactionCenterColor.TeamColor );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( 
                    this.Element, 
                    "基础 " + this.TeamColorPrefab.FactionCenterColor.GetDisplayName() +
                    "\n" +
                    "装饰 " + this.TeamColorPrefab.FactionTrimColor.GetDisplayName()
                    );
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
                var config = Instance.FactionConfig;
                if (config != null)
                {
                    var center = config.SpecialFactionData.TexEmbedSprite_Icon;
                    var trim = config.SpecialFactionData.TexEmbedSprite_IconBorder;

                    if (trim != null)
                        Image.UpdateWith(trim?.UnitySprite, ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show);
                    if (center != null)
                        SubImages[0].WrapperedImage.UpdateWith(center?.UnitySprite, ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show);
                }

                if ( Window_TeamColorPicker.Instance.CurrentFactionTrimColor != null )
                    Image.SetColor( Window_TeamColorPicker.Instance.CurrentFactionTrimColor.TeamColor );
                if ( Window_TeamColorPicker.Instance.CurrentFactionCenterColor != null )
                    SubImages[0].WrapperedImage.SetColor( Window_TeamColorPicker.Instance.CurrentFactionCenterColor.TeamColor );
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
                var config = Instance.FactionConfig;
                if (config != null)
                {
                    var center = config.SpecialFactionData.TexEmbedSprite_Icon;
                    var trim = config.SpecialFactionData.TexEmbedSprite_IconBorder;

                    if (center != null)
                        Image.UpdateWith(center?.UnitySprite, ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show);
                    if (trim != null)
                        SubImages[0].WrapperedImage.UpdateWith(trim?.UnitySprite, ArcenUIWrapperedUnityImage.ShowOrHideStatus.Show);
                }

                if ( Window_TeamColorPicker.Instance.CurrentFactionCenterColor != null )
                    Image.SetColor( Window_TeamColorPicker.Instance.CurrentFactionCenterColor.TeamColor );
                if ( Window_TeamColorPicker.Instance.CurrentFactionTrimColor != null )
                    SubImages[0].WrapperedImage.SetColor( Window_TeamColorPicker.Instance.CurrentFactionTrimColor.TeamColor );
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_TeamColorPicker.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }        
    }

    public delegate void TeamColorClickHandler( TeamColorDefinition CenterColor, TeamColorDefinition TrimColor );
}
