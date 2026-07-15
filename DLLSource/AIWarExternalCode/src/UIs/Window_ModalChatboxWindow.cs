using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.Linq;

namespace Arcen.AIW2.External
{
    public class Window_ChatboxWindow : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_ChatboxWindow Instance;
        
        public bool IsOpen = false;

        //used for recalling chat commands
        public static List<string> MostRecentChatCommands = List<string>.Create_WillNeverBeGCed( 80, "Window_ChatboxWindow-MostRecentChatCommands" );
        public static int lastChatCommandRecalled = -1;

        public Window_ChatboxWindow()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.PreventsNormalInputHandlers = false; //only prevent it if you've selected the text input box, which will happen elsewhere
        }

        public void Open()
        {
            this.IsOpen = true;
            (iChatTextbox.Instance.Element as ArcenUI_Input)?.Focus();
            
        }

        public void Close(bool unused)
        {
            this.IsOpen = false;

            if ( iChatTextbox.Instance != null )
            {
                var inputBox = iChatTextbox.Instance.Element as ArcenUI_Input;
                if ( inputBox != null )
                {
                    inputBox.UnFocus();
                    inputBox.SetText( string.Empty );
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }
            public override void OnUpdate()
            {
                if (Window_ChatboxWindow.Instance.IsOpen)
                {
                    this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                    this.WindowController.ExtraOffsetY = -Window_InGameBottomLeftMenu.Instance.Window.UISpaceHeight;
                }
            }
        }

        public void SendChat()
        {
            if ( iChatTextbox.Instance == null )
                return;
            
            string textOfMessage = iChatTextbox.Instance.GetText();
            if (string.IsNullOrWhiteSpace(textOfMessage))
                return;

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

            if ( textOfMessage.StartsWith( "cmd:" ) && localFactionOrNull != null )
            {
                if( World_AIW2.Instance.IsSimulationArtificiallyStopped )
                {
                    World_AIW2.Instance.IsSimulationArtificiallyStopped = false;
                    CheatsAndCommands.WriteCheatOrCommandResult( "Me", textOfMessage, null,
                        "已恢复模拟",
                        false, true, null );
                    return;
                }

                //send a command, either for cheating or debug or otherwise
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChatCommand], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = textOfMessage;
                command.RelatedString2 = "<color=#" + localFactionOrNull.FactionCenterColor.ColorHexBrighter + ">"
                    + PlayerAccount.Local.Username + "</color>";
                command.RelatedIntegers2.Add( PlayerAccount_AIW2.GetViewingPlanetIndexSafe() );
                command.RelatedIntegers3.Add( localFactionOrNull.FactionIndex );
                command.RelatedIntegers3.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                //if it was already there, remove the other one
                if ( MostRecentChatCommands.Contains( textOfMessage ) )
                    MostRecentChatCommands.Remove( textOfMessage );

                {
                    //insert it at the start of the list
                    MostRecentChatCommands.Insert( 0, textOfMessage );
                    //if the list is too long, cull it
                    if ( MostRecentChatCommands.Count > 50 )
                        MostRecentChatCommands.RemoveAt( MostRecentChatCommands.Count - 1 );
                    lastChatCommandRecalled = -1; //restart the list in terms of what was most recently recalled
                }
            }
            else
            {
                string colorHex = "ffed89";
                if ( localFactionOrNull != null )
                    colorHex = localFactionOrNull.FactionCenterColor.ColorHexBrighter;

                //send a chat message to other players, to show on the sidebar and in the chat log
                //also put the typing players username in front of their text, with their color.
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Chat], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "<color=#" + colorHex + ">"
                    + PlayerAccount.Local.Username + "：</color> " + textOfMessage;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }
        }

        public class btnSendChat : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.SendChat();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "发送" );
            }
        }

        public class iChatTextbox : InputAbstractBase
        {
            public int maxTextLength = 1000;
            public static iChatTextbox Instance;
            public iChatTextbox() { Instance = this; }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu":
                        Window_ChatboxWindow.Instance.Close( true );
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return":
                        Window_ChatboxWindow.Instance.SendChat();
                        Window_ChatboxWindow.Instance.Close( true );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if (ArcenInput.IsInputBlockedForAmountOfTime)
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
            public override void OnUpdate()
            {
                #region Raw Chat Remembrance Stuff
                int commandRevisitIfPossible = 0;
                if ( Input.GetKeyDown( KeyCode.UpArrow ) )
                    commandRevisitIfPossible++;
                else if ( Input.GetKeyDown( KeyCode.DownArrow ) )
                    commandRevisitIfPossible--;

                if ( commandRevisitIfPossible != 0 && MostRecentChatCommands.Count > 0 )
                {
                    lastChatCommandRecalled += commandRevisitIfPossible;
                    if ( lastChatCommandRecalled < 0 )
                    {
                        lastChatCommandRecalled = -1;
                    }
                    else if ( lastChatCommandRecalled >= MostRecentChatCommands.Count )
                    {
                        lastChatCommandRecalled = MostRecentChatCommands.Count;
                    }
                    else
                    {
                        this.SetText( MostRecentChatCommands[lastChatCommandRecalled] );
                    }
                }
                #endregion
            }
        }

        public class tChatLogText : TextAbstractBase
        {
            int chatItemCurrent = 0;

            #region HandleHyperlinkClick
            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string LinkIDString )
            {
                if ( LinkIDString == string.Empty )
                    return MouseHandlingResult.None;
                int linkID = Convert.ToInt32( LinkIDString );

                List<LongTermChatItem> logEntries = World_AIW2.Instance.ChatLog;
                int currentCount = logEntries.Count;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    LongTermChatItem entry = logEntries[i];
                    if ( entry.NonSimUniqueID == linkID )
                    {
                        if ( entry.ChatClickHandlerOrNull == null )
                        {}
                        else
                            entry.ChatClickHandlerOrNull.DoOnClick( Input );
                        return MouseHandlingResult.None;
                    }
                }
                return MouseHandlingResult.None;
            }
            #endregion

            #region HandleHyperlinkHover
            private static readonly ArcenDoubleCharacterBuffer linkTooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ChatboxWindow-tChatLogText-linkTooltipBuffer" );

            public override void HandleHyperlinkHover( string LinkIDString )
            {
                if ( LinkIDString == string.Empty )
                    return;
                int linkID = Convert.ToInt32( LinkIDString );

                List<LongTermChatItem> logEntries = World_AIW2.Instance.ChatLog;
                int currentCount = logEntries.Count;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    LongTermChatItem entry = logEntries[i];
                    if ( entry.NonSimUniqueID == linkID )
                    {
                        if ( entry.ChatClickHandlerOrNull == null )
                        {}
                        else
                        {
                            entry.ChatClickHandlerOrNull.DoOnTooltip( linkTooltipBuffer );
                            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, linkTooltipBuffer.GetStringAndResetForNextUpdate() );
                        }
                        return;
                    }
                }
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                ArcenUI_Text myText = (this.Element as ArcenUI_Text);
                myText.SupportsHyperlinks = true;

                this.SetSkipGetTextFor( 0.3f );

                try
                {
                    if ( World_AIW2.Instance.ChatLog.Count > 0 )
                    {
                        for ( int i = 0; i < World_AIW2.Instance.ChatLog.Count; i++ )
                        {
                            LongTermChatItem chatItem = World_AIW2.Instance.ChatLog[i];
                            int gameSeconds = chatItem.GameSecondLogged;
                            if ( gameSeconds < 0 )
                                gameSeconds = 0;
                            Buffer.Add( "<link=" ).Add( chatItem.NonSimUniqueID );
                            Buffer.Add( ">" );
                            Buffer.Add( "<u>" ).AddHoursAndMinutes( gameSeconds )
                                .Add( "</u>     " );
                            Buffer.Add( chatItem.Text );
                            Buffer.Add( "</link>\n" );
                        }
                        Buffer.Add( "\n" );
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Error fetching chat entry. Last Entry count: " + chatItemCurrent + " " + e, Verbosity.ShowAsError );
                }

                if ( chatItemCurrent != World_AIW2.Instance.ChatLog.Count )
                {
                    this.Element.TryScrollToBottom();
                    chatItemCurrent = World_AIW2.Instance.ChatLog.Count;
                }
            }
            private System.Collections.IEnumerator ScrollToBottom()
            {
                yield return new WaitForEndOfFrame();
                this.Element.RelevantRect.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 0;
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( !this.GetShouldDrawThisFrame() )
                return;

            switch ( InputActionType.InternalName )
            {
                case "Return":
                    this.SendChat();
                    this.Close( true );
                    break;
                case "OpenSystemMenu":
                    this.Close( true );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }
}
