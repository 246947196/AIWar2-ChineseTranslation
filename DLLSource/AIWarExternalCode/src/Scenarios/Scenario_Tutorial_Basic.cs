using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    /* To add a new part to the tutorial, you must do the following.
       Add a ConditionGroup (or pick an existing one)
       Add a Condition
       Update GetConditionsInGroup to add your Condition to the appropriate Group
       Update the GetConditionIsMet() function to detect your condition
       Add appropriate text to WriteCurrentOngoingMessageToDisplay

     */
    
    public class Scenario_Tutorial_01 : BaseScenario
    {
        protected override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner()
        {
            //not sure any of this is needed, but just in case
            result_GetConditionsInGroup.Clear();
        }

        public static Scenario_Tutorial_01 Instance;
        public Scenario_Tutorial_01() { Instance = this; }

        public override bool IsTutorial()
        {
            return true;
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {

        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            
        }

        public override bool GetShouldSkipGameSetup()
        {
            return true;
        }

        public enum ConditionGroup
        {
            OpenDirectAIShipGroup,
            QueueInitialFleet,
            BuildInitialFleet,
            CheckObjectivesMenu,
            StartAttackOnMiddlePlanet,
            SwitchToMiddlePlanet,
            FightOnMiddlePlanet,
//            RetreatToFirstPlanet,
//            RebuildFleet,
            TakeMiddlePlanet,
            PrepareToTakeThirdPlanet,
            ActuallyTakeThirdPlanet,
            // NukeLastPlanet,
            // Die,
            Length
        }

        public enum Condition
        {
            FirstSimStepHasHappened,
            UserHasOpenedDirectAIShipGroup,
            UserHasOpenedDocksMenu,
            UserIsOnStartPlanet,
            UserHasBuiltSpaceDock,
            UserHasQueuedAllFleetShips,
            UserHasUnpausedAIShipGroup,
            UserHasBuiltEnoughEngineers,
            UserHasBuiltAllFleetShips,
            UserHasOpenedObjectiveMenu,
            UserHasOpenedShipsMenu,
            UserHasGivenArkMovementOrder,
            UserHasSetSpaceDockToRallyToControlGroup1,
            MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips,
            MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1,
            MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder,
            MovingToMiddlePlanet_UserHasPausedTheGame,
            MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap,
            MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet,
            MovingToMiddlePlanet_UserHasUnpausedTheGame,
            MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived,
            ArkShieldsHaveDroppedBelow50Percent,
            MovingBackToFirstPlanet_UserHasSelectedArk,
            MovingBackToFirstPlanet_UserHasGivenArkMoveOrder,
            MovingBackToFirstPlanet_UserHasSwitchedViewToTargetPlanet,
            MovingBackToFirstPlanet_ArkHasArrived,
            Rebuilding_UserHasBuiltAllFleetShips,
            Rebuilding_UserHasSelectedAllMilitaryShips,
            Rebuilding_UserHasSetAllMilitaryShipsToControlGroup1,
            Rebuilding_UserHasSelectedArkAlone,
            Rebuilding_UserHasSetSpaceDockToRallyToControlGroup1,
            UserHasRidMiddlePlanetOfDefenses,
            UserHasFreedMiddlePlanetController,
            UserHasClaimedMiddlePlanet,
            UserHasBuiltEnergyCollectorOnMiddlePlanet,
            UserHasScoutedFinalPlanet,
            UserHasBuiltFrigateConstructor,
            UserHasBuiltFrigate,
            UserHasOpenedScienceMenu,
            UserHasUpgradedThings,
            UserHasEnoughEnergy,
            UserHasFreedLastPlanet,
            TutorialIsOver,
            
            // UserHasDeployedNuclearWarhead,
            // UserHasGivenNuclearWarheadMovementOrderToLastPlanet,
            // UserHasNukedLastPlanet,
            Length
        }

        private readonly List<Condition> result_GetConditionsInGroup = List<Condition>.Create_WillNeverBeGCed( 20, "Scenario_Tutorial_01-result_GetConditionsInGroup" );
        public List<Condition> GetConditionsInGroup( ConditionGroup Group )
        {
            this.result_GetConditionsInGroup.Clear();
            switch ( Group )
            {
                case ConditionGroup.OpenDirectAIShipGroup:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDirectAIShipGroup );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltEnoughEngineers );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltSpaceDock );
                    break;
                case ConditionGroup.QueueInitialFleet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDocksMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasQueuedAllFleetShips );
                    break;
                case ConditionGroup.BuildInitialFleet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasEnoughEnergy );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasUnpausedAIShipGroup );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltAllFleetShips );
                    break;
                case ConditionGroup.CheckObjectivesMenu:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedObjectiveMenu );
                    break;
                case ConditionGroup.StartAttackOnMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1 );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDocksMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasSetSpaceDockToRallyToControlGroup1 );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder );
                    break;
                case ConditionGroup.SwitchToMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasPausedTheGame );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasUnpausedTheGame );
                    break;
                case ConditionGroup.FightOnMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedShipsMenu );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived );
