using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcen.AIW2.External
{
    public class Window_ClientMultiplayerConnectionStatus : ToggleableWindowController, IInputActionHandler
    {
        public static Window_ClientMultiplayerConnectionStatus Instance;
        public Window_ClientMultiplayerConnectionStatus()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;

            for ( ClientConnectionStage i = ClientConnectionStage.None + 1; i < ClientConnectionStage.Length; i++ )
                this.TimeTrackersByStage[i] = new ConnectionStageTimeTracker(i.ToString());
        }

        public ThreadingExchanger AwaitingResultOfInitialConnectionThread = new ThreadingExchanger( "Window_ClientMultiplayerConnectionStatus-AwaitingResultOfInitialConnectionThread", 300f );
        private readonly EnumIndexedArray<ClientConnectionStage,ConnectionStageTimeTracker> TimeTrackersByStage = 
            EnumIndexedArray<ClientConnectionStage,ConnectionStageTimeTracker>.Create_WillNeverBeGCed( false, null, "Window_ClientMultiplayerConnectionStatus-TimeTrackersByStage" );

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
                    message = "尚未选择网络框架。";
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
                Instance.DoCancel();
                return MouseHandlingResult.None;
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "正在连接..." );
            }
            public override void OnUpdate() { }
        }

        public class tExtraInfoText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile(ArcenDoubleCharacterBuffer Buffer)
            {
                ClientConnectionStage connectionStage = ArcenNetworkAuthority.ClientStage;
                switch ( connectionStage )
                {
                    case ClientConnectionStage.None:
                    case ClientConnectionStage.Length:
                    case ClientConnectionStage.Disconnected:
                        return;
                }

                for ( ClientConnectionStage i = ClientConnectionStage.None + 1; i < ClientConnectionStage.Length; i++ )
                {
                    if ( i == connectionStage )
                        Instance.TimeTrackersByStage[i].SetStartIfNotAlreadySet();
                    else if ( i < connectionStage )
                        Instance.TimeTrackersByStage[i].SetEndIfNotAlreadySet();
                }

                if ( ArcenNetworkAuthority.ActiveSocket.AreConnectionsByIPAddress() )
                    Buffer.Add( "正在连接到服务器 " ).Add( Window_JoinMultiplayerGameByIPMenu.iModalTextbox.Instance.IP );
                else
                    Buffer.Add( "正在连接到 " ).Add( Window_JoinMultiplayerGameByListMenu.LastHostChosen );
                if ( connectionStage == ClientConnectionStage.Establishing_Connection )
                {
                    Buffer.Add( "\n" ).Add( "正在建立连接" );
                    Instance.TimeTrackersByStage[ClientConnectionStage.Establishing_Connection].WriteTimeStringToBuffer( Buffer );
                    return;
                }

                Buffer.Add( "\n" ).Add( "连接已建立" );

                if ( connectionStage == ClientConnectionStage.Sending_Profile_Name_Expansions_And_Mods )
                {
                    Buffer.Add( "\n" ).Add( "正在发送档案名称、扩展包和模组状态" );
                    Instance.TimeTrackersByStage[ClientConnectionStage.Sending_Profile_Name_Expansions_And_Mods].WriteTimeStringToBuffer( Buffer );
                    return;
                }
                if ( connectionStage == ClientConnectionStage.Loading_World )
                {
                    Buffer.Add( "\n" ).Add( "正在加载世界" );
                    Instance.TimeTrackersByStage[ClientConnectionStage.Loading_World].WriteTimeStringToBuffer( Buffer );
                    return;
                }
                if ( connectionStage == ClientConnectionStage.World_Fully_Loaded )
                {
                    Buffer.Add( "\n" ).Add( "世界已完全加载" );
                    Instance.TimeTrackersByStage[ClientConnectionStage.World_Fully_Loaded].WriteTimeStringToBuffer( Buffer );
                    return;
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            switch ( ArcenNetworkAuthority.ClientStage )
            {
                default:
                    return true;
                case ClientConnectionStage.None:
                case ClientConnectionStage.Disconnected:
                case ClientConnectionStage.Length:
                case ClientConnectionStage.World_Fully_Loaded:
                    this.ClearTimeTrackers();
                    return false;
            }
        }

        public override void OnOpen()
        {
            this.ClearTimeTrackers();
        }

        private void ClearTimeTrackers()
        {
            for( ClientConnectionStage i = ClientConnectionStage.None+1;i< ClientConnectionStage.Length;i++)
                this.TimeTrackersByStage[i].Clear();
        }

        public void DoCancel()
        {
            ArcenNetworkAuthority.ShutdownAndTellThemWhy();
            this.Close();
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    this.DoCancel();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }

    public class ConnectionStageTimeTracker
    {
        public readonly string DebugName = string.Empty;
        public DateTime? StartingTime;
        public DateTime? EndingTime;

        public ConnectionStageTimeTracker(string DebugName)
        {
            this.DebugName = DebugName;
        }

        public void WriteTimeStringToBuffer( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( " - " ).AddFixedDecimal( GetSecondsElapsed(), 1 ).Add( "s" );
        }

        private double GetSecondsElapsed()
        {
            double secondsElapsed = 0;
            if ( this.StartingTime != null )
            {
                DateTime endTime = this.EndingTime ?? ArcenUI.Instance.CurrentNow;
                TimeSpan time = endTime - this.StartingTime.Value;
                secondsElapsed = time.TotalSeconds;
            }

            return secondsElapsed;
        }

        public void Clear()
        {
            if ( this.StartingTime != null || this.EndingTime != null )
            {
                if ( GetSecondsElapsed() > 0.1 )
                {
                    ArcenCharacterBuffer tempBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "TimeTrackersByStage-tempBuffer" );
                    tempBuffer.Add( "Multiplayer Connection Time Taken: " ).Add( this.DebugName );
                    WriteTimeStringToBuffer( tempBuffer );
                    ArcenDebugging.ArcenDebugLogSingleLine( tempBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                }
                this.StartingTime = null;
                this.EndingTime = null;
            }
        }

        public void SetStartIfNotAlreadySet()
        {
            if ( this.StartingTime != null )
                return;
            this.StartingTime = ArcenUI.Instance.CurrentNow;
        }

        public void SetEndIfNotAlreadySet()
        {
            if ( this.EndingTime != null )
                return;
            this.EndingTime = ArcenUI.Instance.CurrentNow;
        }
    }
}
