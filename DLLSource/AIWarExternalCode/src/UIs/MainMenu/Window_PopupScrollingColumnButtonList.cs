using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_PopupScrollingColumnButtonList : ToggleableWindowController, IInputActionHandler
    {
        public CustomPopupData CurrentSelection = null;
        public ProtectedList<CustomPopupData> AllOptions = null;
        public Window_PopupScrollingColumnButtonListClickHandler OnOkClick;
        public Window_PopupScrollingColumnButtonListTooltipHandler TooltipHandler;
        public string HeaderString;
        public string OptionAddedByText;
        private bool hasThisTimeInitialized = false;

        /// <summary>
        /// String1: InternalName
        /// String2: DisplayName
        /// String3: Tooltip
        /// </summary>
        public void Open( string Header, CustomPopupData CurrentSelect, ProtectedList<CustomPopupData> Options, Window_PopupScrollingColumnButtonListClickHandler OnOk,
            Window_PopupScrollingColumnButtonListTooltipHandler tooltipHandler, string OptionAddedBy = "此选项由以下内容添加：" )
        {
            hasThisTimeInitialized = true;
            HeaderString = Header;
            CurrentSelection = CurrentSelect;
            AllOptions = Options;
            OnOkClick = OnOk;
            TooltipHandler = tooltipHandler;
            OptionAddedByText = OptionAddedBy;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !this.hasThisTimeInitialized )
                return false;
            return true;
        }

        public static Window_PopupScrollingColumnButtonList Instance;
        public Window_PopupScrollingColumnButtonList()
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
                if ( Window_PopupScrollingColumnButtonList.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bColumnButton.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnColumnButtonPool = new ButtonAbstractBase.ButtonPool<bColumnButton>( bColumnButton.Original, 10 );
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

                btnColumnButtonPool.Clear( 10 );

                if ( Instance.AllOptions != null )
                {

                    for ( int i = 0; i < Instance.AllOptions.Count; i++ )
                    {
                        CustomPopupData option = Instance.AllOptions[i];
                        bColumnButton item = btnColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( option );
                    }
                }
                
                #region Positioning Logic 1
                btnColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 30, 333.1f, 28 );
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

            private CustomPopupData ButtonData = null;

            public void Assign( CustomPopupData ButtonData )
            {
                this.ButtonData = ButtonData;
            }

            public override bool GetShouldBeHidden()
            {
                return this.ButtonData == null;
            }

            public override void Clear()
            {
                this.ButtonData = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.ButtonData == null )
                    return;

                if ( this.ButtonData.TexEmbedSprite_Icon != null )
                {
                    // if you see bright fuschia it means no one set a proper color!
                    string iconColor = "ff00ff";
                    string iconBorderColor = "ff00ff";
                    string iconOverlayColor = "ffffff"; // but, this isn't really exposed for anyone TOO set so just white i guess
                    if ( this.ButtonData.IconColor != string.Empty )
                        iconColor = this.ButtonData.IconColor;
                    if ( this.ButtonData.IconBorderColor != string.Empty )
                        iconBorderColor = this.ButtonData.IconBorderColor;
                    else
                        iconBorderColor = iconColor;
                    if ( this.ButtonData.IconOverlayColor != string.Empty )
                        iconOverlayColor = this.ButtonData.IconOverlayColor;

                    buffer.Add( "<size=50%><voffset=1.5em>" );

                    buffer.StartDoNotAdvanceXTagIfTrue( this.ButtonData.TexEmbedSprite_IconBorder != null || this.ButtonData.TexEmbedSprite_IconOverlay != null );
                    buffer.AddIconAsColor( this.ButtonData.TexEmbedSprite_Icon.InternalName, iconColor );
                    
                    if ( this.ButtonData.TexEmbedSprite_IconBorder != null )
                    {
                        buffer.EndDoNotAdvanceXTagIfTrue( this.ButtonData.TexEmbedSprite_IconOverlay == null );
                        buffer.AddIconAsColor( this.ButtonData.TexEmbedSprite_IconBorder.InternalName, iconBorderColor );
                    }
                    if ( this.ButtonData.TexEmbedSprite_IconOverlay != null )
                    {
                        buffer.EndDoNotAdvanceXTagIfTrue( true );
                        buffer.AddIconAsColor( this.ButtonData.TexEmbedSprite_IconOverlay.InternalName, iconOverlayColor );
                    }
                    buffer.Add( "</size></voffset>  " );
                }

                if (! this.ButtonData.CanBeSelected )
                    buffer.Add( "<color=#e9493d>" );
                else if ( Instance.CurrentSelection == this.ButtonData )
                    buffer.Add( "<color=#6fafff>" );
                buffer.Add( this.ButtonData.DisplayName );
                buffer.EndColor();

				// experiment putting these codes on the right side
                //buffer.Add("<align=\"right\">");
                //buffer.Add("<line-height=0>");
                //buffer.NewLine();
                buffer.AddDlcMod(this.ButtonData);
                //buffer.Add("  </align></line-height>");
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.ButtonData == null || !this.ButtonData.CanBeSelected )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentSelection = this.ButtonData;

                if ( input.LeftButtonDoubleClicked )
                {
                    Window_PopupScrollingColumnButtonList.Instance.OnOkClick?.Invoke( Window_PopupScrollingColumnButtonList.Instance.CurrentSelection );
                    Window_PopupScrollingColumnButtonList.Instance.hasThisTimeInitialized = false;
                    Instance.Close();
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_PopupScrollingColumnButtonList-bColumnButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.ButtonData == null )
                    return;
                
                if ( !this.ButtonData.CanBeSelected )
                    tooltipBuffer.StartColor( "e9493d" );

                tooltipBuffer.Add( this.ButtonData.DisplayName ).NewLine();

                if ( !this.ButtonData.CanBeSelected )
                {
                    tooltipBuffer.Add( "（不可用）" );

                    if ( this.ButtonData.CannotBeSelectedReason.Length > 0 )
                        tooltipBuffer.Add("\n").Add( this.ButtonData.CannotBeSelectedReason );

                    tooltipBuffer.EndColor();
                }

                if ( Instance.TooltipHandler != null )
                    Instance.TooltipHandler( tooltipBuffer, this.ButtonData );
                else
                    tooltipBuffer.Add(this.ButtonData.Tooltiptext);

                tooltipBuffer.AddDlcMod(this.ButtonData, Instance.OptionAddedByText );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.HeaderString );
            }
        }
        #endregion

        public class btnOk : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_PopupScrollingColumnButtonList.Instance.CurrentSelection == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "必须选择一个选项！", "请先进行选择。", "确定" );
                    return MouseHandlingResult.None;
                }

                Window_PopupScrollingColumnButtonList.Instance.OnOkClick?.Invoke( Window_PopupScrollingColumnButtonList.Instance.CurrentSelection );
                Window_PopupScrollingColumnButtonList.Instance.hasThisTimeInitialized = false;

                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "确定" );
            }
        }

        public class btnCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_PopupScrollingColumnButtonList.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    Window_PopupScrollingColumnButtonList.Instance.hasThisTimeInitialized = false;
                    Instance.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }

    public delegate void Window_PopupScrollingColumnButtonListClickHandler( CustomPopupData SelectedOption );
    public delegate void Window_PopupScrollingColumnButtonListTooltipHandler( ArcenDoubleCharacterBuffer Buffer, CustomPopupData HoveredOption );

    public class CustomPopupData : ArcenExternalSource, IConcurrentPoolable<CustomPopupData>, IProtectedListable
    {
        public string InternalName = string.Empty;
        public string DisplayName = string.Empty;
        public string SortingName = string.Empty;
        public string Tooltiptext = string.Empty;
        public string IconToUse = string.Empty;
        public TextEmbededSprite TexEmbedSprite_Icon = null;
        public TextEmbededSprite TexEmbedSprite_IconBorder = null;
        public TextEmbededSprite TexEmbedSprite_IconOverlay = null;
        public string IconColor = string.Empty;
        public string IconBorderColor = string.Empty;
        public string IconOverlayColor = string.Empty;
        public int InternalID = 0;
        public bool CanBeSelected = true;
        public string CannotBeSelectedReason = string.Empty;

        public void SetToDefaults()
        {
            InternalName = string.Empty;
            DisplayName = string.Empty;
            SortingName = string.Empty;
            Tooltiptext = string.Empty;
            InternalID = 0;
            CanBeSelected = true;
            CannotBeSelectedReason = string.Empty;
            TexEmbedSprite_Icon = null;
            TexEmbedSprite_IconBorder = null;
            TexEmbedSprite_IconOverlay = null;
            IconColor = "";
            IconBorderColor = "";
            IconOverlayColor = "";
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private CustomPopupData() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "CustomPopupDatas" );
            RefTracker.IncrementObjectCount();

            this.SetToDefaults();
        }

        private static ConcurrentPool<CustomPopupData> Pool = new ConcurrentPool<CustomPopupData>( "CustomPopupDatas", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new CustomPopupData(); } );

        public static CustomPopupData GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        public bool GetInPoolStatus()
        {
            return this.isInPool;
        }

        private bool isInPool = false;
        public void SetInPoolStatus( bool IsInPool )
        {
            this.isInPool = IsInPool;
        }

        public void DoBeforeRemoveOrClear()
        {
            this.SetToDefaults();
            this.ReturnToPool();
        }
        #endregion
    }
}
