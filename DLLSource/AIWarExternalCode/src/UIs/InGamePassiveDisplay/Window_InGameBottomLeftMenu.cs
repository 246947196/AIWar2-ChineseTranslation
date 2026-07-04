using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameBottomLeftMenu : WindowControllerAbstractBase
    {
        public static Window_InGameBottomLeftMenu Instance;
        public Window_InGameBottomLeftMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
            }
        }

        public class bSettings : StackWindowOpeningButtonController
        {
            protected override StackMenuWindowController RelatedController => Window_InGameEscapeMenu.Instance;

            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "设置与通用菜单" ); }
        }

        public class bChat : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !Window_ChatboxWindow.Instance.IsOpen )
                    Window_ChatboxWindow.Instance.Open();
                else
                    Window_ChatboxWindow.Instance.Close( true );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "打开消息日志\n<size=60%>(你也可以在这里发送聊天消息。)</size>" ); }
        }
        
        #region bTimer
        public class bTimer : ButtonAbstractBase
        {
            public static readonly ConcurrentQueue<int> MostRecentDisplayRatios = ConcurrentQueue<int>.Create_WillNeverBeGCed( "Window_InGameBottomLeftMenu-bTimer-MostRecentDisplayRatios" );
            public static int AverageOfMostRecentDisplayRatios = 0;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSkipGetTextFor( 0.3f );
                Buffer.AddHoursAndMinutes( World_AIW2.Instance.GameSecond );
                bool writeNothingMore = false;
                if ( World.Instance.IsPaused )
                {
                    Buffer.Add( "\n已暂停" );
                    writeNothingMore = true;
                }
                else
                    Buffer.Add( "\n<size=80%>" );

                string frameFrequencyString = string.Empty;
                GameSpeedType speedType = getGameSpeedType();
                if ( !speedType.IsDefault )
                    frameFrequencyString = speedType.DisplayName;

                bool wroteSomething = false;
                if ( !writeNothingMore )
                {
                    //Phase 2 adaptive frame-budget: show the player's INTENDED speed (PlayerTargetSpeedMultiplier), not the raw
                    //FrameSizeMultiplier, so the label stays stable at what the player chose even while the auto-budget rebalances
                    //the frame-size/interval split underneath. (With the feature off the two are equal, so this is unchanged.)
                    if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier > FInt.One )
                    {
                        wroteSomething = true;

                        if ( frameFrequencyString.Length > 0 )
                            Buffer.AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x " ).Add( frameFrequencyString );
                        else
                            Buffer.AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x Sp." );
                    }
                    else if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier < FInt.One )
                    {
                        wroteSomething = true;
                        if ( frameFrequencyString.Length > 0 )
                            Buffer.AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x " ).Add( frameFrequencyString );
                        else
                            Buffer.AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x Sp." );
                    }
                    else if ( frameFrequencyString.Length > 0 )
                    {
                        wroteSomething = true;
                        Buffer.Add( frameFrequencyString );
                    }
                }

                double ratioInMilliseconds = World_AIW2.Instance.GetPerformanceRatio();

                if ( ratioInMilliseconds > 0 && World_AIW2.Instance.GameSecond >= 5 )
                {
                    bool showHigherRatios = false;                        

                    {
                        double ratioInSpeed = 1f / (ratioInMilliseconds);
                        if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier > FInt.One )
                        {
                            showHigherRatios = true;
                            ratioInSpeed *= World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToDouble();
                        }

                        MostRecentDisplayRatios.Enqueue( Mathf.RoundToInt( Convert.ToSingle( ratioInSpeed * 100 ) ) );
                        while ( MostRecentDisplayRatios.Count > 3 )
                            MostRecentDisplayRatios.TryDequeue( out int unused );

                        float totalTimes = 0;
                        int totalTimesCounted = 0;
                        foreach ( long time in MostRecentDisplayRatios )
                        {
                            totalTimesCounted++;
                            totalTimes += time;
                        }

                        float averageTime = totalTimesCounted <= 0 ? 0 : ( totalTimes / totalTimesCounted);
                        AverageOfMostRecentDisplayRatios = Mathf.RoundToInt( averageTime );
                    }

                    if ( !writeNothingMore )
                    {
                        if ( AverageOfMostRecentDisplayRatios < 90 || showHigherRatios )
                        {
                            Color ratioColor;
                            if ( AverageOfMostRecentDisplayRatios < 70 )
                                ratioColor = ColorMath.LightRed;
                            else
                                ratioColor = ColorMath.LightGreen;
                            if ( wroteSomething )
                                Buffer.Add( " " );
                            Buffer.Add( "<color=#" ).Add( ratioColor.GetHexCode() ).Add( ">" ).Add( AverageOfMostRecentDisplayRatios ).Add( "%</color>" );
                        }
                    }
                }
                else
                {
                    MostRecentDisplayRatios.Clear();
                    AverageOfMostRecentDisplayRatios = 0;
                }
            }

            public GameSpeedType getGameSpeedType()
            {
                int currentGameSpeedTypeIndex = GameSpeedTypeTable.Instance.DefaultSpeedIndex + World_AIW2.Instance.GameSpeedDifferentialFromDefault;
                if ( currentGameSpeedTypeIndex < 0 )
                    currentGameSpeedTypeIndex = 0;
                if ( currentGameSpeedTypeIndex >= GameSpeedTypeTable.Instance.SortedGameSpeeds.Count )
                    currentGameSpeedTypeIndex = GameSpeedTypeTable.Instance.SortedGameSpeeds.Count;
                return GameSpeedTypeTable.Instance.SortedGameSpeeds[currentGameSpeedTypeIndex];
            }
            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameBottomLeftMenu-bTimer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Add( "游戏中已用时间：" );
                tooltipBuffer.AddHoursAndMinutes( World_AIW2.Instance.GameSecond );
                tooltipBuffer.Add( "\n" ).Add( "你可以按 " )
                    .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "TogglePause" ) )
                    .Add( " 或点击此处来切换暂停。" );

                GameSpeedType speedType = getGameSpeedType();
                if ( !speedType.IsDefault )
                    tooltipBuffer.Add( "\n" ).Add( "当前游戏速度样式：" ).Add( speedType.InternalName ).Add( " (" )
                        .AddFixedDecimal( speedType.AttackDamage.ToFloatNonSim(), 1 ).Add( "x 攻击，" )
                        .AddFixedDecimal( speedType.MoveSpeed.ToFloatNonSim(), 1 ).Add( "x 移动)" );

                if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier > FInt.One )
                    tooltipBuffer.Add( "\n" ).Add( "快进中：" ).AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x 时间" );
                else if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier < FInt.One )
                    tooltipBuffer.Add( "\n" ).Add( "慢动作：" ).AddFixedDecimal( World_AIW2.Instance.PlayerTargetSpeedMultiplier.ToFloatNonSim(), 1 ).Add( "x 时间" );

                if ( AverageOfMostRecentDisplayRatios > 0 )
                {
                    {
                        Color ratioColor;
                        if ( AverageOfMostRecentDisplayRatios < 70 )
                            ratioColor = ColorMath.LightRed;
                        else
                            ratioColor = ColorMath.LightGreen;
                        tooltipBuffer.Add( "\n模拟速度：<color=#" ).Add( ratioColor.GetHexCode() ).Add( ">" ).Add( AverageOfMostRecentDisplayRatios ).Add( "%</color>" );
                    }
                }

                tooltipBuffer.Add( "\n帧率：" ).Add( ArcenFramerateTracker.CurrentFramesPerSecond );

                tooltipBuffer.Add( "\n<color=#999999>左键点击暂停或取消暂停，右键点击查看更详细的游戏代码运行速度。</color>" );
                tooltipBuffer.Add( "\n<color=#999999>你可以用此按钮右侧的按钮来控制游戏速度。</color>" );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.LeftButtonClicked )
                    EndpointFunctions.TogglePause( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                else
                {
                    Window_ModalSelfUpdatingTextWindow_UltraWide.Instance.Open( 
                        0.25f, 0.5f, 
                        "游戏代码计时", 
                        "关闭",
                        AppendAllTimingText );
                }
                return MouseHandlingResult.None;
            }

            private bool AppendAllTimingText( ArcenDoubleCharacterBuffer Buffer )
            {
                ArcenShortTermPlanningManager.SubTimeTrackers_LastTimeAskedToTrack = ArcenTime.TimeSinceStartF;
                double ratioInMilliseconds = World_AIW2.Instance.GetPerformanceRatio();
                double ratioInSpeed = 1f / (ratioInMilliseconds);

                long lastSimStepTime = 0;
                double totalSimStepTimes = 0;
                int totalSimStepsCounted = 0;
                foreach ( long time in SimExecution.MostRecentFrameTimes )
                {
                    totalSimStepsCounted++;
                    totalSimStepTimes += time;
                    lastSimStepTime = time;
                }

                Buffer.Add( "\n最后模拟步骤时间：" ).StartColor( lastSimStepTime > 95 ? ColorMath.EnemyRed : ColorMath.White ).AddFixedDecimal( lastSimStepTime, 2 ).Add( "ms" ).EndColor();
                Buffer.Add( "<pos=300>最近模拟步骤平均时间：" );
                if ( totalSimStepsCounted == 0 )
                    Buffer.Add( "无" );
                else
                {
                    double averageTime = (totalSimStepTimes / totalSimStepsCounted);
                    Buffer.StartColor( averageTime > 95 ? ColorMath.EnemyRed : ColorMath.White ).AddFixedDecimal( (totalSimStepTimes / totalSimStepsCounted), 2 ).Add( "ms " ).EndColor();
                }
                Buffer.Add( "\n<size=80%><color=#999999>帧时间超过80毫秒可能会导致模拟减速。你可以按 + 键一次切换到1.5倍速来抵消影响，这对游戏体验的影响应该很小。</size></color>" );

                Buffer.Add( "\n最后模拟步骤：" ).AddFixedDecimal( ( ArcenTime.TimeSinceStartF - World_AIW2.Instance.OnClientOrHost_TimeStartedLastSimFrame ), 2 ).Add( "秒前" );

                //////////////////////////////////////////////////////////////////////////////

                #region Details
                Buffer.Add( "\n<size=70%>详细信息：" );
                if ( World_AIW2.Instance.InSetupPhase )
                    Buffer.Add( " SETUP_PHASE" );
                if ( World.Instance.IsPaused )
                    Buffer.Add( " WORLD_PAUSED" );
                if ( World_AIW2.Instance.IsSimulationArtificiallyStopped )
                    Buffer.Add( " SIM_STOPPED_ARTIFICIAL" );
                if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
                    Buffer.Add( " THREADING_BLOCK_FOR_SHUTDOWN" );
                if ( Mapgen.IsMapCurrentlyGenerating )
                    Buffer.Add( " MAPGEN_BLOCK" );
                if ( Engine_Universal.RunStatus != RunStatus.Running )
                    Buffer.Add( " Runstatus:" ).Add( Engine_Universal.RunStatus.ToString() );
                if ( !World.Instance.IsLoaded )
                    Buffer.Add( " WORLD_NOT_LOADED" );
                if ( Engine_Universal.IsShuttingDown )
                    Buffer.Add( " SHUTTING_DOWN" );
                if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                    Buffer.Add( " SHOULD_NOT_RUN_GAME_STYLE_LOGIC" );
                if ( CentralVars.DEBUG_TURN_OFF_MAIN_SIM_EXECUTION )
                    Buffer.Add( " DEBUG_TURN_OFF_MAIN_SIM_EXECUTION" );
                if ( !ArcenExecutionManager.GetIsSoleExecutionContextDone() )
                    Buffer.Add( " SOLE_EXECUTION_CONTEXT_NOT_DONE" );
                if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                    Buffer.Add( " OUTSIDE_NORMAL_GAMEPLAY" );
                if ( ArcenExecutionManager.SoleContext == null )
                    Buffer.Add( " EXEC_CONTEXT_NULL" );

                string reason;
                float elapsed;
                if (SimPlannerImplementation.GetDidNotRun(out reason, out elapsed))
                {
                    Buffer.Add( "<pos=500>未运行 (" ).AddFormat(elapsed, "{0:#,##0.0}").Add(") 原因：").StartColor(Color.red).Add(reason).EndColor();
                }
                else
                {
                    //..Buffer.Add( "<pos=500>Ran" );
                }
                
                Buffer.Add( "</size>" );
                #endregion Details

                //////////////////////////////////////////////////////////////////////////////

                long lIW = 0; //lastItemWas - kept here so it's a local variable that can't be messed with if there are multiple threads here
                int pX = START_X_FOR_ACCUM;

                #region Outer Sim Spots
                Buffer.Add( "\n<size=70%>外部模拟点：</size><size=60%>" );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "OUFM", Engine_Universal.OnUpdateFromMainThreadHits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "RF", Engine_Universal.RunFrameHits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "PACFOM", Engine_AIW2.ProcessArbitraryFrameOnMainThreadHits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "PCLFAF", SimPlannerImplementation.ProcessCoreLogicForArbitraryFrameOnMainThreadHits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "ATRSS1", SimPlannerImplementation.AllowedToRunSimStep1 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "ATRSS2", SimPlannerImplementation.AllowedToRunSimStep2 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "ATRSS3", SimPlannerImplementation.AllowedToRunSimStep3 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "RACs", ArcenExecutionManager.RunAllContextsHits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CoHs", SimExecution.WorldSimClientOrHost_Hits );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "HOs", SimExecution.WorldSimHostOnly_Hits );

                Buffer.Add( "</size>" );
                #endregion Outer Sim Spots

                pX = START_X_FOR_ACCUM;

                //#region S-T Sim Spots
                //Buffer.Add( "\n<size=70%>S-T Sim Spots:</size><size=60%>" );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "S-TAts", ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread_Attempts );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "S-TSts", ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread_Starts );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "S-TFis", ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread_Finishes );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "ABB", AreaBoostPlanning.Accumulator_ABB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "ABF", AreaBoostPlanning.Accumulator_ABF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "GVB", GravityPlanning.Accumulator_GVB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "GVF", GravityPlanning.Accumulator_GVF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "MFB", MetalFlowPlanning_Player.Accumulator_MFB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "MFF", MetalFlowPlanning_Player.Accumulator_MFF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "NFB", MetalFlowPlanning_NPC.Accumulator_MFB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "NFF", MetalFlowPlanning_NPC.Accumulator_MFF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "MOB", MovementPlanning.Accumulator_MOB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "MOF", MovementPlanning.Accumulator_MOF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "PRB", ProtectionPlanning.Accumulator_PRB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "PRF", ProtectionPlanning.Accumulator_PRF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "TAB", TachyonPlanning.Accumulator_TAB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "TAF", TachyonPlanning.Accumulator_TAF );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "TRB", TractorPlanning.Accumulator_TRB );
                //DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "TRF", TractorPlanning.Accumulator_TRF );

                //Buffer.Add( "</size>" );
                //#endregion S-T Sim Spots

                pX = START_X_FOR_ACCUM;

                #region Inner Sim Spots 
                Buffer.Add( "\n<size=70%>内部模拟点：</size><size=60%>" );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "BLDC", EntitySimLogicImplementation_BaseInfo.Accumulator_BLDC );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "GPLA", EntitySimLogicImplementation_BaseInfo.Accumulator_GPLA );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "WTIM", EntitySimLogicImplementation_BaseInfo.Accumulator_WTIM );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "WSEC", EntitySimLogicImplementation_BaseInfo.Accumulator_WSEC );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "FLC", EntitySimLogicImplementation_BaseInfo.Accumulator_FLC );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "WOR", EntitySimLogicImplementation_BaseInfo.Accumulator_WOR );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "PLA", EntitySimLogicImplementation_BaseInfo.Accumulator_PLA );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "COM", EntitySimLogicImplementation_BaseInfo.Accumulator_COM );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "REM", EntitySimLogicImplementation_BaseInfo.Accumulator_REM );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "SAI", EntitySimLogicImplementation_BaseInfo.Accumulator_SAI );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "WSUF", EntitySimLogicImplementation_BaseInfo.Accumulator_WSUF );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "BLDF", EntitySimLogicImplementation_BaseInfo.Accumulator_BLDF );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "NET", EntitySimLogicImplementation_BaseInfo.Accumulator_NET );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "FAC", EntitySimLogicImplementation_BaseInfo.Accumulator_FAC );

                Buffer.Add( "</size>" );
                #endregion Inner Sim Spots

                pX = START_X_FOR_ACCUM;

                #region Inner Combat 
                Buffer.Add( "\n<size=70%>内部战斗：</size><size=60%>" );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "CSPB", EntitySimLogicImplementation_BaseInfo.Accumulator_CSPB );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CSPF", EntitySimLogicImplementation_BaseInfo.Accumulator_CSPF );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CCSB", EntitySimLogicImplementation_BaseInfo.Accumulator_CCSB );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CCSF", EntitySimLogicImplementation_BaseInfo.Accumulator_CCSF );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CSIB", EntitySimLogicImplementation_BaseInfo.Accumulator_CSIB );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CSIF", EntitySimLogicImplementation_BaseInfo.Accumulator_CSIF );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CSOB", EntitySimLogicImplementation_BaseInfo.Accumulator_CSOB );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CSOF", EntitySimLogicImplementation_BaseInfo.Accumulator_CSOF );

                Buffer.Add( "</size>" );
                #endregion Inner Combat

                pX = START_X_FOR_ACCUM;

                #region Custom Set
                Buffer.Add( "\n<size=70%>自定义集合：</size><size=60%>" );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "CUS1", Engine_Universal.Accumulator_CUS1 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS2", Engine_Universal.Accumulator_CUS2 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS3", Engine_Universal.Accumulator_CUS3 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS4", Engine_Universal.Accumulator_CUS4 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS5", Engine_Universal.Accumulator_CUS5 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS6", Engine_Universal.Accumulator_CUS6 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS7", Engine_Universal.Accumulator_CUS7 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS8", Engine_Universal.Accumulator_CUS8 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUS9", Engine_Universal.Accumulator_CUS9 );

                Buffer.Add( "</size>" );
                #endregion Custom Set

                pX = START_X_FOR_ACCUM;

                #region Custom Set B
                Buffer.Add( "\n<size=70%>自定义集合 B：</size><size=60%>" );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, false, "CUB1", Engine_Universal.Accumulator_CUB1 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB2", Engine_Universal.Accumulator_CUB2 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB3", Engine_Universal.Accumulator_CUB3 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB4", Engine_Universal.Accumulator_CUB4 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB5", Engine_Universal.Accumulator_CUB5 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB6", Engine_Universal.Accumulator_CUB6 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB7", Engine_Universal.Accumulator_CUB7 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB8", Engine_Universal.Accumulator_CUB8 );
                DrawTextForAccumulator( Buffer, ref pX, ref lIW, true, "CUB9", Engine_Universal.Accumulator_CUB9 );

                Buffer.Add( "</size>" );
                #endregion Custom Set B

                pX = START_X_FOR_ACCUM;

                //////////////////////////////////////////////////////////////////////////////

                #region Time Styles
                Buffer.Add( "\n<size=70%>时间样式：</size><size=60%>" );
                Buffer.Add( "   AT: " ).AddFixedDecimalThousands( ArcenTime.TimeSinceStartF, 1 );
                Buffer.Add( "   GenSGT: " ).AddFixedDecimalThousands( ArcenTime.NonSetupGeneralGameTimeSinceLastLoadOrStartF, 1 );
                Buffer.Add( "   NetSGT: " ).AddFixedDecimalThousands( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF, 1 );
                Buffer.Add( "   UTFVF: " ).AddFixedDecimalThousands( ArcenTime.UnpausedTimeSinceStartForVisualEffectsF, 1 );

                Buffer.Add( "</size>" );
                #endregion Time Styles

                //////////////////////////////////////////////////////////////////////////////

                //if (!string.IsNullOrEmpty(reason))
                //{
                //    Buffer
                //        .Add( "<pos=300>" ).StartColor( ColorMath.EnemyRed )
                //        .Add( "Did not run (" ).AddFormat(elapsed, "{0:#,##0.0}").Add(") reason: ").StartBold().Add(reason).EndBold().EndColor();
                //}
                //else
                //{
                //}

                if ( AverageOfMostRecentDisplayRatios > 0 )
                {
                    Color ratioColor;
                    if ( AverageOfMostRecentDisplayRatios < 70 )
                        ratioColor = ColorMath.LightRed;
                    else
                        ratioColor = ColorMath.LightGreen;
                    Buffer.Add( "\n模拟速度：" ).Add( "<color=#" ).Add( ratioColor.GetHexCode() ).Add( ">" ).Add( AverageOfMostRecentDisplayRatios ).Add( "%</color>" );
                }

                Buffer.Add( "\n帧率：" ).Add( ArcenFramerateTracker.CurrentFramesPerSecond );

                Buffer.Add( "\n");

                //Main Execution Context
                ArcenClientOrHostSimPlanningContext solContext = ArcenExecutionManager.SoleContext;
                if ( solContext != null )
                {
                    ArcenClientOrHostSimPlanningContext cont = solContext;

                    Buffer.Add( "\n<color=#aaaaaa><pos=40>" ).Add( cont.MyPermanentThreadName ).Add( "</color>: " );
                    if ( cont.TimesRun <= 0 )
                    {
                        Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未运行！</size>" ).EndColor();

                        Buffer.Add( "<pos=500>" );

                        if ( cont.TimesRunAttempted <= 0)
                            Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未尝试运行！</size>" ).EndColor();
                        else
                            Buffer.AddNumberMoreReadable( cont.TimesRunAttempted ).Add( " <size=70%>运行尝试</size>" );
                    }
                    else
                    {
                        Buffer.AddNumberMoreReadable( cont.TimesRun ).Add( " <size=70%>运行次数</size>" );

                        Buffer.Add( "<pos=500>" );

                        if ( cont.TimesRunAttempted <= 0 )
                            Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未尝试运行！</size>" ).EndColor();
                        else
                            Buffer.AddNumberMoreReadable( cont.TimesRunAttempted ).Add( " <size=70%>运行尝试</size>" );

                        if ( cont.TimesSuicided > 0 )
                            Buffer.Add( "    " ).AddNumberMoreReadable( cont.TimesSuicided ).Add( " <size=70%>中止</size>" );
                        //else
                        //{
                        //    if ( cont.SuicidesAtTime > 0 )
                        //        Buffer.Add( "    <size=70%>Would Abort At: " ).AddFixedDecimalThousands( cont.SuicidesAtTime, 1 ).Add( "s</size>" );
                        //}
                    }
                    Buffer.Add( "\n<size=60%><pos=80>" );
                    Buffer.AddFixedDecimalThousands( ( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - cont.GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective() ), 1 ).Add( "s <size=50%>距上次运行</size>" );
                    Buffer.Add( "<pos=500>" );
                    Buffer.AddNumberMoreReadable( cont.GetLastElapsedMiliseconds () ).Add( "ms <size=50%>上次运行耗时</size>" );
                    Buffer.Add( "</size>" );
                }

                //Short-Term Planning:
                for ( int i = 0; i < ArcenShortTermPlanningManager.AllContexts.Count; i++ )
                {
                    ArcenShortTermPlanningContext cont  = ArcenShortTermPlanningManager.AllContexts[i];

                    Buffer.Add( "\n<color=#aaaaaa><pos=40>" ).Add( cont.NameForLogs ).Add( "</color>: " );
                    if ( cont.TimesRun <= 0 )
                    {
                        Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未运行！</size>" ).EndColor();
                    }
                    else
                    {
                        Buffer.StartColor( cont.LastMillisecondsTaken > 20 ? ColorMath.EnemyRed : Color.white )
                            .AddNumberMoreReadable( cont.LastMillisecondsTaken ).Add( "ms <size=70%>用时</size>" ).EndColor();
                        Buffer.Add( "<pos=500>" );
                        Buffer.StartColor( cont.LargestMillisecondsTaken > 40 ? ColorMath.EnemyRed : Color.white )
                            .AddNumberMoreReadable( cont.LargestMillisecondsTaken ).Add( "ms <size=70%>最长用时</size>" ).EndColor();
                        Buffer.Add( "    <size=60%>" ).AddFixedDecimalThousands( (ArcenTime.TimeSinceStartF - cont.LastTimeRun ), 2 ).Add( "s</size> <size=50%>距上次运行</size>" );
                    }

                    foreach ( ArcenShortTermPlanningContext.SubTimeTracker tracker in cont.SubTimeTrackers )
                    {
                        if ( tracker.IgnoreIfNeverRun && tracker.TimesRun <= 0 )
                            continue;
                        Buffer.Add( "\n<size=70%><pos=40>" ).Add( tracker.SubTimeTrackerName ).Add( ": " );
                        if ( tracker.TimesRun <= 0 )
                        {
                            Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=50%>Has Not Yet Run!</size>" ).EndColor();
                        }
                        else
                        {
                            Buffer.StartColor( tracker.LastMillisecondsTaken > 20 ? ColorMath.EnemyRed : Color.white )
                                .AddNumberMoreReadable( tracker.LastMillisecondsTaken ).Add( "ms <size=50%>用时</size>" ).EndColor();
                            Buffer.Add( "<pos=500>" );
                            if ( tracker.LargestTimeTakenIsTimestampInstead )
                                Buffer.StartColor( ColorMath.Gray )
                                    .AddNumberMoreReadable( tracker.LargestMillisecondsTaken ).Add( "s (Timestamp)" ).EndColor();
                            else
                                Buffer.StartColor( tracker.LargestMillisecondsTaken > 40 ? ColorMath.EnemyRed : Color.white )
                                    .AddNumberMoreReadable( tracker.LargestMillisecondsTaken ).Add( "ms <size=50%>最长用时</size>" ).EndColor();
                        }
                        Buffer.Add( "</size>" );
                    }
                }

                //long-term-continuous contexts
                for ( int i = 0; i < ArcenVariousLongTermContextManager.AllContexts_ClientOrHost.Count; i++ )
                {
                    ArcenLongTermContinuousPlanningClientOrHostContext cont = ArcenVariousLongTermContextManager.AllContexts_ClientOrHost[i];

                    Buffer.Add( "\n<color=#aaaaaa><pos=40>" ).Add( cont.MyPermanentThreadName ).Add( "</color>: " );
                    if ( cont.TimesRun <= 0 )
                    {
                        Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未运行！</size>" ).EndColor();

                        Buffer.Add( "<pos=500>" );

                        if ( cont.TimesRunAttempted <= 0 )
                            Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未尝试运行！</size>" ).EndColor();
                        else
                            Buffer.AddNumberMoreReadable( cont.TimesRunAttempted ).Add( " <size=70%>运行尝试</size>" );
                    }
                    else
                    {
                        Buffer.AddNumberMoreReadable( cont.TimesRun ).Add( " <size=70%>运行次数</size>" );

                        Buffer.Add( "<pos=500>" );

                        if ( cont.TimesRunAttempted <= 0 )
                            Buffer.StartColor( ColorMath.EnemyRed ).Add( "<size=70%>尚未尝试运行！</size>" ).EndColor();
                        else
                            Buffer.AddNumberMoreReadable( cont.TimesRunAttempted ).Add( " <size=70%>运行尝试</size>" );
                    }

                    Buffer.Add( "\n<size=60%><pos=80>" );
                    Buffer.AddFixedDecimalThousands( ( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - cont.GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective() ), 1 ).Add( "s <size=50%>距上次运行</size>" );
                    Buffer.Add( "<pos=500>" );
                    Buffer.AddNumberMoreReadable( cont.GetLastElapsedMiliseconds () ).Add( "ms <size=50%>上次运行耗时</size>" );
                    Buffer.Add( "</size>" );
                }

                //long-term from-deepinfo contexts
                for ( int i = 0; i < ArcenVariousLongTermContextManager.AllContexts_HostContinuous.Count; i++ )
                {
                    IContextForMonitoring cont = ArcenVariousLongTermContextManager.AllContexts_HostContinuous[i];

                    Buffer.Add( "\n<color=#aaaaaa><pos=40>" ).Add( cont.NameForDisplay ).Add( "</color>: " );
                    Buffer.AddFixedDecimalThousands( (ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - cont.GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective()), 1 ).Add( "s <size=70%>距上次运行</size>" );
                    Buffer.Add( "<pos=500>" );
                    Buffer.AddNumberMoreReadable( cont.GetLastElapsedMiliseconds() ).Add( "ms <size=70%>To Run Last Time</size>" );
                }

                Buffer.Add( "\n" );

                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction fac = World_AIW2.Instance.Factions[i];

                    Buffer.Add( "\n<color=#aaaaaa>" ).Add( fac.FactionIndex ).Add( ":<pos=40>" ).Add( fac.GetDisplayName() ).Add( " LRP</color>: " )
                        .StartColor( fac.LastDoLongRangePlanningSeconds > 5 ? ColorMath.EnemyRed : Color.white )
                        .AddFixedDecimalThousands( fac.LastDoLongRangePlanningSeconds, 2 ).Add( "s <size=70%>用时</size>" ).EndColor();
                    Buffer.Add( "<pos=500>" );
                    if ( fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim == 0 )
                        Buffer.StartColor( ColorMath.EnemyRed ).Add( "尚未运行 LRP！" ).EndColor().Add( "    <size=70%>" ).Add( fac.LastDoLongRangePlanningReason ).Add( "</size>" );
                    else
                    {
                        Buffer.StartColor( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim < 30f ? 
                            Color.white : ColorMath.EnemyRed ).AddFixedDecimalThousands( (ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim), 2 )
                            .Add( "s <size=70%>距上次运行</size>" ).EndColor().Add( "    <size=70%>" ).Add( fac.LastDoLongRangePlanningReason ).Add( "</size>" );
                    }
                    Buffer.Add( "\n<pos=500><size=50%>Thread     " );
                    Buffer.Add( fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim ).Add( " Ended     " );
                    Buffer.Add( Mathf.RoundToInt( fac.LastIntermittentLongRangePlanningStartedTime_Nonsim ) ).Add( " Started     " );
                    Buffer.Add( fac.LastDoLongRangePlanningExtraInfo ).Add( " " );
                    Buffer.Add( "</size>" );

                    int x = 0;
                    DrawTextForTimings( Buffer, ref x, "Stage1", fac.PerSecondFactionTicksLastFrame_Stage1 );
                    DrawTextForTimings( Buffer, ref x, "Stage2", fac.PerSecondFactionTicksLastFrame_Stage2 );
                    DrawTextForTimings( Buffer, ref x, "Stage2A", fac.PerSecondFactionTicksLastFrame_Stage2A );
                    DrawTextForTimings( Buffer, ref x, "Stage3Base", fac.PerSecondFactionTicksLastFrame_Stage3Base );
                    DrawTextForTimings( Buffer, ref x, "Stage3Deep", fac.PerSecondFactionTicksLastFrame_Stage3Deep );
                    DrawTextForTimings( Buffer, ref x, "Stage4", fac.PerSecondFactionTicksLastFrame_Stage4 );
                    if ( x > 0 )
                        Buffer.Add( "</size>" );
                }

                {
                    Buffer.Add( "\n" );
                    Buffer.Add( "\n跨派系数据：\n" );
                    int x = 0;
                    DrawTextForTimings( Buffer, ref x, "SpeedGroups", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_SpeedGroups );
                    DrawTextForTimings( Buffer, ref x, "FacBasics", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_FactionPerSecondBasics );
                    DrawTextForTimings( Buffer, ref x, "PlanSort", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_PlanetInfluenceSort );
                    DrawTextForTimings( Buffer, ref x, "Combat", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_Combat );
                    DrawTextForTimings( Buffer, ref x, "Fleets", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_Fleets );
                    DrawTextForTimings( Buffer, ref x, "Hack-Difc", EntitySimLogicImplementation_BaseInfo.PerSecondTicksLastFrame_HackingDiff );
                    if ( x > 0 )
                        Buffer.Add( "</size>" );

                    Buffer.Add( "\n步骤数据：\n" );
                    x = 0;
                    DrawTextForTimings( Buffer, ref x, "GloPla", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_GlobalPlayer.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Ws1", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_WorldStep1.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "WorldSec", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_WorldSec.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Fleet", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_Fleet.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Scenario", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_Scenario.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Faction", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_Faction.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Ws2", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_WorldStep2.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Planet", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_Planet.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "Combat", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_Combat.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "RemoveDead", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_RemoveDead.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "ShipAI", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_ShipAILogic.LeftItem );
                    DrawTextForTimings( Buffer, ref x, "WorldSuff", EntitySimLogicImplementation_BaseInfo.PerFrameTicksLastFrame_WorldSuffix.LeftItem );

                    if ( x > 0 )
                        Buffer.Add( "</size>" );

                    Buffer.Add( "\n主线程计时：\n" );
                    x = 0;
                    DrawTextForThreadTimers( Buffer, ref x, "Entire", Engine_Universal.EntireMainThreadTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "Thrding", Engine_Universal.ThreadingCheckTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "GSEMain", Engine_Universal.GSEMainUpdateTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "Heartbt", Engine_Universal.HeartbeatTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "ProcArb", Engine_Universal.ProcessArbitraryTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "UpVis", Engine_Universal.UpdateVisualsTimer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "UI-1", Engine_Universal.UI1Timer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "UI-2", Engine_Universal.UI2Timer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "UI-3", Engine_Universal.UI3Timer.EffectiveMilliseconds );
                    DrawTextForThreadTimers( Buffer, ref x, "GSE2Upd", Engine_Universal.GSE2UpdateTimer.EffectiveMilliseconds );

                    if ( x > 0 )
                        Buffer.Add( "</size>" );

                    Buffer.Add( "\n窗口计时：\n" );
                    x = 0;

                    for ( int i = 0; i < ArcenUIWindowTable.Instance.Rows.Count; i++ )
                    {
                        ArcenUI_Window window = ArcenUIWindowTable.Instance.Rows[i];
                        if ( window.WindowTimer != null )
                        {
                            DrawTextForThreadTimers( Buffer, ref x, window.WindowAbbrev, window.WindowTimer.EffectiveMilliseconds, 25f );
                        }
                    }

                    if ( x > 0 )
                        Buffer.Add( "</size>" );
                }

                Buffer.Add( "\n" );
                Buffer.Add( "\n所有潜在线程请求类型：" );

                //all arcen potential thread requests
                for ( int i = 0; i < ArcenThreadingRequestTypeTable.Instance.Rows.Count; i++ )
                {
                    ArcenThreadingRequestType requestType = ArcenThreadingRequestTypeTable.Instance.Rows[i];

                    Buffer.Add( "\n<color=#aaaaaa><pos=40>" ).Add( requestType.InternalName ).Add( "</color>: " );
                    if ( requestType.TotalRunSoFar <= 0 )
                    {
                        Buffer.StartColor( ColorMath.Gray ).Add( "<size=70%>从未运行</size>" ).EndColor();

                        Buffer.Add( "<pos=500>" );
                    }
                    else
                    {
                        Buffer.AddNumberMoreReadable( requestType.TotalRunSoFar ).Add( " <size=70%>运行次数</size>" );

                        Buffer.Add( "<pos=500>" );

                        if ( requestType.TotalSkippedBecauseOfExistingRunning <= 0 )
                            Buffer.StartColor( ColorMath.Gray ).Add( "<size=70%>无跳过</size>" ).EndColor();
                        else
                            Buffer.AddNumberMoreReadable( requestType.TotalSkippedBecauseOfExistingRunning ).Add( " <size=70%>因已在运行而跳过</size>" );
                    }

                    Buffer.Add( "\n<size=60%><pos=80>" );
                    Buffer.AddFixedDecimalThousands( (ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - requestType.LastNonSetupNetworkReadyGameTimeSinceLastLoadOrStartFRun), 1 ).Add( "s <size=50%>距上次运行</size>" );

                    int count = 1;
                    foreach ( DateTime TimeStarted in ArcenThreading.MatchingTaskStartTimes( requestType ) )
                    {
                        Buffer.Add( "   " );
                        Buffer.Add( count );
                        Buffer.Add( " for " );
                        Buffer.AddFixedDecimal( ( DateTime.Now - TimeStarted ).TotalSeconds, 2 );
                        Buffer.Add( "s" );

                        count++;
                    }

                    Buffer.Add( "<pos=500>" );
                    Buffer.AddNumberMoreReadable( requestType.LastElapsedMS ).Add( "ms <size=50%>上次运行耗时</size>" );
                    Buffer.Add( "<pos=600>" );
                    Buffer.AddNumberMoreReadable( requestType.LongestElapsedMS ).Add( "ms <size=50%>最长运行时间</size>" );
                    Buffer.Add( "</size>" );
                }

                Buffer.Add( "\n" );
                Buffer.Add( "\n小队包装器数据：" );
                Buffer.Add( "\nClear: " ).AddNumberMoreReadable( SquadWrapperStats.Clear );
                Buffer.Add( "\nSetNull: " ).AddNumberMoreReadable( SquadWrapperStats.SetNull );
                Buffer.Add( "\nSetDirect: " ).AddNumberMoreReadable( SquadWrapperStats.SetDirect );
                Buffer.Add( "\nSetLazy: " ).AddNumberMoreReadable( SquadWrapperStats.SetLazy );
                Buffer.Add( "\nCallDictSuccess: " ).AddNumberMoreReadable( SquadWrapperStats.CallDictSuccess );
                Buffer.Add( "\nCallDictFail_ProperlyDead: " ).AddNumberMoreReadable( SquadWrapperStats.CallDictFail_ProperlyDead );
                Buffer.Add( "\nCallDictFail_NoReason: " ).AddNumberMoreReadable( SquadWrapperStats.CallDictFail_NoReason );

                Buffer.Add( "\n" );
                Buffer.Add( "\n其他静默错误数据：" );
                Buffer.Add( "\nSilentObjectiveErrorsA: " ).AddNumberMoreReadable( ObjectiveCategory.SilentObjectiveErrorsA );
                Buffer.Add( "\nSilentObjectiveErrorsB: " ).AddNumberMoreReadable( ObjectiveCategory.SilentObjectiveErrorsB );

                Buffer.Add( "\n" );
                Buffer.Add( "\n监视计数：" );
                Buffer.Add( "\n Planet: " ).AddNumberMoreReadable( World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() );
                Buffer.Add( "\n ActualObjective: " ).AddNumberMoreReadable( ActualObjective.RefTracker.TotalEverAdded );

                Buffer.Add( "\n" );

                Buffer.Add( "\n玩家账户：" ).Add( World.Instance.AllPlayerAccounts.Count );
                Buffer.Add( "\n本地玩家账户：" ).Add( PlayerAccount.Local == null ? "null" : PlayerAccount.Local.PlayerPrimaryKeyID.ToString() );
                foreach ( PlayerAccount account in World.Instance.AllPlayerAccounts )
                {
                    Buffer.Add( "\n<color=#999999>Acct:</color> " ).Add( account.PlayerPrimaryKeyID ).Add( "<pos=60>" ).Add( account.Username );
                    if ( account.Network_IsHost )
                        Buffer.Add( " <color=#ffbd5e>(Host)</color>" );
                    if ( account == PlayerAccount.Local )
                        Buffer.Add( " <color=#ffe432>(You)</color>" );
                    if ( account.CurrentConnectionIndex > 0 )
                        Buffer.Add( " <color=#32fdff>(Connection Index: " ).Add( account.CurrentConnectionIndex ).Add( ")</color>" );
                }

                Buffer.Add( "\n" );

                Buffer.Add( "\n客户端连接：" ).Add( ArcenNetworkAuthority.ClientConnections.Count );
                foreach ( ArcenNetworkClientConnection conn in ArcenNetworkAuthority.ClientConnections )
                {
                    Buffer.Add( "\n<color=#999999>Connection:</color> <color=#32fdff>" ).Add( conn.ConnectionIndex ).Add( "</color><pos=110>" ).Add( conn.ProfileName );
                    if ( conn.HasBeenSentWorldData )
                        Buffer.Add( " <color=#ffbd5e>(HasBeenSentWorldData)</color>" );
                    else
                        Buffer.Add( " <color=#ff6232>(!HasBeenSentWorldData)</color>" );
                    if ( conn.HasWorldDataRecieptBeenVerified )
                        Buffer.Add( " <color=#ffbd5e>(HasWorldDataRecieptBeenVerified)</color>" );
                    else
                        Buffer.Add( " <color=#ff6232>(!HasWorldDataRecieptBeenVerified)</color>" );
                }

                Buffer.Add( "\n" );

                Buffer.Add( "\n派系：" ).Add( World_AIW2.Instance.Factions.Count );
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    Buffer.Add( "\n<color=#999999>Fac:</color> " ).Add( fac.FactionIndex ).Add( "<pos=60>" ).Add( fac.GetDisplayName() );
                }

                return ratioInSpeed < 0.7f;
            }
        }
        #endregion

        #region bSlower and bFaster
        public class bSlower : ButtonAbstractBase
        {
            private static InputActionTypeData inputHoldToAdjustFrameFrequencyInUI = null;
            public static bool GetIsHoldToAdjustFrameFrequencyInUIPressed()
            {
                if ( inputHoldToAdjustFrameFrequencyInUI == null )
                    inputHoldToAdjustFrameFrequencyInUI = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToAdjustFrameFrequencyInUI" );
                return inputHoldToAdjustFrameFrequencyInUI.CalculateIsKeyDownNow_IgnoreConflicts();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( bSlower.GetIsHoldToAdjustFrameFrequencyInUIPressed() )
                    EndpointFunctions.LowerOrRaiseGameSpeedStyle( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, false );
                else
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, false );
                return MouseHandlingResult.None;
            }
            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameBottomLeftMenu-bSlower" );
            public override void HandleMouseover()
            {
                if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier > 1)
                    tooltipBuffer.Add( "你可以点击此处或按 " )
                        .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "DecreaseFrameSize" ) )
                        .Add( " 来让模拟运行变慢（回到正常速度）。" ).Add( "\n" );
                else
                    tooltipBuffer.Add( "你可以点击此处或按 " )
                        .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "DecreaseFrameSize" ) )
                        .Add( " 来让模拟以慢动作运行。" ).Add( "\n" );
                tooltipBuffer.Add( "如果你想保持时间流速不变，但想要更从容的战斗节奏、更少的紧迫感，请按住 " ).Add(
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldToAdjustFrameFrequencyInUI" ) ).Add( " 并点击此处，或按 " )
                    .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "DecreaseFrameFrequency" ) ).Add( "。" );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class bFaster : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( bSlower.GetIsHoldToAdjustFrameFrequencyInUIPressed() )
                    EndpointFunctions.LowerOrRaiseGameSpeedStyle( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, true );
                else
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, true );
                return MouseHandlingResult.None;
            }
            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameBottomLeftMenu-bFaster" );
            public override void HandleMouseover()
            {
                if ( World_AIW2.Instance.PlayerTargetSpeedMultiplier < 1 )
                    tooltipBuffer.Add( "你可以点击此处或按 " )
                        .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseFrameSize" ) )
                        .Add( " 来让模拟运行变快（回到正常速度）。" ).Add( "\n" );
                else
                    tooltipBuffer.Add( "你可以点击此处或按 " )
                        .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseFrameSize" ) )
                        .Add( " 来让模拟快进运行。" ).Add( "\n" );
                tooltipBuffer.Add( "\n" ).Add( "如果你想保持时间流速不变，但想要更快的战斗和飞船移动速度，请按住 " ).Add(
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldToAdjustFrameFrequencyInUI" ) ).Add( " 并点击此处，或按 " )
                    .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseFrameFrequency" ) ).Add( "。" );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        private const int START_X_FOR_ACCUM = 90;
        private const int INC_X_FOR_ACCUM = 80;

        public static void DrawTextForAccumulator( ArcenDoubleCharacterBuffer Buffer, ref int currentX, ref long lastItemWas, bool CompareThisItemToPriorItem, string Identifier, long Amount )
        {
            long numberToDraw = Amount;
            while ( numberToDraw > 100000000 )
                numberToDraw -= 100000000;
            while ( numberToDraw > 10000000 )
                numberToDraw -= 10000000;
            while ( numberToDraw > 1000000 )
                numberToDraw -= 1000000;
            while ( numberToDraw > 100000 )
                numberToDraw -= 100000;
            while ( numberToDraw > 10000 )
                numberToDraw -= 10000;

            if ( currentX >= 950 )
            {
                currentX = START_X_FOR_ACCUM;
                Buffer.Add( "\n" );
            }

            Buffer.Add( "<pos=" ).Add( currentX ).Add( ">" ).Add( Identifier ).Add( ": " ).AddNumberMoreReadable( numberToDraw );
            if ( CompareThisItemToPriorItem )
            {
                if ( Amount != lastItemWas )
                {
                    long differential = (Amount - lastItemWas);
                    if ( differential < -1 || differential > 1 )
                        Buffer.StartColor( "ff5050" ); //red
                    else
                        Buffer.StartColor( "ffa740" ); //orange

                    Buffer.Add( " (" ).AddNumberMoreReadable( differential ).Add( ")</color>" );
                }
            }

            lastItemWas = Amount;
            currentX += INC_X_FOR_ACCUM;
        }

        private const int START_X_FOR_TIMINGS = 40;
        private const int INC_X_FOR_TIMINGS = 80;

        public static void DrawTextForTimings( ArcenDoubleCharacterBuffer Buffer, ref int currentX, string Identifier, long AmountOfTicks )
        {
            //if ( AmountOfTicks < 10000 )
            //    return; //if less than 1ms, then don't draw.  There are 10k ticks per MS

            if ( currentX >= 950 || currentX <= 0 )
            {
                if ( currentX <= 0 )
                    Buffer.Add( "<size=60%>" );
                else
                    Buffer.Add( "\n" );
                currentX = START_X_FOR_TIMINGS;
            }
            float milliseconds = (AmountOfTicks / 10000f);
            if ( milliseconds > 5 )
                Buffer.Add( "<color=#fa8266>" );
            else
                Buffer.Add( "<color=#aaaaaa>" );
            Buffer.Add( "<pos=" ).Add( currentX ).Add( ">" ).Add( Identifier ).Add( ": " ).AddFixedDecimalThousands( milliseconds, 1 );
            Buffer.Add( "</color>" );

            currentX += INC_X_FOR_TIMINGS;
        }


        private const int START_X_FOR_THREAD_TIMER = 40;
        private const int INC_X_FOR_THREAD_TIMER = 80;

        public static void DrawTextForThreadTimers( ArcenDoubleCharacterBuffer Buffer, ref int currentX, string Identifier, float EffectiveMilliseconds, float warnAfter = 100f )
        {
            if ( currentX >= 950 || currentX <= 0 )
            {
                if ( currentX <= 0 )
                    Buffer.Add( "<size=60%>" );
                else
                    Buffer.Add( "\n" );
                currentX = START_X_FOR_THREAD_TIMER;
            }
            if ( EffectiveMilliseconds > warnAfter )
                Buffer.Add( "<color=#fa8266>" );
            else
                Buffer.Add( "<color=#aaaaaa>" );
            Buffer.Add( "<pos=" ).Add( currentX ).Add( ">" ).Add( Identifier ).Add( ": " ).AddFixedDecimalThousands( EffectiveMilliseconds, 1 );
            Buffer.Add( "</color>" );

            currentX += INC_X_FOR_THREAD_TIMER;
        }

        #region Fleets
        public class tControlGroupBase : ButtonAbstractBase
        {
            public static tControlGroupBase[] ControlGroupButtons = new tControlGroupBase[11];

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //do something
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "即将推出！" ); }
            public override bool GetShouldBeHidden()
            {
                return true;// Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView;
            }
        }

        public class tControlGroup1 : tControlGroupBase
        {
            public tControlGroup1()
            {
                tControlGroupBase.ControlGroupButtons[0] = this;
            }
        }
        public class tControlGroup2 : tControlGroupBase
        {
            public tControlGroup2()
            {
                tControlGroupBase.ControlGroupButtons[1] = this;
            }
        }
        public class tControlGroup3 : tControlGroupBase
        {
            public tControlGroup3()
            {
                tControlGroupBase.ControlGroupButtons[2] = this;
            }
        }
        public class tControlGroup4 : tControlGroupBase
        {
            public tControlGroup4()
            {
                tControlGroupBase.ControlGroupButtons[3] = this;
            }
        }
        public class tControlGroup5 : tControlGroupBase
        {
            public tControlGroup5()
            {
                tControlGroupBase.ControlGroupButtons[4] = this;
            }
        }
        public class tControlGroup6 : tControlGroupBase
        {
            public tControlGroup6()
            {
                tControlGroupBase.ControlGroupButtons[5] = this;
            }
        }
        public class tControlGroup7 : tControlGroupBase
        {
            public tControlGroup7()
            {
                tControlGroupBase.ControlGroupButtons[6] = this;
            }
        }
        public class tControlGroup8 : tControlGroupBase
        {
            public tControlGroup8()
            {
                tControlGroupBase.ControlGroupButtons[7] = this;
            }
        }
        public class tControlGroup9 : tControlGroupBase
        {
            public tControlGroup9()
            {
                tControlGroupBase.ControlGroupButtons[8] = this;
            }
        }
        public class tControlGroup10 : tControlGroupBase
        {
            public tControlGroup10()
            {
                tControlGroupBase.ControlGroupButtons[9] = this;
            }
        }
        public class tControlGroup11 : tControlGroupBase
        {
            public tControlGroup11()
            {
                tControlGroupBase.ControlGroupButtons[10] = this;
            }
        }
        #endregion
    }
}
