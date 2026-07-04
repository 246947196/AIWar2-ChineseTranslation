using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Net;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcen.AIW2.External
{
    public class Window_JoinMultiplayerGameByListMenu : Window_DynamicallyFilledAbstractBase, IInputActionHandler
    {
        public static Window_JoinMultiplayerGameByListMenu Instance;
        
        private bool _open;
        
        public Window_JoinMultiplayerGameByListMenu()
        {
            Instance = this;
            
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
        }

        #region Open/Close Stuff
        
        public override void Close()
        {
            _open = false;
        }

        public void Open()
        {
            _open = true;    
        }
        
        public bool GetIsOpen()
        {
            return _open;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return _open;
        }
        
        #endregion

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ModalTextboxWindow.Instance != null )
                {
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                    }
                }
            }
        }


        //private float leftWidth_Normal = 200;
        //private float rightWidth_Normal = 240;

        private float leftWidth_VeryWideTriple = 80;
        private float rightWidth_VeryWideTriple = 30;
        private float thirdWidth_VeryWideTriple = 360;

        //private float leftWidth_VeryWideTriple = 280;
        //private float rightWidth_VeryWideTriple = 80;
        //private float thirdWidth_VeryWideTriple = 80;

        public static string LastHostChosen = string.Empty;

        //private float fullWidth = 440;

        private ArcenCachedExternalTypeDirect type_bConnect = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bConnect ) );
        private ArcenCachedExternalTypeDirect type_tServerOption = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tServerOption ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            float runningY = topBuffer;

            Rect leftBounds;
            Rect rightBounds;
            Rect thirdBounds;

            int filterIndex = ArcenNetworkAuthority.ActiveSocket.GetCurrentNetworkConnectionOptionIndex();
            for ( int i = 0; i < ArcenNetworkAuthority.PotentialServerOptions.Count; i++ )
            {
                ArcenNetworkConnectionOption option = ArcenNetworkAuthority.PotentialServerOptions[i];
                if ( !option.GetDoesMatchCurrentNetworkOptionFilter( filterIndex ) )
                    continue;

                #region Servere *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsTriple( out leftBounds, out rightBounds, out thirdBounds, ref runningY, leftWidth_VeryWideTriple, rightWidth_VeryWideTriple, thirdWidth_VeryWideTriple );
                AddButton( Set, type_bConnect, string.Empty, i, i, leftBounds, -1f );
                AddText( Set, type_tServerOption, string.Empty, i, i, thirdBounds, 13f );
                #endregion *****************************************
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int filterIndex = ArcenNetworkAuthority.ActiveSocket.GetCurrentNetworkConnectionOptionIndex();
                int count = 0;
                for ( int i = 0; i < ArcenNetworkAuthority.PotentialServerOptions.Count; i++ )
                {
                    ArcenNetworkConnectionOption option = ArcenNetworkAuthority.PotentialServerOptions[i];
                    if ( option.GetDoesMatchCurrentNetworkOptionFilter( filterIndex ) )
                        count++;
                }
                Buffer.Add( ArcenNetworkAuthority.ActiveSocket.GetNetworkConnectionOptionHeader() ).Add( "  (" ).AddNumberMoreReadable( count ).Add( ")" );
            }
            public override void OnUpdate() { }
        }

        #region tQuestionMarkInfo
        public class tQuestionMarkInfo : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "[多人游戏问题？]" );
            }
            public override void HandleMouseover()
            {
                string message;
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                if ( frame == null )
                    message = "Network framework not yet chosen.";
                else
                {
                    message = frame.ClientConnectTooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, message );
            }
        }
        #endregion

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "关闭" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        #region GetConnectionOptionForController
        public static ArcenNetworkConnectionOption GetConnectionOptionForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= ArcenNetworkAuthority.PotentialServerOptions.Count )
                return null;
            return ArcenNetworkAuthority.PotentialServerOptions[tableIndex];
        }
        #endregion

        #region tServerOption
        public class tServerOption : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenNetworkConnectionOption connection = GetConnectionOptionForController( this );
                if ( connection == null )
                    buffer.Add( "null" );
                else
                {
                    buffer.StartColor( connection.GetTextColor() );
                    buffer.Add( connection.GetNameText() );
                }
            }

            public override void HandleMouseover()
            {
                string message;
                ArcenNetworkConnectionOption connection = GetConnectionOptionForController( this );
                if ( connection == null )
                    message = "null";
                else
                {
                    message = connection.GetTooltipText();
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, message );
            }
        }
        #endregion

        public class bConnect : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                ArcenNetworkConnectionOption connection = GetConnectionOptionForController( this );
                if ( connection == null )
                    Buffer.Add( "ERROR" );
                else if ( !connection.GetCanAttemptConnection() )
                {
                    Buffer.StartColor( "777777" );
                    Buffer.Add( "Closed" );
                }
                else
                {
                    Buffer.StartColor( connection.GetTextColor() );
                    Buffer.Add( "连接" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenNetworkConnectionOption connection = GetConnectionOptionForController( this );
                if ( connection == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Could not connnect to a null connection!", Verbosity.ShowAsError );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                else if ( !connection.GetCanAttemptConnection() )
                {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                Instance.Close();

                LastHostChosen = connection.GetNameText();
                ArcenNetworkAuthority.ActiveSocket.ConnectAsClient( connection.GetUniqueIDAsStringToConnectTo() );
                
                Window_ClientMultiplayerConnectionStatus.Instance.Open();
                return MouseHandlingResult.None;
            }


            public override void HandleMouseover()
            {
                string message;
                ArcenNetworkConnectionOption connection = GetConnectionOptionForController( this );
                if ( connection == null )
                    message = "null";
                else
                {
                    message = connection.GetTooltipText();
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, message );
            }
            public override void OnUpdate() { }
        }

        public class bRefresh : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "刷新" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( ArcenNetworkAuthority.ActiveSocket != null )
                    ArcenNetworkAuthority.ActiveSocket.RefreshAndSortNetworkConnectionOptions();
                return MouseHandlingResult.None;
            }
        }

        public class bFilter : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "显示：" ).Add( ArcenNetworkAuthority.ActiveSocket.GetNetworkConnectionOptionName( ArcenNetworkAuthority.ActiveSocket.GetCurrentNetworkConnectionOptionIndex() ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenNetworkAuthority.ActiveSocket.IncrementCurrentNetworkConnectionOptionIndex();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public override void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            base.Handle( Int1, InputActionType );
            //switch ( InputActionType.InternalName )
            //{
            //    case "OpenSystemMenu":
            //        this.Close();
            //        //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
            //        ArcenInput.BlockForAJustPartOfOneSecond();
            //        break;
            //}
        }
    }
}
