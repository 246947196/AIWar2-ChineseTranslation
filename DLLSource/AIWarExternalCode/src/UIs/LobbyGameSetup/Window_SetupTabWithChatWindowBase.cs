using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.Linq;

namespace Arcen.AIW2.External
{
    public abstract class Window_SetupTabWithChatWindowBase : WindowControllerAbstractBase
    {
        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SetupTopTabs.Instance == null )
                return false;
            if ( !Window_SetupTopTabs.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            return true;
        }
        #endregion

        #region CalculateIsMultiplayer
        public static bool CalculateIsMultiplayer()
        {
            return ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.SinglePlayerOnly;
        }
        #endregion

        #region iChatTextbox_Base
        public abstract class iChatTextbox_Base : InputAbstractBase
        {
            public int maxTextLength = 1000;
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( addedChar == '\n' || addedChar == '\r' )
                    return '\0';
                if ( input.Length >= this.maxTextLength )
                    return '\0';

                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( ArcenSerializationBuffer.CharMapping.Contains( addedChar ) ) //block everything except alphanumerics allowed chars
                    return addedChar;
                return addedChar;
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        //clear the text from the textbox to prevent players from spamming the chat on accident
                        ClearTextbox();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        DoSend();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
                if ( InputActionTypeDataTable.GetActionByName_FairlySlow( "Return" ).CalculateIsSinglePress() )
                {
                    DoSend();
                }
            }

            public abstract void DoSend();

            public abstract void ClearTextbox();

            protected void DoSend_Inner( iChatTextbox_Base Textbox )
            {
                if ( Textbox == null )
                    return;
                string textOfMessage = Textbox.GetText();
                if ( textOfMessage == null || textOfMessage.Length <= 0 )
                {
                    ((ArcenUI_Input)Textbox.Element).Focus(); //but still re-focus or it winds up feeling strange if you hit enter many times.
                    return; // If no text is in the box, do nothing
                }

                {
                    //send a chat message to other players, to show on the sidebar and in the chat log
                    //also put the typing players username in front of their text, with their color.
                    string colorHex = PlayerAccount.Local.GetFactionCenterColor().ColorHexBrighter;
                    Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( localFactionOrNull != null )
                        colorHex = localFactionOrNull.FactionCenterColor.ColorHexBrighter;
                    ArcenNetworkAuthority.SendLowLevelChatMessage( "<color=#" + colorHex + ">" +
                        PlayerAccount.Local.Username + "：</color> " + textOfMessage, false, 0, true, -1, -1 );
                }

                //Finally, clear the text from the textbox to prevent players from spamming the chat on accident
                Textbox.SetText( string.Empty );

                ((ArcenUI_Input)Textbox.Element).Focus();
            }

