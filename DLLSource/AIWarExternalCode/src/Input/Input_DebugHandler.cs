using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Threading;

namespace Arcen.AIW2.External
{
    public partial class Input_DebugHandler : BaseInputHandler
    {
        private static InputActionTypeData inputTurbo50xSpeedWhileHeld = null;
        private static InputActionTypeData inputTurbo100xSpeedWhileHeld = null;
        private static FInt backupSpeed50x = FInt.Zero;
        private static FInt backupSpeed100x = FInt.Zero;

        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            string InputActionInternalName = InputActionType.InternalName;

            //ArcenDebugging.ArcenDebugLogNoDateOrAnything(string.Format("Input_DebugHandler.HandleInner( Int1={0}, InputActionType={1} )", Int1, InputActionType), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);


            switch ( InputActionInternalName )
            {
                case "Debug_ToggleHideGUI":
                    ArcenUI.Instance.InHideGUIMode = !ArcenUI.Instance.InHideGUIMode;
                    //UnityEngine.Debug.Log( "Toggle GUI" );
                    break;
                case "Debug_ReloadSelectXmlData":
                    Engine_AIW2.Instance.ReloadSelectDataFromXml();
                    World_AIW2.Instance.QueueChatMessageOrCommand( "调试：重新加载选择的XML数据。", ChatType.ShowLocallyOnly, null );
                    break;
                case "Debug_RebootAIW2":
                    {
                        ModalClickHandler Reboot = 
                            ()=>
                                {
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "调试：重启 AIW2。", ChatType.ShowLocallyOnly, null );
                                    Engine_AIW2.Instance.QuitGameAndGoBackToMainMenu();
                                    Engine_Universal.ReloadXmlDataAsMuchAsIsAllowed();
                                };
                        ModalPopupData.CreateAndLogYesNoStyle( Reboot, null, 
                            "重启 AIW2？", 
                            "\n这将关闭所有内容，返回主菜单，然后重新加载所有支持的内容。这与切换模组开关时发生的过程相同。\n\n你想要继续吗？\n", 
                            "是", "否" );
                        break;
                    }
                case "Debug_ToggleHideGimbals":
                    Engine_AIW2.Instance.InHideGimbalMode = !Engine_AIW2.Instance.InHideGimbalMode;
                    //UnityEngine.Debug.Log( "Toggle Gimbals" );
                    break;
                case "Debug_StartTestChamber":
                    {
                        TestChamberTable.Instance.ReloadSelectData();

                        if ( !Engine_AIW2.Instance.IsTestChamber )
                        {
                            if ( World.Instance.IsLoaded )
                                return;
                        }
                        //UnityEngine.Debug.Log( "try start!" );
                        Engine_Universal.ClearAllTraceOfExistingGame();

                        World.Instance.AllPlayerAccounts.Clear();
                        PlayerAccount playerAccount = PlayerAccount.CreateBrandNew_AndAddToListOfAccounts( PlayerProfile.Local.DisplayName, 
                            PlayerProfile.Local.GameSpecificSubObject.GetFactionCenterColorLookupName(),
                            PlayerProfile.Local.GameSpecificSubObject.GetFactionTrimColorLookupName(), true );
                        PlayerAccount.SetLocalPlayerAccount( playerAccount );

                        World_AIW2.Instance.Setup.Clear();
                        World_AIW2.Instance.Setup.ScenarioDoNotCallDirectly = ScenarioDataTable.Instance.GetRowByName( "TestChamber" );
                        World_AIW2.Instance.Setup.SetUpDefaultFactionConfigurations( false );
                        World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;

                        World_AIW2.Instance.AssignPlayersToFactionsAfterClearingAllPlayerFactionLinks( false, true, true );

                        Engine_AIW2.Instance.GenerateNewWorldThatIsBlankForLobbyOrIsPopulatedByTutorialOrTestChamber( World_AIW2.Instance.Setup.ScenarioDoNotCallDirectly, null, StartWorldSource1.StartTestChamber, StartWorldSource2.NotLoadingAnything );
                    }
                    break;
                case "Debug_InstantStopSim":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.StopSim;
                    break;
                case "Debug_InstantStopSimAndVisualUpdates":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.StopSimAndVisualUpdates;
                    break;
                case "Debug_ResumeFromInstantStop":
                    Engine_Universal.DebugTimeMode = DebugTimeMode.Normal;
                    break;
                case "Debug_RunExactlyNMoreSimSteps":
                    Engine_Universal.DebugSimStepsToRun = 1;
                    Engine_Universal.DebugTimeMode = DebugTimeMode.Normal;
                    break;
                case "Debug_RunNMoreVisualUpdateSeconds":
                    Engine_Universal.DebugVisualUpdateSecondsToRun = GameSettings.Current.DebugVisualUpdateSecondsIntervalLength;
                    switch ( Engine_Universal.DebugTimeMode )
                    {
                        case DebugTimeMode.StopSimAndVisualUpdates:
                            Engine_Universal.DebugTimeMode = DebugTimeMode.StopSim; 
                            break;
                    }
                    break;
                case "Debug_DumpSquadData":
                    {
                        if ( !World.Instance.IsPaused )
                        {
                            GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.PauseOnly], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "Input-Debug_DumpSquadData-buffer", 10f );
                        int count = 0;
                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
                        {
                            count++;
                            if ( selected.InstancedRenderer != null )
                                selected.InstancedRenderer.WriteDebugDataTo( buffer );
                        }
                        string dataToShow = "小队数据转储，共 " + count + " 个实体：" + buffer.ToStringAndReturnToPool();
                        string folderName = Engine_Universal.CurrentPlayerDataDirectory + "SquadDataExports/";
                        if ( !System.IO.Directory.Exists( folderName ) )
                            System.IO.Directory.CreateDirectory( folderName );
                        string fileName = folderName + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt";
                        System.IO.File.AppendAllText( fileName, dataToShow );

                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "小队数据", dataToShow, "确定" );
                    }
                    break;
                case "Debug_DoCoreDump":
                    {
                        if ( !World.Instance.IsPaused )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.PauseOnly], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        string folderName = Engine_Universal.CurrentPlayerDataDirectory + "CoreDumps/";
                        if ( !System.IO.Directory.Exists( folderName ) )
                            System.IO.Directory.CreateDirectory( folderName );
                        string fileName = folderName + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second;
                        if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.ModalMenu )
                            fileName += "_ingame";
                        else
                            fileName += "_mainmenu";
                        fileName += "-" + World.NumberCreatedOrPoolRequested + "-loads";
                        fileName += ".txt";


                        using ( System.IO.FileStream diskStream = new System.IO.FileStream( fileName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite ) )
                        {
                            using ( System.IO.StreamWriter diskWriter = new System.IO.StreamWriter( diskStream ) )
                            {
                                if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.ModalMenu )
                                    diskWriter.WriteLine( "当前在游戏中" );
                                else
                                    diskWriter.WriteLine( "当前在主菜单" );

                                diskWriter.Write( "World Loads: " );
                                diskWriter.WriteLine( World.NumberCreatedOrPoolRequested.ToString( "#,##0" ) );
                                diskWriter.Write( "Date: " );
                                diskWriter.WriteLine( DateTime.Now.ToLongDateString() );
                                diskWriter.Write( "Time: " );
                                diskWriter.WriteLine( DateTime.Now.ToLongTimeString() );
                                diskWriter.WriteLine();
                                ArcenExternalTypeManager.DumpAllData( diskWriter );
                            }
                        }

                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "外部层级数据已转储！", "以下数据已转储到文件：\n" + fileName, "确定" );
                    }
                    break;
                case "Debug_DumpInfoFromUI":
                    {
                        //if ( !World.Instance.IsPaused )
                        //{
                        //    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TogglePause], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        //    World_AIW2.Instance.QueueGameCommand( command, true );
                        //}
                        ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "Input-Debug_DumpInfoFromUI-buffer", 10f );
                        buffer.Add( "Active Windows:\n" );
                        ArcenUI_Window win;
                        for ( int j = 0; j < ArcenUI.Instance.WindowsAll.Count; j++ )
                        {
                            win = ArcenUI.Instance.WindowsAll[j];
                            win.DumpWindowInfo( buffer );
                        }
                        string dataToShow = buffer.ToStringAndReturnToPool();
                        ArcenDebugging.ArcenDebugLogSingleLine( dataToShow, Verbosity.DoNotShow );
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "UI 数据", dataToShow, "确定" );
                    }
                    break;
                case "LogAllThreads":
                    ArcenDebugging.ArcenDebugLogSingleLine( "正在记录所有当前线程...", Verbosity.Chat );
                    try
                    {
                        ArcenThreading.ReportStacksOfAllThreads();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( e );
                    }
                    break;
                case "AbortAllThreads":
                    ArcenDebugging.ArcenDebugLogSingleLine( "正在中止所有当前线程...", Verbosity.Chat );
                    try
                    {
                        ArcenThreading.AbortAllThreads();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( e );
                    }
                    break;
                case "IncreaseSpeed10x":
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 10 );
                    break;
                case "DecreaseSpeed10x":
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, -10 );
                    break;
                case "DebugAction1":
                    LOG.Chat("DebugAction1: {0}", InputActionType.CalculateValue() );
                    GenericCommand1(InputActionType.CalculateValue());
                    break;
                case "DebugAction2":
                    LOG.Chat("DebugAction2: {0}", InputActionType.CalculateValue() );
                    GenericCommand2(InputActionType.CalculateValue());
                    break;
                case "DebugAction3":
                    LOG.Chat("DebugAction3: {0}", InputActionType.CalculateValue() );
                    GenericCommand3(InputActionType.CalculateValue());
                    break;
                case "DebugAction4":
                    LOG.Chat("DebugAction4: {0}", InputActionType.CalculateValue() );
                    GenericCommand4(InputActionType.CalculateValue());
                    break;
                case "ToggleExperimentalTooltips":
                {
                    var setting = GameSettings.Current.GetSetting("UseTooltipWriter");
                    var val = GameSettings.Current.GetIntBySetting(setting);
                    val = (val + 1)%3;
                    GameSettings.Current.SetIntBySetting(setting, val);
                    
                    LOG.Chat("UseTooltipWriter: {0}", ((WriterToUse)val).ToString().ToUpper());
                    
                    break;
                }
                case "DumpNextTooltip":
                {
                    if (InputManager.mState.Controls[(int)ArcenInputCode.LeftShift].CurrentData.IsDownNow)
                    {
                        LOG.Chat("DumpNextTooltip [remove empty]: Triggered");
                        EntityText.DumpNextTooltipText = EntityText.DebugAction.Dump_Truncate;    
                    }
                    else
                    {
                        EntityText.DumpNextTooltipText = EntityText.DebugAction.Dump;
                        LOG.Chat("DumpNextTooltip: Triggered");
                    }
                    
                    break;
                }
                case "ToggleDebugTooltips":
                {
                    var setting = GameSettings.Current.GetSetting("Debug_Tooltip_TextRegions");
                    var val = GameSettings.Current.GetBoolBySetting(setting);
                    val = !val;
                    GameSettings.Current.SetBoolBySetting(setting, val);
                    
                    LOG.Chat("Debug_Tooltip_TextRegions: {0}", val.ToString().ToUpper());
                    
                    break;
                }
                case "IncreaseTooltipScale":
                {
                    var setting_name = EntityText.GetTooltipScaleCurrentlyShown();
                    if (string.IsNullOrEmpty(setting_name))
                    {
                        LOG.Chat("IncreaseTooltipScale: [null] setting name for tooltip scale!");
                        break;
                    }
                    
                    var setting = GameSettings.Current.GetSetting( setting_name );
                    if (setting == null)
                    {
                        LOG.Chat("IncreaseTooltipScale: [null] setting named '{0}'!", setting_name);
                        break;
                    }
                    
                    var val = GameSettings.Current.GetFloatBySetting(setting);
                    val = val + setting.RoundingType.ToIncrement();
                    if (val > setting.MaxFloatValue)
                        val = setting.MaxFloatValue;
                    if (val < setting.MinFloatValue)
                        val = setting.MinFloatValue;
                    GameSettings.Current.SetFloatBySetting(setting, val);
                    
                    LOG.Chat("{0}: {1}", setting.GetDisplayName(), val );
                    
                    break;
                }
                case "DecreaseTooltipScale":
                {
                    var setting_name = EntityText.GetTooltipScaleCurrentlyShown();
                    if (string.IsNullOrEmpty(setting_name))
                    {
                        LOG.Chat("DecreaseTooltipScale: [null] setting name for tooltip scale!");
                        break;
                    }
                    
                    var setting = GameSettings.Current.GetSetting( setting_name );
                    if (setting == null)
                    {
                        LOG.Chat("DecreaseTooltipScale: [null] setting named '{0}'!", setting_name);
                        break;
                    }
                    
                    var val = GameSettings.Current.GetFloatBySetting(setting);
                    val = val - setting.RoundingType.ToIncrement();
                    if (val > setting.MaxFloatValue)
                        val = setting.MaxFloatValue;
                    if (val < setting.MinFloatValue)
                        val = setting.MinFloatValue;
                    GameSettings.Current.SetFloatBySetting(setting, val);
                    
                    LOG.Chat("{0}: {1}", setting.GetDisplayName(), val );
                    
                    break;
                }
            }

            UpdateTurboHeld();
        }

        public void UpdateTurboHeld()
        {
            if ( inputTurbo50xSpeedWhileHeld == null )
                inputTurbo50xSpeedWhileHeld = InputActionTypeDataTable.GetActionByName_FairlySlow( "Turbo50xSpeedWhileHeld" );
            if ( inputTurbo100xSpeedWhileHeld == null )
                inputTurbo100xSpeedWhileHeld = InputActionTypeDataTable.GetActionByName_FairlySlow( "Turbo100xSpeedWhileHeld" );

            if (inputTurbo50xSpeedWhileHeld.CalculateIsKeyDownNow_IgnoreConflicts())
            {
                if (backupSpeed50x == FInt.Zero)
                {
                    backupSpeed50x = World_AIW2.Instance.FrameSizeMultiplier;
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 50 );
                }
            }
            else
            {
                if (backupSpeed50x != FInt.Zero)
                {
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, -50 );
                    backupSpeed50x = FInt.Zero;
                }
            }

            if (inputTurbo100xSpeedWhileHeld.CalculateIsKeyDownNow_IgnoreConflicts())
            {
                if (backupSpeed100x == FInt.Zero)
                {
                    backupSpeed100x = World_AIW2.Instance.FrameSizeMultiplier;
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 100 );
                }
            }
            else
            {
                if (backupSpeed100x != FInt.Zero)
                {
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, -100 );
                    backupSpeed100x = FInt.Zero;
                }
            }
        }

        partial void GenericCommand1(float input);
        partial void GenericCommand2(float input);
        partial void GenericCommand3(float input);
        partial void GenericCommand4(float input);
    }
}