//                    this.result_GetConditionsInGroup.Add( Condition.ArkShieldsHaveDroppedBelow50Percent );
                    break;
                // case ConditionGroup.RetreatToFirstPlanet:
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasSelectedArk );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasGivenArkMoveOrder );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasSwitchedViewToTargetPlanet );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_ArkHasArrived );
                //     break;
                // case ConditionGroup.RebuildFleet:
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasBuiltAllFleetShips );
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasSelectedAllMilitaryShips );
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasSetAllMilitaryShipsToControlGroup1 );
                //     break;
                case ConditionGroup.TakeMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasRidMiddlePlanetOfDefenses );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasFreedMiddlePlanetController );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasClaimedMiddlePlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltEnergyCollectorOnMiddlePlanet );
                    break;
                case ConditionGroup.PrepareToTakeThirdPlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserIsOnStartPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasScoutedFinalPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltFrigateConstructor );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltFrigate );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedScienceMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasUpgradedThings );
                    break;
                case ConditionGroup.ActuallyTakeThirdPlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasEnoughEnergy );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasFreedLastPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.TutorialIsOver );                    
                    break;
                    // case ConditionGroup.KillAI:
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasKilledAI );
                    //     break;
                    // case ConditionGroup.NukeLastPlanet:
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasDeployedNuclearWarhead );
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasGivenNuclearWarheadMovementOrderToLastPlanet );
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasNukedLastPlanet );
                    //     break;
                    // case ConditionGroup.Die:
                    //     break;
            }

            return this.result_GetConditionsInGroup;
        }

        public override bool WriteCurrentOngoingMessageToDisplay( ArcenDoubleCharacterBuffer Buffer )
        {
            //if ( !World.Instance.GetIsTutorialConditionFulfilled( Condition.FirstSimStepHasHappened ) )
            //    return false;

            for ( ConditionGroup group = 0; group < ConditionGroup.Length; group++ )
            {
                List<Condition> conditions = this.GetConditionsInGroup( group );
                bool allAlreadyMet = conditions.Count > 0;
                for ( int i = 0; i < conditions.Count; i++ )
                {
                    Condition condition = conditions[i];
                    if ( condition.GetAlreadyMet() )
                        continue;
                    allAlreadyMet = false;
                    break;
                }
                if ( allAlreadyMet )
                    continue;
                bool allMetNow = conditions.Count > 0;
                for ( int i = 0; i < conditions.Count; i++ )
                {
                    Condition condition = conditions[i];
                    if ( condition.GetMetNow() )
                        continue;
                    allMetNow = false;
                    break;
                }
                if(allMetNow)
                {
                    for ( int i = 0; i < conditions.Count; i++ )
                    {
                        Condition condition = conditions[i];
                        condition.MarkMet();
                    }
                    continue;
                }

                int maxHeader = 34;

                switch ( group )
                {
                    case ConditionGroup.OpenDirectAIShipGroup:
                        if ( !Condition.UserHasOpenedDirectAIShipGroup.GetMetNow() )
                        {
                            WriteHeader( Buffer, 1, maxHeader );
                            Buffer.Add( "欢迎来到 AI War 2！" ).Add( "\n" );
                            Buffer.Add( "这是一个帮助玩家快速上手的基础教程。" ).Add( "\n\n" );

                            Buffer.Add( "首先让我们学习如何控制视角：" ).Add( "\n\n" );

                            Buffer.Add( "--方向键或 WASD 移动视角上下左右，或将鼠标移至屏幕边缘" ).Add( "\n" );
                            Buffer.Add( "--按住 Q 并移动鼠标旋转视角" ).Add( "\n" );
                            Buffer.Add( "--滚轮或 PageUp/PageDown 缩放" ).Add( "\n" );
                            Buffer.Add( "视角移动速度可在设置菜单中调整，按 Escape 打开设置。\n" );
                            Buffer.Add( "\n" );
                            Buffer.Add( "准备好后，在侧边栏中选择建造菜单，点击 Build 标签或按下 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab")).Add(" 直到打开。")
                                  .Add("按下 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab"))
                                  .Add(" 将在建造与船坞菜单间切换。建造菜单用于放置建筑等重要单位，船坞菜单用于建造主要作战单位。" ).Add( "\n" );
                        }
                        //TODO: tell the player "Home Command Stations are important"
                        else if ( !Condition.UserHasBuiltEnoughEngineers.GetMetNow() )
                        {
                            WriteHeader( Buffer, 2, maxHeader );
                            Buffer.Add( "为了协助建造，让我们先建造更多工程师。你初始有两个，但我们需要更多。工程师是非常有用的单位，可以协助建造建筑或舰船，还能在战后修理你的单位。\n\n")
                                .Add( "要建造工程师，在建造菜单中找到它们（图标看起来像齿轮），点击工程师图标。选择工程师图标后，你将进入建筑放置模式，鼠标光标将变为正在建造的单位形状。\n\n")
                                .Add("在地图上左键点击 10 次开始建造 10 个工程师，或按住 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("Build5xUnits"))
                                .Add( "（每次建造五个单位），点击两次。")
                                .Add(" 建造完成后，右键点击退出建筑放置模式。你总共应该有至少 10 个工程师。");
                        }
                        else if(!Condition.UserHasBuiltSpaceDock.GetMetNow() )
                        {
                            WriteHeader( Buffer, 3, maxHeader );
                            Buffer.Add( "现在在建造菜单的基础设施部分，你会在列表末尾附近找到太空船坞（悬停图标查找）。点击它，再点击星球地图开始建造。" ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.QueueInitialFleet:
                        if ( !Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 4, maxHeader );
                            Buffer.Add( "要建造舰船，打开船坞标签，点击或按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab")).Add(" 一次。" );
                        }
                        else if(Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 5, maxHeader );
                            Buffer.Add( "现在我们需要建造一些小型的\"舰队舰船\"来准备战斗。\n\n在船坞菜单中，你可以看到可建造的舰船。图标下方的数字是你能同时拥有的该类型舰船的最大数量。").Add("\n").
                                Add("建造队列会自动循环，会持续建造所选单位直到你让它停止。\n你可以通过建造队列上方的'暂停'按钮暂停建造，但现在先不要暂停；首先我们需要建造一支舰队！\n\n点击每种舰船类型的图标来加入队列。").Add("\n\n").
                                Add("当前有 5 种舰船可供建造。侦查舰仅用于探索，我们稍后会用到它们。其他是初始战斗舰船。");
                        }
                        else if ( !Condition.UserHasQueuedAllFleetShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 6, maxHeader );
                            Buffer.Add( "在\"太空船坞\"下点击所有当前可建造的舰船类型。点击后舰船模型会高亮，表示正在建造。" ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.BuildInitialFleet:
                        if (! Condition.UserHasEnoughEnergy.GetMetNow() )
                        {
                            WriteHeader( Buffer, 7, maxHeader );
                            Buffer.Add( "看起来在建造完所有舰队舰船之前能量用完了。你需要废弃一些多余的单位或建筑才能继续教程。要废弃一个或一组单位，选中它们并按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ScrapUnits" )).Add("\n\n");
                        }
                        else if(! Condition.UserHasUnpausedAIShipGroup.GetMetNow() )
                        {
                            WriteHeader( Buffer, 8, maxHeader );
                            Buffer.Add( "你需要取消暂停太空船坞，以便它能建造你的舰队。" ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasBuiltAllFleetShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 9, maxHeader );
                            Buffer.Add( "你的舰队正在建造！不过可能需要一些时间。你可以查看'船坞'菜单了解剩余舰船数量。\n\n");
                            Buffer.Add( "你可以通过按 " ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseFrameSize" ) )
                                .Add( " 或 " ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "DecreaseFrameSize" ) ).Add(" 加速或减速时间。\n\n")
                            .Add( "现在我们将等待每种类型的最大数量建造完成。" ).Add( "\n\n" );
                        }
                        break;
                    case ConditionGroup.CheckObjectivesMenu:
                        if(!Condition.UserHasOpenedObjectiveMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 10, maxHeader );
                            Buffer.Add("有时难以知道下一个目标是什么。要了解游戏中的目标，让我们打开侧边栏中的目标菜单或点击")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenObjectivesTab"))
                                .Add(" 并悬停查看项目。该菜单提供了有关你在游戏中应完成目标的有用指南。\n");
                        }
                        break;
                    case ConditionGroup.StartAttackOnMiddlePlanet:
                        if ( !Condition.MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 11, maxHeader );
                            Buffer.Add( "查看目标后，让我们集结舰队去大干一场。" ).Add( "\n" );
                            Buffer.Add( "选择所有军事单位（框选全部，或按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SelectAllMobileMilitary")).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1.GetMetNow() )
                        {
                            WriteHeader( Buffer, 12, maxHeader );
                            Buffer.Add( "接下来，将所有已选单位加入第一个控制组，按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ModifyControlGroup" )).Add(" + X，X 是一个数字。例如你可以使用 " ).Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ModifyControlGroup")).Add(" + 1 作为控制组 1。游戏中最多可以定义 10 个控制组，但现在我们用一个控制组包含所有单位。").Add( "\n\n" );
                            Buffer.Add( "完成后，让我们重新打开船坞菜单，这样新建舰船可以直接集结到你的舰队。" ).Add( "\n" );
                        }
                        else if( !Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 13, maxHeader );
                            Buffer.Add( "我们不希望每次都手动将新建单位分配到控制组，所以让我们设置它们集结到该组。确保你仍在起始星球，选择船坞菜单（或按 B 一次）。" ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasSetSpaceDockToRallyToControlGroup1.GetMetNow() )
                        {
                            WriteHeader( Buffer, 14, maxHeader );
                            Buffer.Add("你可以通过'集结'按钮将新建舰船集结到固定位置，或通过'编组'按钮集结到编组。")
                                .Add("我们想使用编组按钮，所以按下它。集结激活时图标将变为绿色。").Add("\n\n")
                                .Add("这也会自动将所有新建舰船发送到控制组的位置，并将它们加入控制组。" ).Add( "\n\n" )
                                .Add( "注意每个船坞可以设置不同的集结方式。所以如果你希望护卫舰和舰队舰船一起集结（目前你确实希望如此），那么点击两者的按钮。" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder.GetMetNow() )
                        {
                            WriteHeader( Buffer, 15, maxHeader );
                            Buffer.Add("最后，派遣你的舰队前往下一个星球。星球右侧有一个虫洞；你可能需要向右平移才能看到。选中你的单位后，按住 ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SendThroughWormhole"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" 虫洞或虫洞上方名称，让舰船穿过虫洞前往下一个星球。" ).Add( "\n\n" )
                                .Add("注意！之后如果你远距离旅行，只需切换到银河地图并悬停在任何星球上。它将显示你的舰船将要经过的路线。然后按 ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" 你的舰船将直接前往目的地。").Add( "\n" );
                        }
                        break;
                    case ConditionGroup.SwitchToMiddlePlanet:
                        if ( !Condition.MovingToMiddlePlanet_UserHasPausedTheGame.GetMetNow() )
                        {
                            WriteHeader( Buffer, 16, maxHeader );
                            Buffer.Add( "但你不想你的单位在你看不到它们的情况下到达那里。" ).Add( "\n" );
                            Buffer.Add( "按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause"))
                                .Add(" 或 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap.GetMetNow() )
                        {
                            WriteHeader( Buffer, 17, maxHeader );
                            Buffer.Add( "现在按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleGalaxyMap")).Add(" 从行星视图切换到银河视图。" ).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 18, maxHeader );
                            Buffer.Add( "这是银河视图，在实际游戏中你的大部分策略都在这里制定。" ).Add( "\n" );
                            Buffer.Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("MakePlanetClickSelectAndSwitchView"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SelectUnit")).Add(" 中间那颗星球，也就是你刚命令舰船前往的地方。").Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasUnpausedTheGame.GetMetNow() )
                        {
                            WriteHeader( Buffer, 19, maxHeader );
                            Buffer.Add("很好！现在按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause")).Add("（或 ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add("）再次取消暂停游戏。").Add( "\n" );
                        }
                        break;
                    case ConditionGroup.FightOnMiddlePlanet:
                        if ( !Condition.UserHasOpenedShipsMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 20, maxHeader );
                            Buffer.Add("在等待舰队到达时，让我们打开舰船侧边栏菜单，点击或按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenShipsTab")).Add("。这将显示星球上的所有舰船，是管理战斗的常用方式。你可以看到你的单位，也可以通过点击舰船侧边栏中的图标来选择它们。").Add("\n\n")
                                .Add("侧边栏还会显示双方的小队数量和战力值；战力是你部队强大程度的指标，显示在 stylized S 旁边的数字。" ).Add( "\n" );
                        }
                        if ( !Condition.MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived.GetMetNow() )
                        {
                            WriteHeader( Buffer, 21, maxHeader );
                            Buffer.Add("如果你的游戏仍处于暂停状态，按 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause")).Add("（或 ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add(" 取消暂停。我们稍等片刻，让舰船到达并开始行动。" ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.TakeMiddlePlanet:
                        if ( !Condition.UserHasRidMiddlePlanetOfDefenses.GetMetNow() )
                        {
                            WriteHeader( Buffer, 22, maxHeader );
                            Buffer.Add( "你可以通过右键点击位置或目标来移动所选单位。这个星球由守卫哨站防御，靠近时会生成 AI 舰队舰船。让我们先把部队移向守卫哨站并将其摧毁。你需要保持舰队集中以最大化火力。" )
                                .Add(" 也就是说，你也可以将部队设为追击模式，点击 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleFRD")).Add("，让它们自行选择目标。").Add( "\n\n" );
                            Buffer.Add( "可能需要多次进攻，如果最初几次失利，你可能需要建造新舰队。你可能会看到增援部队集结到你的舰队；这是你之前设置的编组集结。" ).Add( "\n\n" );

                            Buffer.Add( "通常建议在占领星球前摧毁所有 AI 防御建筑和单位。" ).Add( "\n" );
                            Buffer.Add( "当所有 AI 防御被摧毁后教程将继续。" ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasFreedMiddlePlanetController.GetMetNow() )
                        {
                            WriteHeader( Buffer, 23, maxHeader );
                            Buffer.Add( "做得好！现在对敌人虫洞和\"指挥站\"下达攻击命令（如果尚未下达），让舰船摧毁它们。" ).Add( "\n\n" );

                            Buffer.Add( "你的单位通常不会在没有命令的情况下攻击这些目标，因为摧毁它们会触发\"AI 进度\"增加，即 AI 攻击你的积极性提高。" ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasClaimedMiddlePlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 24, maxHeader );
                            Buffer.Add("要占领星球，返回你的母星（使用 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleGalaxyMap")).Add(" 打开银河菜单，然后按住 Ctrl 点击该星球切换到原始星球），在建造菜单中找到\"殖民船\"。点击殖民船图标，然后点击星球选择建造位置。建造完成后，将其带到中间星球。然后在中间星球打开建造菜单建造一个新的指挥站。指挥站有三种类型；教程中随便选一个。" ).Add( "\n\n" );

                            Buffer.Add( "当星球属于你后教程将继续。" ).Add( "\n\n" ).Add("指挥站建造速度较慢，所以你可以派几个工程师过去帮助加快建造。这是工程师的众多价值之一！");
                        }
                        else if(! Condition.UserHasBuiltEnergyCollectorOnMiddlePlanet.GetMetNow())
                        {
                            Buffer.Add( "游戏中的关键资源之一是能量；能量是一种全局资源，允许你建造舰船、炮塔和其他关键建筑。获取能量的主要方式是在每个星球上建造能量收集器。让我们在你的新星球上建造一个。你可以在建造菜单的基础设施部分找到它" ).Add( "\n\n" )
                                .Add("注意你可以通过设置菜单中的自动化选项让能量收集器在你的星球上自动建造。") ;
                        }
                        break;
                      case ConditionGroup.PrepareToTakeThirdPlanet:
                          
                          if(!Condition.UserIsOnStartPlanet.GetMetNow() && !Condition.UserHasScoutedFinalPlanet.GetMetNow() )
                          {
                            WriteHeader( Buffer, 25, maxHeader );
                            Buffer.Add( "为了完成教程，我们将击败第三颗星球上的 AI。在攻击之前，先派一些侦查舰到最后的星球看看他们的防御情况。让我们先回到第一颗星球找一些侦查舰。\n\n");
                          }
                          else if(!Condition.UserHasScoutedFinalPlanet.GetMetNow() )
                          {
                            WriteHeader( Buffer, 26, maxHeader );
                            Buffer.Add("侦查舰是隐形、快速但无武装的舰船，用于在攻击前获取星球情报，或监视敌人的活动。侦查舰属于舰队舰船，在太空船坞建造。这个星球上应该已经有一些了，选中它们然后切换到银河地图。" ).Add( "\n\n" )
                                .Add("你可以从银河地图给单位下达命令。由于你已经选中了一些侦查舰，").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("MakePlanetClickSelectAndSwitchView"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" 最后星球以派遣侦查舰。提前了解敌人防御将帮助你选择攻击哪些星球以及如何攻击。\n");
                          }
                          else if(!Condition.UserHasBuiltFrigateConstructor.GetMetNow() )
                          {
                              WriteHeader( Buffer, 27, maxHeader );
                              Buffer.Add( "敌方星球是 Mark 2 星球，比你刚占领的 Mark 1 星球强大得多。你需要加强你的舰队才能击败它。首先，我们要建造护卫舰。让我们回到起始星球建造一个护卫舰船坞；它在建造菜单中太空船坞附近。" ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasBuiltFrigate.GetMetNow() )
                          {
                            WriteHeader( Buffer, 28, maxHeader );
                            Buffer.Add( "护卫舰明显比舰队舰船更强，这使它们成为宝贵的工具。首先，打开船坞菜单，点击护卫舰船坞的'编组'按钮，让建造的单位集结到你的舰队。然后点击突击护卫舰开始建造。" ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasOpenedScienceMenu.GetMetNow() && !Condition.UserHasUpgradedThings.GetMetNow() )
                          {
                            WriteHeader( Buffer, 29, maxHeader );
                            Buffer.Add( "你还需要升级一些舰队舰船。升级单位会让它们变得更强大，并允许你建造更多。让我们打开科技菜单看看。" ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasUpgradedThings.GetMetNow() )
                          {
                            WriteHeader( Buffer, 30, maxHeader );
                            Buffer.Add( "悬停在单位上时会告诉你升级后能变强多少。升级一些舰队舰船（最上面的类别），然后我们进攻。注意你可能不想升级侦查舰，因为它不是战斗单位。" ).Add( "\n\n" );
                        }
                        break;
                    case ConditionGroup.ActuallyTakeThirdPlanet:
                        if ( !Condition.UserHasEnoughEnergy.GetMetNow() )
                        {
                            WriteHeader( Buffer, 31, maxHeader );
                            Buffer.Add( "你需要更多能量来建造更多舰船。获取能量的主要方式是在每个星球上建造能量收集器——确保每个星球都有一个！如果你的领土不足以支持能量需求，你也可以选中单位并点击 " )
                                .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ScrapUnits" ) ).Add( " 来废弃单位。\n\n" );
                        }
                        else if ( !Condition.UserHasFreedLastPlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 33, maxHeader );
                            Buffer.Add( "摧毁最后一颗星球以完成教程。你可能需要多次攻击，或升级更多舰船才能做到" ).Add( "\n\n" );
                        }
                        else if ( !Condition.TutorialIsOver.GetMetNow() )
                        {
                            WriteHeader( Buffer, 34, maxHeader );
                            Buffer.Add( "恭喜你完成了教程！这只是让你熟悉操作的快速简单体验。现在试试快速开始选项之一吧。\n\n记得关注你的目标标签！记住失败也没关系——一些最史诗的故事来自于奋力拼搏后的失败。别紧张，看看银河系会给你带来什么。" ).Add( "\n\n" );
                        }
                        break;
                   // case ConditionGroup.KillAI:
                   //     if( !Condition.UserHasKilledAI.GetMetNow())
                   //     {
                   //         Buffer.Add( "如果你看最后的星球，你会看到 AI 的母星。通常它要远得多，而且防御非常严密。" ).Add( "\n" )
                   //             .Add("但这是教程，AI 忘了建造防御。去击败 AI Overlord 你就赢了！").Add("\n");
                   //     }
                   //     else
                   //     {
                   //         Buffer.Add( "你赢了！现在试试从主菜单使用快速开始进行正常游戏").Add("\n");
                   //     }
                   //     break;
                    // case ConditionGroup.NukeLastPlanet:
                    //     if ( !Condition.UserHasDeployedNuclearWarhead.GetMetNow() )
                    //     {
                    //         Buffer.Add( "If you look at the next planet down the line, you'll see lots of nasty units. That's the AI's homeworld. Normally it's much further away, and even nastier." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "The only thing you have to do to win is destroy the AI Master Controller on that planet." ).Add( "\n" );
                    //         Buffer.Add( "You don't have the conventional forces to do that in this tutorial." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "So we'll Nuke it instead." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Select your Ark, click the Warhead button, and then the Nuclear Warhead button above that." ).Add( "\n" );
                    //     }
                    //     else if ( !Condition.UserHasGivenNuclearWarheadMovementOrderToLastPlanet.GetMetNow() )
                    //     {
                    //         Buffer.Add( "Congratulations, you have just exposed an galaxy-threatening fully-annihilating-fusion-reaction bomb to danger. It's also your only one." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Better use it quick." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Select the Nuke and tell it to move to the AI homeworld." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Normally an AI homeworld has devices which counter the effects of nuclear warheads, and it's a big pain to destroy them, but for this tutorial we've somehow neglected to include those." ).Add( "\n" );
                    //     }
                    //     else if ( !Condition.UserHasNukedLastPlanet.GetMetNow() )
                    //     {
                    //         Buffer.Add( "Now we wait for the bang (well, no sound effect just yet, but it will make a lot of stuff just go away)." ).Add( "\n" );
                    //     }
                    //     break;
                    // case ConditionGroup.Die:
                    //     Buffer.Add( "Well done! You've made the AI very angry, and it's now going to find you and facilitate communications." ).Add( "\n" );
                    //     Buffer.Add( "" ).Add( "\n" );
                    //     Buffer.Add( "We can't really help you now, but it will help you learn the very most important lesson in AI War:" ).Add( "\n" );
                    //     Buffer.Add( "" ).Add( "\n" );
                    //     Buffer.Add( "Do not make the AI mad before you can make it dead." ).Add( "\n" );
                    //     break;
                }
                break;
            }

            Buffer.Add( "</size>" );
            return true;
        }

        public void WriteHeader( ArcenDoubleCharacterBuffer Buffer, int Index, int MaxIndex )
        {
            Buffer.Add( "<b><color=#78beff>T</color><color=#6db9ff>u</color><color=#5eb1ff>t</color><color=#5ec4ff>o</color><color=#52c0ff>r</color><color=#52d8ff>i</color><color=#42d5ff>a</color><color=#24deff>l</color><color=#78beff> 步骤 " )
                    .Add( Index ).Add( "/" ).Add( MaxIndex ).Add( "</b>:</color>\n" );

            if ( World.Instance.IsPaused )
                Buffer.Add( "<color=#ffd75e>游戏已暂停！</color>\n" ).Add(FontSizes.BASE_SIZE_STRING);
        }
        public bool GetIsConditionMet(Condition Condition)
        {
            return true; //this is the old style tutorial, not the new kind that is actually used
        }
        private static bool GetAreAllFactionMobileMilitaryMovingToPlanet( Faction localFaction, Planet targetPlanet )
        {
            bool foundAll = true;
            foreach ( GameEntity_Squad entity in localFaction.Squads( EntityRollupType.MobileCombatants ) )
            {
                if ( GetIsShipMovingToPlanet( targetPlanet, entity ) )
                    continue;
                foundAll = false;
                break;
            }
            return foundAll;
        }

        private static bool GetIsShipMovingToPlanet( Planet targetPlanet, GameEntity_Squad entity )
        {
            EntityOrder entityOrder = entity.ForShortTermPlanning_CurrentValidOrder;
            if ( entityOrder.TypeData == null ) 
                return false;
            if ( entityOrder.TypeData.Type != EntityOrderType.Wormhole )
                return false;
            if ( entityOrder.RelatedPlanetIndex != targetPlanet.Index )
                return false;
            return true;
        }

        private static bool GetAreEnoughFactionMobileMilitaryOnPlanet( Faction localFaction, Planet targetPlanet )
        {
            int numOnPlanet = 0;
            int total = 0;
            foreach ( GameEntity_Squad entity in localFaction.Squads( EntityRollupType.MobileCombatants ) )
            {
                total++;
                if ( entity.Planet == targetPlanet )
                    numOnPlanet++;
            }
            if( numOnPlanet * 1.5 > total)
                return true;
            return false;
        }

        public override void DoOnFirstUnpauseLogic_SinceLoadOrGeneration_HostOrClient( ArcenClientOrHostSimContextCore Context )
        {
        }
        public override void DoOnFirstUnpauseLogic_FromGameSetup_HostOnly( bool IsFromMapGen )
        {
            

        }
    }

    public static class Tutorial01_Condition_Extensions
    {
        public static bool GetAlreadyMet(this Scenario_Tutorial_01.Condition Condition)
        {
            return false;// World.Instance.GetIsTutorialConditionFulfilled( Condition );
        }

        public static bool GetMetNow(this Scenario_Tutorial_01.Condition Condition )
        {
            return Scenario_Tutorial_01.Instance.GetIsConditionMet( Condition );
        }

        public static void MarkMet( this Scenario_Tutorial_01.Condition Condition )
        {
            //World.Instance.SetTutorialConditionFulfilled( Condition, true );
        }
    }
}