            public override bool GetShouldBeHidden()
            {
                //only show during multiplayer
                return !Window_SetupTabWithChatWindowBase.CalculateIsMultiplayer();
            }
        }
        #endregion

        #region btnSendChat_Base
        public class btnSendChat_Base : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "发送" );
            }

            public override bool GetShouldBeHidden()
            {
                //only show during multiplayer
                return !Window_SetupTabWithChatWindowBase.CalculateIsMultiplayer();
            }
        }
        #endregion

        #region tChatHeaderText_Base
        public class tChatHeaderText_Base : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Window_SetupTabWithChatWindowBase.CalculateIsMultiplayer() )
                    Buffer.Add( "多人游戏信息与聊天" );
                else
                    Buffer.Add( "日志" );
            }
        }
        #endregion

        #region tChatText_Base
        public class tChatText_Base : TextAbstractBase
        {
            public bool hasHadAnyNewEntries = false;
            public int LastEntryCount = 0;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                //draw the actual chat log
                try
                {
                    //Do NOT use World_AIW2.Instance.ChatLog, because that is based off of game seconds and meant for durig the game

                    List<MomentaryChatItem> logEntries = Engine_Universal.LocalMomentaryDisplayLog;
                    if ( logEntries.Count > 0 )
                    {
                        while ( logEntries.Count > 500 )
                        {
                            logEntries.RemoveAt( 0 ); //only have 100 at a time, so get rid of the older ones
                            this.hasHadAnyNewEntries = true;
                        }

                        for ( int i = 0; i < logEntries.Count; i++ )
                        {
                            MomentaryChatItem chat = logEntries[i];
                            Buffer.StartColor( ColorMath.Gray ).Add( chat.ShortTimeString ).Add( "：" ).EndColor();
                            Buffer.Add( chat.Text ).Add( "\n" );
                        }
                        Buffer.Add( "\n" );
                        if ( logEntries.Count != this.LastEntryCount )
                        {
                            this.hasHadAnyNewEntries = true;
                            this.LastEntryCount = logEntries.Count;
                        }
                    }
                }
                catch ( Exception )
                {}
            }

            public override void OnUpdate() 
            {
                /* 
                 * This is what we will do if a message came through
                 * We will start the Coroutine first. This will do nothing until the next frame
                 * At the next frame, it will drop the scroll bar to the very bottom of the log
                 * You have to wait for the next frame if you don't want it to display oddly
                 * Otherwise, Unity scroll秒后 it adds the text, so you end up not going where you want 
                 * We then set the number of the last displayed entry to whatever it is now
                 */
                if ( this.hasHadAnyNewEntries )
                {
                    this.Element.TryScrollToBottom();
                    this.hasHadAnyNewEntries = false;
                }
            }
        }
        #endregion

        #region tPlayerInfoText_Base
        public class tPlayerInfoText_Base : TextAbstractBase
        {
            private static readonly Dictionary<string, bool> handledNames = Dictionary<string, bool>.Create_WillNeverBeGCed( 500, "Window_SetupTabWithChatWindowBase-tPlayerInfoText_Base-handledNames" );

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                handledNames.Clear();
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                {
                    int clientCount = ArcenNetworkAuthority.ClientConnections.Count;

                    Buffer.Add( "正在主持多人游戏（" ).Add( ArcenNetworkAuthority.ActiveSocket.DisplayName ).Add( "）\n" );
                    if ( ArcenNetworkAuthority.ActiveSocket.AreConnectionsByIPAddress() &&
                        !GameSettings.Current.GetBoolBySetting( "HideIPAddressInLobbyAndEscMenu" ) )
                    {
                        Buffer.Add( "提供给客户端的公网IP：\n" ).StartColor( "ff974b" ).Add( ArcenNetworkAuthority.GetMyPublicIPAddress() ).EndColor().Add( "\n" );
                        List<string> localIPs = ArcenNetworkAuthority.GetListOfLocalIPAddresses();
                        if ( localIPs.Count > 0 )
                        {
                            Buffer.Add( "或局域网IP选项：\n" ).StartColor( "acff4b" );
                            for ( int i = 0; i < localIPs.Count; i++ )
                            {
                                Buffer.Add( localIPs[i] ).Add( "\n" );
                            }
                            Buffer.EndColor();
                        }
                    }

                    if ( PlayerAccount.Local == null )
                        Buffer.StartColor( ColorMath.LighterRed ).Add( "未分配玩家账户！" ).EndColor().Add( "\n" );
                    else if ( World_AIW2.Instance.GetLocalPlayerFactionOrNull() == null )
                        Buffer.StartColor( ColorMath.Yellow ).Add( "您处于旁观模式。" ).EndColor().Add( "\n" );
                    Buffer.Add( "已连接客户端：" ).Add( clientCount ).Add( "\n" );
                    for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                    {
                        PlayerAccount account = World.Instance.AllPlayerAccounts[i];
                        Buffer.StartColor( account.GetFactionCenterColor().ColorHexBrighter );
                        string name = account.Username;
                        if ( name == null || name.Length <= 0 )
                            name = "未知玩家账户 " + account.PlayerPrimaryKeyID;
                        Buffer.Add( name );

                        if ( PlayerAccount.Local == account )
                            Buffer.Add( "  (主机 - 您)" );
                        else
                        {
                            if ( World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( account.PlayerPrimaryKeyID) == null )
                                Buffer.StartColor( ColorMath.Yellow ).Add( "  (旁观者)" ).EndColor();

                            bool isConnected = false;
                            for ( int j = 0; j < ArcenNetworkAuthority.ClientConnections.Count; j++ )
                            {
                                ArcenNetworkClientConnection connection = ArcenNetworkAuthority.ClientConnections[j];
                                if ( connection.ProfileName == name )
                                {
                                    Buffer.Add( "  (已连接)" );
                                    isConnected = true;
                                    break;
                                }
                            }
                            if ( !isConnected )
                                Buffer.Add( "  (离线)" );
                        }

                        Buffer.EndColor();
                        Buffer.Add( "\n" );
                        handledNames[name] = true;
                    }

                    for ( int i = 0; i < ArcenNetworkAuthority.ClientConnections.Count; i++ )
                    {
                        ArcenNetworkClientConnection connection = ArcenNetworkAuthority.ClientConnections[i];
                        if ( handledNames.ContainsKey( connection.ProfileName ) )
                            continue; //already did this one, don't show it again!

                        string name = connection.ProfileName;
                        if ( name == null || name.Length <= 0 )
                        {
                            name = "等待获取名称…";
                            Buffer.Add( name );
                        }
                        else
                        {
                            Buffer.Add( name );
                            Buffer.Add( "  (尚无玩家账户)" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                else if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    Buffer.Add( "多人游戏客户端" ).Add( "\n" );
                    if ( PlayerAccount.Local == null )
                        Buffer.StartColor( ColorMath.LighterRed ).Add( "未分配玩家账户！" ).EndColor().Add( "\n" );
                    else if ( World_AIW2.Instance.GetLocalPlayerFactionOrNull() == null )
                        Buffer.StartColor( ColorMath.Yellow ).Add( "您处于旁观模式。" ).EndColor().Add( "\n" );

                    for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                    {
                        PlayerAccount account = World.Instance.AllPlayerAccounts[i];
                        Buffer.StartColor( account.GetFactionCenterColor().ColorHexBrighter );
                        string name = account.Username;
                        if ( name == null || name.Length <= 0 )
                            name = "未知玩家账户 " + account.PlayerPrimaryKeyID;
                        Buffer.Add( name );

                        if ( account.Network_IsHost )
                        {
                            Buffer.Add( "  (主机)" );
                            if ( World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( account.PlayerPrimaryKeyID ) == null )
                                Buffer.StartColor( ColorMath.Yellow ).Add( "  (旁观者)" ).EndColor();
                        }
                        else if ( PlayerAccount.Local == account )
                            Buffer.Add( "  (您)" );
                        else
                        {
                            if ( World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( account.PlayerPrimaryKeyID ) == null )
                                Buffer.StartColor( ColorMath.Yellow ).Add( "  (旁观者)" ).EndColor();
                        }

                        Buffer.EndColor();
                        Buffer.Add( "\n" );
                        handledNames[name] = true;
                    }
                }
                //else
                //    Buffer.Add( "Single-player Game" ).Add( "\n" );
            }
        }
        #endregion
    }
}
