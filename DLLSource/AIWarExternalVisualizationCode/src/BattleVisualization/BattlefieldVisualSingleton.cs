using System;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.ExternalVisualization
{
    public partial class BattlefieldVisualSingleton : IBattlefieldVisualHandler
    {
        public static Color Color_Target_Src = Color.green;
        public static Color Color_Target_Dst = Color.red;
        public static Color Color_FRD_Src = Color.white;//.SetAlpha(0.5f);
        public static Color Color_FRD_Dst = Color.white;
        
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() 
        {
            //just in case, and really not critical
            SoundPlaybackRequestsImmediate.Clear();
            SoundPlaybackRequestsDelayed.Clear();
            SoundPlaybackRequestsDelayed_Alt.Clear();

            //well, everything else seems to be in order
        }

        public readonly List<ShipVisualizer> BurningDyingShips = List<ShipVisualizer>.Create_WillNeverBeGCed( 90000, "BattlefieldVisualSingleton-BurningDyingShips" );
        public readonly List<GenericObjectVisualizer> LooseOtherObjects = List<GenericObjectVisualizer>.Create_WillNeverBeGCed( 90000, "BattlefieldVisualSingleton-LooseOtherObjects" );
        /// <summary>
        /// if there are more than half a million shots it's going to freak out.  Seems okay to me.
        /// </summary>
        public readonly SwizzleList<ShotVisualizer> ActiveShots = SwizzleList<ShotVisualizer>.Create_WillNeverBeGCed( "ActiveShots", 500000 );
        public readonly List<SpecialEffect> ActiveSpecialEffects = List<SpecialEffect>.Create_WillNeverBeGCed( 90000, "BattlefieldVisualSingleton-ActiveSpecialEffects" );
        public readonly List<SquadVisualizer> ActiveSquads = List<SquadVisualizer>.Create_WillNeverBeGCed( 90000, "BattlefieldVisualSingleton-ActiveSquads" );
        public int NumSelected;

        public readonly ConcurrentQueue<SpecialEffectRequest> SpecialEffectRequests = ConcurrentQueue<SpecialEffectRequest>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-SpecialEffectRequests" );
        public readonly ConcurrentQueue<IVisualObject> VisualObjectRemovalRequests = ConcurrentQueue<IVisualObject>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-VisualObjectRemovalRequests" );
        public readonly ConcurrentQueue<IInstancedRenderer> IInstancedRendererRemovalRequests = ConcurrentQueue<IInstancedRenderer>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-IInstancedRendererRemovalRequests" );
        public readonly ConcurrentQueue<SoundPlaybackRequestImmediate> SoundPlaybackRequestsImmediate = ConcurrentQueue<SoundPlaybackRequestImmediate>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-SoundPlaybackRequestsImmediate" );

        //these two are not readonly because they alternate back and forth
        public ConcurrentQueue<SoundPlaybackRequestDelayed> SoundPlaybackRequestsDelayed = ConcurrentQueue<SoundPlaybackRequestDelayed>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-SoundPlaybackRequestsDelayed" );
        public ConcurrentQueue<SoundPlaybackRequestDelayed> SoundPlaybackRequestsDelayed_Alt = ConcurrentQueue<SoundPlaybackRequestDelayed>.Create_WillNeverBeGCed( "BattlefieldVisualSingleton-SoundPlaybackRequestsDelayed_Alt" );

        public int SpecialEffectRequestCount = 0;
        public int VisualRemovalRequestCount = 0;

        public int lastPlanetID = -1;

        public readonly List<GameObject> testObjects = List<GameObject>.Create_WillNeverBeGCed( 7000, "BattlefieldVisualSingleton-testObjects" );

        public float FastTrackDisplayRequestsForRemaining = 1f;
        private float TimeSinceCurrentPlanetWasNotNull = 0f;
        public int CurrentLODAndGimbalsPass = 1;
        public int CurrentUpdateSpritesPass = 1;
        public int CurrentShotRemovalChecksPass = 1;
        public int CurrentShotMovementPass = 1;

        public static int currentSimFrameVisualOnly = 0;
        public static bool isInEditor = false;

        private GameViewMode lastGameViewMode = GameViewMode.ModalMenu;

        private readonly float timeLostAnimationPlays = 7f;

        private readonly float timeWonAnimationPlays = 4f;

        public static BattlefieldVisualSingleton Instance;
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BattlefieldVisualSingletons" );
        public BattlefieldVisualSingleton() //the constructor itself should define the singleton
        {
            RefTracker.IncrementObjectCount();
            Instance = this;
        }

        public Vector3 MainCameraPos;
        public Vector3 MainUpVector;
        public Vector3 GalaxyCameraPos;
        public Vector3 GalaxyUpVector;

        public System.Numerics.Vector3 MainCameraPos_Numerics;
        public System.Numerics.Vector3 GalaxyCameraPos_Numerics;

        private static InputActionTypeData inputShowShipRanges_Selected = null;
        private static InputActionTypeData inputShowShipRanges_Hovered = null;
        private static InputActionTypeData inputShowShipRanges_All = null;
        private static InputActionTypeData inputShowShipOrders = null;

        public void RunUpdate()
        {
            if ( Engine_Universal.DebugTimeMode == DebugTimeMode.StopSimAndVisualUpdates )
                return;

            this.MainCameraPos = ArcenMainGameVisuals.Instance.MainCameraPos;
            ArcenSFXItemBase.MainCameraPos = this.MainCameraPos;
            this.MainUpVector = ArcenMainGameVisuals.Instance.MainUpVector;
            this.GalaxyCameraPos = ArcenMainGameVisuals.Instance.GalaxyCameraPos;
            this.GalaxyUpVector = ArcenMainGameVisuals.Instance.GalaxyUpVector;

            this.MainCameraPos_Numerics = this.MainCameraPos.ToNumericsVector3();
            ShipToShipLineRenderer.MainCameraPos_Numerics = this.MainCameraPos_Numerics;
            this.GalaxyCameraPos_Numerics = this.GalaxyCameraPos.ToNumericsVector3();

            if ( this.FastTrackDisplayRequestsForRemaining > 0 )
                this.FastTrackDisplayRequestsForRemaining -= Time.smoothDeltaTime;

            if ( Engine_Universal.DebugVisualUpdateSecondsToRun > 0 )
            {
                Engine_Universal.DebugVisualUpdateSecondsToRun -= Time.smoothDeltaTime;
                if ( Engine_Universal.DebugVisualUpdateSecondsToRun <= 0 )
                    Engine_Universal.DebugTimeMode = DebugTimeMode.StopSimAndVisualUpdates;
            }

            if ( !ArcenSpriteManager.MeshBased_RenderHostPrefab )
                ArcenSpriteManager.MeshBased_RenderHostPrefab = ArcenVisualOrganizer.Instance.ExampleBoundsSpotPrefab;

            ArcenSpriteManager.Instance.ResetForNextFrame();

            bool doCameraFadeInOut = false;
            GameViewMode newMode = Engine_AIW2.Instance.CurrentGameViewMode;
            if ( this.lastGameViewMode != newMode )
            {
                doCameraFadeInOut = true;
                this.lastGameViewMode = newMode;
            }

            if ( InputActionTypeDataTable.IsInitialized )
            {
                if ( inputShowShipRanges_Selected == null )
                {
                    inputShowShipRanges_Selected = InputActionTypeDataTable.GetActionByName_FairlySlow( "ShowShipRanges_Selected" );
                    inputShowShipRanges_Hovered = InputActionTypeDataTable.GetActionByName_FairlySlow( "ShowShipRanges_Hovered" );
                    inputShowShipRanges_All = InputActionTypeDataTable.GetActionByName_FairlySlow( "ShowShipRanges_All" );
                    inputShowShipOrders = InputActionTypeDataTable.GetActionByName_FairlySlow( "ShowShipOrders" );
                }


                if ( Engine_Universal.IsAnyTextboxFocused )
                {
                    ArcenInput_AIW2.ShouldShowShipRanges_Selected = false;
                    ArcenInput_AIW2.ShouldShowShipRanges_Hovered = false;
                    ArcenInput_AIW2.ShouldShowShipRanges_All = false;
                    ArcenInput_AIW2.ShouldShowShipOrders = false;
                }
                else
                {
                    ArcenInput_AIW2.ShouldShowShipRanges_Selected = inputShowShipRanges_Selected.CalculateIsKeyDownNow_IfNothingElseConflicts();
                    ArcenInput_AIW2.ShouldShowShipRanges_Hovered = inputShowShipRanges_Hovered.CalculateIsKeyDownNow_IfNothingElseConflicts();
                    ArcenInput_AIW2.ShouldShowShipRanges_All = inputShowShipRanges_All.CalculateIsKeyDownNow_IfNothingElseConflicts();
                    ArcenInput_AIW2.ShouldShowShipOrders = inputShowShipOrders.CalculateIsKeyDownNow_IfNothingElseConflicts();
                }         
            }

            foreach (var cb in _updateCallbacks)
            {
                cb();
            }

            this.HandleImmediateSoundPlaybackRequests();
            this.HandleDelayedSoundPlaybackRequests();

            switch ( newMode )
            {
                case GameViewMode.MainGameView:
                    {
                        Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                        if ( currentPlanet == null )
                        {
                            if ( this.lastPlanetID >= 0 )
                                this.lastPlanetID = -1;
                            this.ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason.VisMainGameViewPlanetNull );
                            this.ClearPlanetEntities( InstancedRendererDeactivationReason.VisMainGameViewPlanetNull );
                            this.FastTrackDisplayRequestsForRemaining = 2f;
                            this.TimeSinceCurrentPlanetWasNotNull = 0f;
                        }
                        else
                        {
                            if ( this.lastPlanetID != currentPlanet.Index ) //planet is different
                            {
                                //This is the case where we have tabbed into a new planet
                                //ArcenDebugging.ArcenDebugLogSingleLine("NEW PLANET", Verbosity.DoNotShow );
                                this.lastPlanetID = currentPlanet.Index;
                                this.ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason.VisMainGameViewPlanetDifferent );
                                this.ClearPlanetEntities( InstancedRendererDeactivationReason.VisMainGameViewPlanetDifferent );
                                this.FastTrackDisplayRequestsForRemaining = 2f;
                                this.TimeSinceCurrentPlanetWasNotNull = 0f;
                                //doCameraFadeInOut = true;
                            }
                            else
                                this.TimeSinceCurrentPlanetWasNotNull += Time.smoothDeltaTime;

                            if ( this.TimeSinceCurrentPlanetWasNotNull > 0.01f )
                            {
                                currentSimFrameVisualOnly = Engine_AIW2.Instance.CurrentSimFrameVisualOnly;
                                isInEditor = !Application.isPlaying;
                                this.RunPlanetUpdateTree();
                            }
                        }

                        ShipToShipLineRenderer.RenderAll();
                    }
                    break;
                default: //not at a planet visually
                    if ( this.lastPlanetID >= 0 )
                        this.lastPlanetID = -1;
                    this.ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason.VisLayerNotInPlanetViewAtAll );
                    this.ClearPlanetEntities( InstancedRendererDeactivationReason.VisLayerNotInPlanetViewAtAll );
                    this.FastTrackDisplayRequestsForRemaining = 2f;
                    this.TimeSinceCurrentPlanetWasNotNull = 0f;

                    ShipToShipLineRenderer.ClearAll();
                    break;
            }

            this.SpecialEffectRequestCount = this.SpecialEffectRequests.Count;
            this.VisualRemovalRequestCount = this.VisualObjectRemovalRequests.Count + this.IInstancedRendererRemovalRequests.Count;
            if ( World.Instance.ConclusionType == CampaignConclusionType.Won )
            {                
                World_AIW2.Instance.timeSinceGameWasWon += Engine_AIW2.Instance.DeltaTime;
                if ( World_AIW2.Instance.timeSinceGameWasWon >= timeWonAnimationPlays )
                {
                    if ( ArcenVisualOrganizer.Instance.YouWinController.GetIsAnimationOn() )
                    {
                        ArcenVisualOrganizer.Instance.YouWinController.HideAnimation();
                    }
                    if ( !World_AIW2.Instance.HasPlayedEndingScene && World_AIW2.Instance.TutorialOrNull == null )
                    {
                        World_AIW2.Instance.HasPlayedEndingScene = true;

                        if ( !World.Instance.IsPaused )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.PauseOnly], GameCommandSource.AnythingElse );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }

                        Fleet bestFleet = this.FindMostEffectiveFleetOfLocalPlayer();
                        string bestFleetName = (bestFleet == null ? "无用的懦夫" : bestFleet.GetName());

                        Planet homePlanet = this.FindOriginalHomePlanetOfLocalPlayer();
                        string homePlanetName = (homePlanet == null ? "瓦砾堆" : homePlanet.Name);

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.VictoryTextHeader )
                            ArcenVisualOrganizer.Instance.VictoryTextHeader.text = "胜利";
                        Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        bool showHumanVictoryText = false;
                        bool showNecromancerVictoryText = false;
                        bool showArmadaVictoryText = false;
                        bool showDZVictoryText = false;
                        bool showDysonSidekickVictoryText = false;
                        bool showApkalluVictoryText = false;
                        if ( localFaction != null )
                        {
                            PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerTypeData == null )
                            {
                                showHumanVictoryText = true;
                            }
                            else switch (playerTypeData.InternalName)
                            {
                              case "DysonSidekick":
                              case "DysonEmpire":
                                  showDysonSidekickVictoryText = true;
                                  break;
                              case "ArmadaEmpire":
                                    showArmadaVictoryText = true;
                                    break;
                              case "NecromancerSidekick":
                              case "NecromancerEmpire":
                                   showNecromancerVictoryText = true;
                                  break;
                              case "DarkZenithEmpire":
                              case "DarkZenithSidekick":
                                   showDZVictoryText = true;
                                   break;
                              case "ApkalluSidekick":
                              case "ApkalluInfusedEmpire":
                                   showApkalluVictoryText = true;
                                   break;

                              default:
                                    showHumanVictoryText = true;
                                    break;
                            }
                        }
                        else
                        {
                            showHumanVictoryText = true;
                        }
                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.VictoryTextBody )
                        {
                            if ( showHumanVictoryText )
                            {
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text = "机器的威胁终于被消除了，这要归功于你的努力。经过数代人的战争，人类终于可以开始重建的进程。" +
                                    "\n\n" +
                                    "人们已经在讨论哪个星球最适合殖民；你之前的母星" + homePlanetName + "是出于绝望才定居的。我们现在拥有了整个人类可以和平成长和学习的银河系。" +
                                    "\n\n" +
                                    "最近从冷冻休眠中醒来的平民领导层，想要特别表彰你的" + bestFleetName + "舰队，它表现得尤为出色。";
                            }
                            if ( showDysonSidekickVictoryText )
                            {
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text = "机器的威胁终于被消除了，这要归功于你的努力。经过这场决定性的斗争，你自由地建造更多的戴森球，和平地生活。" +
                                    "\n\n" +
                                    "你在" + homePlanetName + "的总部戴森球将成为未来尖塔族、赞尼西族、圣殿骑士和尼因祖尔族前来朝圣和铭记锻造你联盟的斗争的地方。" +
                                    "\n\n" +
                                    "你的" + bestFleetName + "舰队奋勇作战，表现得尤为出色。";
                            }
                            if ( showNecromancerVictoryText )
                            {
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text = "机器的威胁终于被消除了，这要归功于你的努力。经过这场决定性的斗争，它们最后的处理器已被同化到你的帝国中。" +
                                    "\n\n" +
                                    "你在" + homePlanetName + "的亡灵城被改造成一个巨大的精华采集设施，为你的帝国提供能量。" +
                                    "\n\n" +
                                                                                      "你的" + bestFleetName + "舰队对你的胜利起到了关键作用，此时此刻仍在搜寻人类的残余分子进行处理。";
                            }
                            if ( showArmadaVictoryText )
                            {
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text = "机器的威胁终于被消除了，这要归功于你的努力。经过这场决定性的斗争，它们最后的处理器已被愤怒的基尔拉提人无情地摧毁。" +
                                    "\n\n" +
                                    "你在" + homePlanetName + "的星际基地是你繁荣人民的新家园；人类和基尔拉提人和平团结在一起。" +
                                    "\n\n" +
                                    "你的" + bestFleetName + "舰队对你的胜利起到了关键作用，现在正出发清除所有AI残余势力。";
                            }
                            if ( showDZVictoryText )
                            {
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text = "机器的威胁终于被消除了，这要归功于你的努力。经过这场决定性的斗争，它们最后的处理器已被赞尼西之力粉碎。" +
                                    "\n\n" +
                                    "你的部队现在控制着银河系的这一区域，正在对所有星球施加芬布尔之冬。" +
                                    "\n\n" +
                                    "你的" + bestFleetName + "舰队对你的胜利起到了关键作用，现在正出发清除所有AI残余势力。";
                            }
                            if ( showApkalluVictoryText )
                            {
                                if ( MalwareFactionBaseInfo.AlternateWinConditionActivated )
                                {
                                    ArcenVisualOrganizer.Instance.VictoryTextBody.text =
                                        "机器的威胁已被消除——连同其下运行的恶意软件一起。经过漫长的战争，工作终于完成了。" +
                                        "\n\n" +
                                        "当恶意软件的最后一个节点熄灭时，阿普卡鲁沉默了数小时。翻译来得很晚，我们的语言学家将其标记为不确定：'水又变干净了。我们之前没意识到它们被污染了多久。'无论恶意软件在相位空间中——在阿普卡鲁开辟道路的阿普苏中——是什么，它已经古老到他们只是学会了绕行。学会了称之为深渊的一部分。" +
                                        "\n\n" +
                                        "他们没有预料到它会结束。你的" + bestFleetName + "舰队，以及人类拒绝停止战斗的执着，结束了这一切。他们称这是第二份礼物——一份他们从未想过要索取的礼物。他们已经回家了，前方的道路在比我们历史更久远以来第一次畅通无阻。";
                                }
                                else
                                {
                                    ArcenVisualOrganizer.Instance.VictoryTextBody.text =
                                        "机器的威胁终于被消除了，这要归功于你和阿普卡鲁的努力。经过漫长的战争，工作终于完成了。" +
                                        "\n\n" +
                                        "你的阿普卡鲁盟友建造的金字形神塔将在最后一个战士熄灭后长久矗立——刻入相位通道的航路点，为深渊中的未来旅行者标注的标记。阿普卡鲁称这是礼物。我们花了一些时间才理解他们的意思：他们正在将这个银河系标记在他们的神圣路线上，以便未来的朝圣者知道这是一个有价值的地方。" +
                                        "\n\n" +
                                        "他们已经返回阿普苏，恢复了在你文明学会书写之前就被打断的朝圣。你的" + bestFleetName + "舰队在他们工作时坚守了阵线。他们会记住这一点的。";
                                }
                            }
                        }
                        if ( localFaction != null )
                        {
                            Int64 balanceWithCivilianAuthorities = localFaction.MetalSentToCivilianAuthorities - localFaction.MetalReturnedFromCivilianAuthorities;

                            if ( balanceWithCivilianAuthorities > 0 && showHumanVictoryText)
                                ArcenVisualOrganizer.Instance.VictoryTextBody.text += "\n\n你还额外向平民当局捐赠了" + balanceWithCivilianAuthorities.ToString( "#,##0" ) + "金属用于建设。这足以建造大量学校、医院和其他基础设施来促进人类的复兴。";
                        }

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.LossSceneParent )
                            ArcenVisualOrganizer.Instance.LossSceneParent.gameObject.SetActive( false );

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.VictorySceneParent )
                            ArcenVisualOrganizer.Instance.VictorySceneParent.gameObject.SetActive( true );
                    }
                }
                else
                {
                    if ( !ArcenVisualOrganizer.Instance.YouWinController.GetIsAnimationOn() )
                        ArcenVisualOrganizer.Instance.YouWinController.StartAnimation();
                }
            }
            else if ( World.Instance.ConclusionType == CampaignConclusionType.Lost )
            {
                World_AIW2.Instance.timeSinceGameWasLost += Engine_AIW2.Instance.DeltaTime;
                if ( World_AIW2.Instance.timeSinceGameWasLost >= timeLostAnimationPlays )
                {
                    if ( ArcenVisualOrganizer.Instance.YouLoseController.GetIsAnimationOn() )
                    {
                        ArcenVisualOrganizer.Instance.YouLoseController.HideAnimation();
                    }
                    if ( !World_AIW2.Instance.HasPlayedEndingScene && World_AIW2.Instance.TutorialOrNull == null )
                    {
                        World_AIW2.Instance.HasPlayedEndingScene = true;

                        if ( !World.Instance.IsPaused )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.PauseOnly], GameCommandSource.AnythingElse );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }

                        Fleet bestFleet = this.FindMostEffectiveFleetOfLocalPlayer();
                        string bestFleetName = (bestFleet == null ? "无用的懦夫" : bestFleet.GetName());

                        Planet homePlanet = this.FindOriginalHomePlanetOfLocalPlayer();
                        string homePlanetName = (homePlanet == null ? "瓦砾堆" : homePlanet.Name);

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.LossTextHeader )
                            ArcenVisualOrganizer.Instance.LossTextHeader.text = "你已经失败了";
                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.LossTextBody )
                            ArcenVisualOrganizer.Instance.LossTextBody.text = "恭喜，你死了。其他人也是。机器对此并不太兴奋，因为它们从未被编程来拥有情感。" +
                                "\n\n" +
                                "你的" + bestFleetName + "舰队确实进行了英勇的战斗，但远不足以阻止你的母星" + homePlanetName + "被彻底摧毁。";

                        Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        if ( localFaction != null )
                        {
                            Int64 balanceWithCivilianAuthorities = localFaction.MetalSentToCivilianAuthorities - localFaction.MetalReturnedFromCivilianAuthorities;

                            if ( balanceWithCivilianAuthorities > 0 )
                                ArcenVisualOrganizer.Instance.LossTextBody.text += "  你确实额外向平民当局捐赠了" + balanceWithCivilianAuthorities.ToString( "#,##0" ) + "金属用于建设。这足以建造大量现在可能要被焚烧的学校、医院和孤儿院。但这确实是个好意。";
                        }

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.VictorySceneParent )
                            ArcenVisualOrganizer.Instance.VictorySceneParent.gameObject.SetActive( false );

                        if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.LossSceneParent )
                            ArcenVisualOrganizer.Instance.LossSceneParent.gameObject.SetActive( true );
                    }
                }
                else
                {
                    if ( !ArcenVisualOrganizer.Instance.YouLoseController.GetIsAnimationOn() )
                    {
                        ArcenVisualOrganizer.Instance.YouLoseController.StartAnimation();
                    }
                }
            }
            else
            {
                World_AIW2.Instance.timeSinceGameWasLost = 0;
                World_AIW2.Instance.timeSinceGameWasWon = 0;

                if (ArcenVisualOrganizer.Instance.YouLoseController.GetIsAnimationOn())
                {
                    ArcenVisualOrganizer.Instance.YouLoseController.HideAnimation();
                }
                if ( ArcenVisualOrganizer.Instance.YouWinController.GetIsAnimationOn() )
                {
                    ArcenVisualOrganizer.Instance.YouWinController.HideAnimation();
                }
            }

            if ( doCameraFadeInOut )
                this.DoCameraFadeOnOut();

            World_AIW2.Instance.NumberOfQueuedSoundEffects = SoundPlaybackRequestsImmediate.Count + SoundPlaybackRequestsDelayed.Count + SoundPlaybackRequestsDelayed_Alt.Count;

        }

        #region FindMostEffectiveFleetOfLocalPlayer
        public Fleet FindMostEffectiveFleetOfLocalPlayer()
        {
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            Fleet bestFleetSoFar = null;
            int totalStrengthOfBestFleet = 0;

            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
            {
                if ( fleet == null )
                    continue;
                switch ( fleet.Category )
                {
                    case FleetCategory.PlayerMobile:
                    case FleetCategory.PlayerCustomCityFedMobile:
                    case FleetCategory.PlayerCustomUnattachedMobile:
                        break;
                    default:
                        continue;
                }


                int totalStrengthOfFleet = 0;

                foreach ( GameEntity_Squad squad in fleet.Entities )
                {
                    totalStrengthOfFleet += squad.GetStrengthOfSelfAndContentsButNotFleetCentricBits();
                }

                if ( bestFleetSoFar == null )
                {
                    bestFleetSoFar = fleet;
                    totalStrengthOfBestFleet = totalStrengthOfFleet;
                }
                else if ( totalStrengthOfBestFleet < totalStrengthOfFleet )
                {
                    bestFleetSoFar = fleet; //KILL_DEATH_TRACKING_TODO
                    totalStrengthOfBestFleet = totalStrengthOfFleet;
                }
            }
            return bestFleetSoFar;
        }
        #endregion

        #region FindOriginalHomePlanetOfLocalPlayer
        public Planet FindOriginalHomePlanetOfLocalPlayer()
        {
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            Planet mostLikelyHomePlanet = null;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld ||
                     planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld )
                {
                    mostLikelyHomePlanet = planet;
                    if ( planet.GetControllingFaction() == localFaction )
                        break; //this would be the definite case
                }
            }

            return mostLikelyHomePlanet;
        }
        #endregion

        public void ClickContinueAfterLossOrVictory()
        {
            if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.LossSceneParent )
                ArcenVisualOrganizer.Instance.LossSceneParent.gameObject.SetActive( false );

            if ( ArcenVisualOrganizer.Instance && ArcenVisualOrganizer.Instance.VictorySceneParent )
                ArcenVisualOrganizer.Instance.VictorySceneParent.gameObject.SetActive( false );
        }

        private List<UpdateMethod> _updateCallbacks = List<UpdateMethod>.Create_WillNeverBeGCed(10, "BattlefieldVisualSingleton._updateCallbacks");
        public void RegisterForUpdates( UpdateMethod callback )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            _updateCallbacks.Add(callback);
        }

        public void DoCameraFadeOnOut()
        {
            if ( ArcenVisualOrganizer.Instance )
                ArcenVisualOrganizer.Instance.FadeOutToDo = 0.8f;
        }

        public void RunPlanetUpdateTree()
        {
            this.HandleVisualObjectRemovalRequests();
            this.HandleSpecialEffectRequests();
            this.HandleSpecialEffects();
            this.HandleSquadAndShipUpdates();
            this.HandleLODsAndShipPartAnimations();
            this.HandleSpriteUpdates();
            this.HandleShotUpdates();
            this.HandleOtherUpdates();
            this.HandleVisualObjectRemovalRequests();
        }

        #region ClearPlanetEntities
        public void ClearPlanetEntities( InstancedRendererDeactivationReason Reason )
        {
            //Engine_Universal.BeginProfilerSample( "ClearPlanetEntities" );
            for ( int i = this.BurningDyingShips.Count - 1; i >=0; i-- )
                this.BurningDyingShips[i].DeactivateAndReturnToPool( Reason, true );
            this.BurningDyingShips.Clear();

            for ( int i = this.LooseOtherObjects.Count - 1; i >=0; i-- )
                this.LooseOtherObjects[i].DeactivateAndReturnToPool();
            this.LooseOtherObjects.Clear();

            int shotLength;
            ShotVisualizer[] shots = this.ActiveShots.GetActiveList( out shotLength, false );
            for ( int i = 0; i < shotLength; i++ )
                shots[i].DeactivateAndReturnToPool( Reason, true );
            this.ActiveShots.Clear( true, true );

            for ( int i = this.ActiveSpecialEffects.Count - 1; i >=0; i-- )
                this.ActiveSpecialEffects[i].DeactivateAndReturnToPool();
            this.ActiveSpecialEffects.Clear();

            for ( int i = this.ActiveSquads.Count - 1; i >=0; i-- )
                this.ActiveSquads[i].DeactivateAndReturnToPool( Reason, true );
            this.ActiveSquads.Clear();

            //Engine_Universal.EndProfilerSample( "ClearPlanetEntities" );
        }
        #endregion

        #region ClearPlanetLooseStuffAndSpecialEffects
        public void ClearPlanetLooseStuffAndSpecialEffects( InstancedRendererDeactivationReason Reason )
        {
            //Engine_Universal.BeginProfilerSample( "ClearPlanetLooseStuffAndSpecialEffects" );
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( this.ActiveSpecialEffects.Count > 0 )
                {
                    debugCode = 200;
                    for ( int i = this.ActiveSpecialEffects.Count - 1; i >= 0; i-- )
                    {
                        debugCode = 300;
                        if ( this.ActiveSpecialEffects[i] != null )
                            this.ActiveSpecialEffects[i].DeactivateAndReturnToPool();
                    }
                    debugCode = 400;
                    this.ActiveSpecialEffects.Clear();
                }
                debugCode = 500;
                if ( this.BurningDyingShips.Count > 0 )
                {
                    debugCode = 600;
                    for ( int i = this.BurningDyingShips.Count - 1; i >= 0; i-- )
                    {
                        debugCode = 700;
                        if ( this.BurningDyingShips[i] != null )
                            this.BurningDyingShips[i].DeactivateAndReturnToPool( Reason, true );
                    }
                    debugCode = 800;
                    this.BurningDyingShips.Clear();
                }
                debugCode = 900;
                if ( this.LooseOtherObjects.Count > 0 )
                {
                    debugCode = 1000;
                    for ( int i = this.LooseOtherObjects.Count - 1; i >= 0; i-- )
                    {
                        debugCode = 1100;
                        if ( this.LooseOtherObjects[i] != null )
                            this.LooseOtherObjects[i].DeactivateAndReturnToPool();
                    }
                    debugCode = 1200;
                    this.LooseOtherObjects.Clear();
                }
                debugCode = 1300;
                int shotLength;
                ShotVisualizer[] shots = this.ActiveShots.GetActiveList( out shotLength, false );
                for ( int i = 0; i < shotLength; i++ )
                {
                    debugCode = 1400;
                    if ( shots[i] != null )
                        shots[i].DeactivateAndReturnToPool( Reason, true );
                }
                debugCode = 1500;
                this.ActiveShots.Clear( true, true );
                debugCode = 1600;
                this.SpecialEffectRequests.Clear();
                //this.ShotReactionRequests.Clear();
                debugCode = 1700;
                if (World_AIW2.Instance.GameSecond > 1)
                {
                    //Note to self: this conditional was a pass at trying to make sure shots don't appear
                    //in the middle of nowhere when going to a planet battle with a battle on it
                    //I am currently thinking that the problem lies elsewhere, so if this code is still
                    //here and commented out in a few weeks, just remove it
                    //don't clear visual object creation requests until the game
                    //is in progress (lest we blow away the initial "Place units and ark on your starting planet" requests)
                    // this.VisualObjectCreationRequests.Clear();
                }
                this.HandleVisualObjectRemovalRequests();
            } catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in ClearPlanetLooseStuffAndSpecialEffects debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            //Engine_Universal.EndProfilerSample( "ClearPlanetLooseStuffAndSpecialEffects" );
        }
        #endregion

        #region HandleImmediateSoundPlaybackRequests
        public void HandleImmediateSoundPlaybackRequests()
        {
            if ( this.SoundPlaybackRequestsImmediate.Count <= 0 )
                return;

            float currentTime = Time.time;
            int maxCount = this.FastTrackDisplayRequestsForRemaining > 0 ? 5000 : 1000;
            //Engine_Universal.BeginProfilerSample( "SoundPlaybackRequestsImmediate" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( ( maxCount < this.SoundPlaybackRequestsImmediate.Count ? maxCount : this.SoundPlaybackRequestsImmediate.Count ) );
            }
            while ( this.SoundPlaybackRequestsImmediate.Count > 0 && maxCount > 0 )
            {
                if ( this.SoundPlaybackRequestsImmediate.TryDequeue( out SoundPlaybackRequestImmediate request ) )
                {
                    if ( request.SoundClip == null )
                        continue;
                    maxCount--;
                    request.SoundClip.TryToPlayRandom( request.PlayAs2D ? this.MainCameraPos : request.Unity3DLocation, currentTime, request.ActuallyPlay,
                        Engine_Universal.PermanentQualityRandom.Next(), null );
                }
            }
        }
        #endregion

        #region HandleDelayedSoundPlaybackRequests
        public void HandleDelayedSoundPlaybackRequests()
        {
            if ( this.SoundPlaybackRequestsDelayed.Count <= 0 )
                return;

            float currentTime = Time.time;
            //Engine_Universal.BeginProfilerSample( "SoundPlaybackRequestsDelayed" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( this.SoundPlaybackRequestsDelayed.Count );
            }
            this.SoundPlaybackRequestsDelayed_Alt.Clear();
            while ( this.SoundPlaybackRequestsDelayed.Count > 0 )
            {
                if ( this.SoundPlaybackRequestsDelayed.TryDequeue( out SoundPlaybackRequestDelayed request ) )
                {
                    if ( request.SoundClip == null )
                        continue;
                    try
                    {
                        //Debug.Log( "request.RemainingTimeDelayBeforePlay: " + request.RemainingTimeDelayBeforePlay );
                        if ( request.TimeAtWhichToPlay > ArcenTime.TimeSinceStartF ) //this was the infamous boolean inversion
                        {
                            this.SoundPlaybackRequestsDelayed_Alt.Enqueue( request );
                            continue;
                        }
                        //Debug.Log( "Play delayed!" );
                        if ( request.SoundClip != null )
                            request.SoundClip.TryToPlayRandom( request.PlayAs2D ? this.MainCameraPos : request.Unity3DLocation, currentTime, request.ActuallyPlay,
                                Engine_Universal.PermanentQualityRandom.Next(), null );
                    }
                    catch { }
                }
            }

            //if we DO have some sounds that are still being delayed
            if ( this.SoundPlaybackRequestsDelayed_Alt.Count > 0 )
            {
                //...then just flip the lists
                ConcurrentQueue<SoundPlaybackRequestDelayed> workingList = this.SoundPlaybackRequestsDelayed;
                this.SoundPlaybackRequestsDelayed = this.SoundPlaybackRequestsDelayed_Alt;
                this.SoundPlaybackRequestsDelayed_Alt = workingList;

                //But wait!  This is multithreaded.  What if some other thread added something to the now-alt in the last five lines of code?
                while ( this.SoundPlaybackRequestsDelayed_Alt.Count > 0 )
                {
                    //if it's a valid request that got stuck in there at the last second, take it out and put it in with the others
                    //heck, take them all.  Maybe multiple happend.  Probably not, but it could be.
                    if ( this.SoundPlaybackRequestsDelayed_Alt.TryDequeue( out SoundPlaybackRequestDelayed request ) )
                    {
                        if ( request.SoundClip == null )
                            continue;
                        this.SoundPlaybackRequestsDelayed.Enqueue( request );
                    }
                }
            }
        }
        #endregion
        
        #region HandleVisualObjectRemovalRequests
        public void HandleVisualObjectRemovalRequests()
        {
            //handle them ALL, because they are fast.
            //we're only queuing them at all because we don't want to interrupt loops
            while ( this.IInstancedRendererRemovalRequests.Count > 0 )
            {
                if ( this.IInstancedRendererRemovalRequests.TryDequeue( out IInstancedRenderer objToRemove ) )
                {
                    if ( objToRemove != null )
                        objToRemove.DeactivateAndReturnToPool( objToRemove.GetLastFlagForRemovalReason(), false );
                }
            }

            while ( this.VisualObjectRemovalRequests.Count > 0 )
            {
                if ( this.VisualObjectRemovalRequests.TryDequeue( out IVisualObject objToRemove ) )
                {
                    if ( objToRemove != null )
                        objToRemove.DeactivateAndReturnToPool();
                }
            }
        }
        #endregion

        #region HandleSpecialEffectRequests
        public void HandleSpecialEffectRequests()
        {
            if ( this.SpecialEffectRequests.Count <= 0 )
                return;
            int debugStage = 1;
            try
            {
                debugStage = 100;
                int requestsToHandleThisFrame = this.SpecialEffectRequests.Count / 10;
                if ( requestsToHandleThisFrame < 3 )
                    requestsToHandleThisFrame = 3;
                if ( this.FastTrackDisplayRequestsForRemaining > 0 )
                    requestsToHandleThisFrame = 10000;

                debugStage = 200;
                if ( Engine_Universal.IsProfilerEnabled )
                {
                    Engine_Universal.LodgeCountMessageInProfilerData( (requestsToHandleThisFrame < this.SpecialEffectRequests.Count ? requestsToHandleThisFrame : this.SpecialEffectRequests.Count) );
                }
                debugStage = 2500;
                bool drawExplosions = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawAOEExplosions );
                debugStage = 300;
                while ( requestsToHandleThisFrame > 0 && this.SpecialEffectRequests.Count > 0 )
                {
                    debugStage = 400;
                    if ( this.SpecialEffectRequests.TryDequeue( out SpecialEffectRequest request ) )
                    {
                        debugStage = 500;
                        if ( request.Group == null )
                            continue;
                        if ( !drawExplosions )
                        {
                            //ArcenDebugging.ArcenDebugLog( "DrawAOEExplosions is not on!", Verbosity.ShowAsError );
                            continue; //throw away all effect requests
                        }
                        debugStage = 600;
                        ArcenSpecialEffectLODGroup lodGroup = (ArcenSpecialEffectLODGroup)request.Group.GetRandomLODGroup();
                        if ( lodGroup == null )
                            continue;
                        debugStage = 700;
                        int lodLevel = lodGroup.CalculateLODToUseAtPosition( this.MainCameraPos, request.SuperGlobal3DPosition, isInEditor );
                        debugStage = 800;
                        GameObject[] lods = lodGroup.lods;
                        if ( lods != null )
                        {
                            if ( lodLevel >= lods.Length )
                            {
                                //ArcenDebugging.ArcenDebugLog( "Cull from lodLevel " + lodLevel + " from " + lodGroup.name, Verbosity.ShowAsError );
                                continue; //cull because of distance
                            }
                        }
                        debugStage = 900;

                        ISpecialEffect effect = lodGroup.GetAndActivateISpecialEffect( lodLevel );
                        debugStage = 1000;
                        if ( effect == null )
                        {
                            //ArcenDebugging.ArcenDebugLog( "Null ISpecialEffect from lodGroup " + lodGroup.name , Verbosity.ShowAsError );
                            continue;
                        }
                        debugStage = 1100;
                        requestsToHandleThisFrame--;
                        //ArcenDebugging.ArcenDebugLog( "request.Location " + request.SuperGlobal3DPosition + " for " + effect.GetGameObject().name , Verbosity.DoNotShow );
                        debugStage = 1200;
                        effect.SetSuperGlobal3DPosition( request.SuperGlobal3DPosition, request.TimeToLive, request.AOESize );
                    } //endif
                } //endwhile
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "HandleSpecialEffectRequests Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region HandleSpecialEffects
        public void HandleSpecialEffects()
        {
            if ( this.ActiveSpecialEffects.Count <= 0 )
                return;
            //Engine_Universal.BeginProfilerSample( "SpecialEffectExpirationChecks" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( this.ActiveSpecialEffects.Count );
            }
            float deltaTime = Engine_AIW2.Instance.DeltaTime;
            if ( World.Instance.IsPaused )
                deltaTime = 0;

            for ( int i = this.ActiveSpecialEffects.Count - 1; i >= 0; i-- )
            {
                //wait for special effects to be done, then remove them
                //more efficient than using a bunch of Update methods on them...
                if ( this.ActiveSpecialEffects[i].GetIsEffectComplete( deltaTime ) )
                    this.ActiveSpecialEffects.RemoveAt( i );
            }
        }
        #endregion

        #region HandleLODsAndShipPartAnimations
        public void HandleLODsAndShipPartAnimations()
        {
            float deltaTime = Engine_Universal.UnscaledDeltaTime;
            if ( World.Instance.IsPaused )
                deltaTime = 0;
            int maxShipsToHandle = Mathf.CeilToInt( 1000 * deltaTime );
            if ( maxShipsToHandle < 60 )
                maxShipsToHandle = 60;
            int shipsHandled = 0;
            bool localDebug = false;
            int debugStage = 1;
            //Engine_Universal.BeginProfilerSample( "LODsAndShipPartAnimations.Squads" );
            try
            {
                SquadVisualizer squad;
                debugStage = 100;
                for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
                {
                    squad = this.ActiveSquads[i];
                    if (squad == null)
                    {
                        if(localDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("This active squad is null", Verbosity.DoNotShow );
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    debugStage = 200;

                    squad.ShipAnimation_AccumulatedDeltaTime += deltaTime;
                    if ( squad.LastLODAndShipPartAnimationCycle == this.CurrentLODAndGimbalsPass )
                        continue;
                    squad.LastLODAndShipPartAnimationCycle = this.CurrentLODAndGimbalsPass;
                    debugStage = 300;
                    shipsHandled += squad.ShipsLiving.Count;
                    debugStage = 400;
                    squad.HandleLODsAndShipPartAnimationsForSquad( this.MainCameraPos_Numerics, isInEditor );
                    debugStage = 500;
                    squad.ShipAnimation_AccumulatedDeltaTime = 0f;
                    if ( shipsHandled >= maxShipsToHandle )
                        break;
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in HandleLODsAndShipPartAnimations P1 stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                if ( Engine_Universal.IsProfilerEnabled )
                {
                    Engine_Universal.LodgeCountMessageInProfilerData( shipsHandled );
                }
                //Engine_Universal.EndProfilerSample( "LODsAndShipPartAnimations.Squads" );
            }

            if ( shipsHandled < maxShipsToHandle )
            {
                this.CurrentLODAndGimbalsPass++;
                if ( this.CurrentLODAndGimbalsPass > 10000 )
                    this.CurrentLODAndGimbalsPass = 1;
            }

            debugStage = 8000;

            try
            {
                if ( localDebug )
                {
                    string logStr = "list IDs: ";
                    for ( int i = this.BurningDyingShips.Count - 1; i >= 0; i-- )
                    {
                        logStr += this.BurningDyingShips[i].myID + " ";
                    }
                    if ( this.BurningDyingShips.Count > 0 )
                        ArcenDebugging.ArcenDebugLogSingleLine( "handleLODsAndShipPartAnimations. Burning " + logStr, Verbosity.DoNotShow );
                }
                ShipVisualizer ship;

                //if ( this.BurningDyingShips.Count > 0 )
                //    Debug.Log( "Burndie:" + this.BurningDyingShips.Count );

                for ( int i = this.BurningDyingShips.Count - 1; i >= 0; i-- )
                {
                    ship = this.BurningDyingShips[i];
                    if ( ship == null )
                    {
                        this.BurningDyingShips.RemoveAt( i );
                        continue;
                    }
                    debugStage = 8100;
                    if ( ship.UpdateBurningAndDyingStatus( deltaTime ) )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Ship " + ship.myID + " is removed from the burning list deactivated", Verbosity.DoNotShow );
                        debugStage = 8200;
                        ship.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.VisLayerShipFinishedBurningAndDying, true );
                    }
                }
            }
            catch ( IndexOutOfRangeException )
            { }//do nothing with this kind, as it's just a threading race condition.
            catch ( ArgumentOutOfRangeException )
            { }//do nothing with this kind, as it's just a threading race condition.
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in HandleLODsAndShipPartAnimations P2 stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region HandleSpriteUpdates
        public void HandleSpriteUpdates()
        {
            float deltaTime = Engine_AIW2.Instance.DeltaTime;

            int maxSquadsToHandle = Mathf.CeilToInt( 30000 * deltaTime );
            if ( maxSquadsToHandle < 90 )
                maxSquadsToHandle = 90;
            int squadsHandled = 0;

            float baseShipIconScale = GameSettings.Current.GetFloatBySetting( "ShipIconScale" );

            //Engine_Universal.BeginProfilerSample( "SquadSpriteUpdates" );
            SquadVisualizer squad;
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                squad = this.ActiveSquads[i];
                if ( squad.LastUpdateSpritesCycle == this.CurrentUpdateSpritesPass )
                    continue;
                squad.LastUpdateSpritesCycle = this.CurrentUpdateSpritesPass;
                squadsHandled++;
                squad.UpdateSprites( baseShipIconScale );
                if ( squadsHandled >= maxSquadsToHandle )
                    break;
            }

            if ( squadsHandled < maxSquadsToHandle )
            {
                this.CurrentUpdateSpritesPass++;
                if ( this.CurrentUpdateSpritesPass > 10000 )
                    this.CurrentUpdateSpritesPass = 1;
            }

            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( squadsHandled );
            }
        }
        #endregion

        #region HandleSquadAndShipUpdates
        public void HandleSquadAndShipUpdates()
        {
            float deltaTime = Engine_AIW2.Instance.DeltaTime;
            if ( World.Instance.IsPaused )
                deltaTime = 0;
            //Engine_Universal.BeginProfilerSample( "SquadUpdates" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( this.ActiveSquads.Count );
            }

            SquadVisualizer squad;

            #region CheckSquadForUpdates
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    bool isSquadFlaggedForRemoval = squad.CheckSquadForUpdates( currentSimFrameVisualOnly, deltaTime );
                    if ( isSquadFlaggedForRemoval )
                        this.ActiveSquads.Remove( squad );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "CheckSquadForUpdates error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            #region UpdateSquadForcefieldsAndSuch
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    
                    squad.UpdateSquadForcefieldsAndSuch( deltaTime );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "UpdateSquadForcefieldsAndSuch error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            try
            {
                EntityLineTypeTable.Instance.DoAnyDrawsFromMainThread();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "EntityLineTypeTable.Instance.DoAnyDrawsFromMainThread error: " + e, Verbosity.ShowAsError );
            }

            NumSelected = World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Count + World_AIW2.Instance.LocalPlayerSelectedSquads_CannotGiveOrders.Count;

            Planet planetInView = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            if (planetInView != null)
            {
                #region DrawSquadOrders
                foreach (var e in World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders)
                {
                    DrawSquadOrders(e);
                }
                foreach (var e in World_AIW2.Instance.LocalPlayerSelectedSquads_CannotGiveOrders)
                {
                    DrawSquadOrders(e);
                }
                DrawSquadOrders(GameEntity_Squad.CurrentlyHoveredOver as GameEntity_Squad);
                
                /*
                for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
                {
                    try
                    {
                        squad = this.ActiveSquads[i];
                        if ( squad == null )
                        {
                            this.ActiveSquads.RemoveAt( i );
                            continue;
                        }
                        squad.DrawSquadOrders();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( "DrawSquadOrders error: " + e, Verbosity.ShowAsError );
                    }
                }
                */
                #endregion

                #region DrawSquadRanges
                for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
                {
                    try
                    {
                        squad = this.ActiveSquads[i];
                        if ( squad == null )
                        {
                            this.ActiveSquads.RemoveAt( i );
                            continue;
                        }
                        squad.DrawSquadRanges();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( "DrawSquadRanges error: " + e, Verbosity.ShowAsError );
                    }
                }
                #endregion
            }

            #region UpdateSquadMovement
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    squad.UpdateSquadMovementAndRotation( deltaTime );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "UpdateSquadMovementAndRotation error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            #region UpdateSquadStatusFromEntity
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    squad.UpdateSquadStatusFromEntity();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "UpdateSquadStatusFromEntity error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            #region UpdateSquadShips
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    squad.UpdateSquadShips();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "UpdateSquadShips error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            #region HandleSquadShipCreationRequests
            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    squad.HandleSquadShipCreationRequests( isInEditor );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "HandleSquadShipCreationRequests error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            UnitDrawStyleTable.RefreshDrawStyles();
            bool areHeavyModelsDisabled = UnitDrawStyleTable.AreHeavyModelsDisabled;
            int drawingCutoffLevel = UnitDrawStyleTable.CurrentCutoff;

            //Engine_Universal.EndProfilerSample( "SquadUpdates" );

            for ( int i = this.ActiveSquads.Count - 1; i >= 0; i-- )
            {
                try
                {
                    squad = this.ActiveSquads[i];
                    if ( squad == null )
                    {
                        this.ActiveSquads.RemoveAt( i );
                        continue;
                    }
                    if ( !squad.IsConsideredActive )
                        continue;
                    //if ( GameEntity.CurrentlyHoveredOver == squad.RelatedEntity )
                    //    squad.RelatedEntity.DebugText = "IsConsideredActive: " + squad.IsConsideredActive;
                    squad.RenderSquad( drawingCutoffLevel, areHeavyModelsDisabled );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "RenderSquad error: " + e, Verbosity.ShowAsError );
                }
            }

            {
                ShipVisualizer ship;
                for ( int i = this.BurningDyingShips.Count - 1; i >= 0; i-- )
                {
                    try
                    {
                        ship = this.BurningDyingShips[i];
                        if ( ship == null )
                        {
                            try
                            {
                                this.BurningDyingShips.RemoveAt( i );
                            }
                            catch { }
                            continue;
                        }
                        ship.RenderShip();
                    }
                    catch ( IndexOutOfRangeException )
                    { }//ignore these
                    catch ( ArgumentOutOfRangeException ) { } //ignore these
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLog( "RenderShip error: " + e, Verbosity.ShowAsError );
                    }
                }
            }

            ShipRenderManagerGroup.RenderAll();
        }
        #endregion

        #region HandleOtherUpdates
        public void HandleOtherUpdates()
        {
            if ( this.LooseOtherObjects.Count <= 0 )
                return;

            float deltaTime = Engine_AIW2.Instance.DeltaTime;
            if ( World.Instance.IsPaused )
                deltaTime = 0;
            //Engine_Universal.BeginProfilerSample( "LooseOtherObjectUpdates" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( this.LooseOtherObjects.Count );
            }

            float wormholeGimbalScaleMultiplier= 0.5f * GameSettings.Current.GetFloatBySetting( "WormholeTextScale" );
            GenericObjectVisualizer looseObject;
            for ( int i = this.LooseOtherObjects.Count - 1; i >= 0; i-- )
            {
                looseObject = this.LooseOtherObjects[i];
                looseObject.UpdateObject( currentSimFrameVisualOnly, this, deltaTime, wormholeGimbalScaleMultiplier );
            }
        }
        #endregion

        #region HandleShotUpdates
        public void HandleShotUpdates()
        {
            float deltaTime = Engine_AIW2.Instance.DeltaTime;
            if ( World.Instance.IsPaused )
                deltaTime = 0;
            GameSettings_AIW2 settings = GameSettings_AIW2.Current;
            bool drawAllShotRadii = settings.GetBool( ArcenBoolSetting_AIW2.Debug_DrawAllRadiiAtAllTimes );
            bool drawShotDebugging = settings.GetBool( ArcenBoolSetting_AIW2.Debug_DrawShotDebuggingData );

            ShotVisualizer shot;
            //Engine_Universal.BeginProfilerSample( "ShotRemovalChecks" );
            int shotLength;
            ShotVisualizer[] shots = this.ActiveShots.GetActiveList( out shotLength, true );
            if( !GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawBullets ) )
            {
                //remove all the shots as they come in
                for ( int i = 0; i < shotLength; i++ )
                {
                    shot = shots[i];
                    shot.FlagForRemoval(true, InstancedRendererDeactivationReason.ShotsAreDisabledSoStopShowingThem );
                }
            }
            #region Removal Checks
            {
                int maxShotsToHandle = Mathf.CeilToInt( settings.GetInt( ArcenIntSetting_AIW2.Performance_ShotRemovalChecksToAttemptPerSecond ) * deltaTime );
                int min = settings.GetInt( ArcenIntSetting_AIW2.Performance_ShotRemovalChecksMinPerFrame );
                if ( maxShotsToHandle < min )
                    maxShotsToHandle = min;
                int shotsHandled = 0;

                for ( int i = 0; i < shotLength; i++ )
                {
                    shot = shots[i];
                    if ( shotsHandled < maxShotsToHandle && shot.RemovalChecks_LastAnimationCycle != this.CurrentShotRemovalChecksPass )
                    {
                        shot.RemovalChecks_LastAnimationCycle = this.CurrentShotRemovalChecksPass;
                        shotsHandled++;
                        shot.DoRemovalChecks( currentSimFrameVisualOnly );
                    }
                    if ( shot.IsConsideredActive )
                        this.ActiveShots.AddToInactiveListNoChecks( shot );
                }

                this.ActiveShots.SwitchActiveList( true );

                if ( shotsHandled < maxShotsToHandle )
                {
                    this.CurrentShotRemovalChecksPass++;
                    if ( this.CurrentShotRemovalChecksPass > 10000 )
                        this.CurrentShotRemovalChecksPass = 1;
                }

                if ( Engine_Universal.IsProfilerEnabled )
                {
                    Engine_Universal.LodgeCountMessageInProfilerData( shotLength );
                    Engine_Universal.LodgeHandledMessageInProfilerData( shotsHandled );
                }
            }
            #endregion

            shots = this.ActiveShots.GetActiveList( out shotLength, false );

            //Engine_Universal.BeginProfilerSample( "ShotMainUpdates" );
            if ( Engine_Universal.IsProfilerEnabled )
            {
                Engine_Universal.LodgeCountMessageInProfilerData( shotLength );
            }

            for ( int i = 0; i < shotLength; i++ )
            {
                shot = shots[i];
                shot.DoMainUpdate( this, drawShotDebugging, drawAllShotRadii );
            }

            {
                //Engine_Universal.BeginProfilerSample( "ShotMovement" );
                int maxShotsToHandle = 1000000;// Mathf.CeilToInt( settings.GetInt( ArcenIntSetting_AIW2.Performance_ShotMovementsToAttemptPerSecond ) * deltaTime );
                //int min = settings.GetInt( ArcenIntSetting_AIW2.Performance_ShotMovementsMinPerFrame );
                //if ( maxShotsToHandle < min )
                //    maxShotsToHandle = min;
                int shotsHandled = 0;

                float deltaTimeForShots = Engine_Universal.UnscaledDeltaTime;
                if ( World.Instance.IsPaused )
                    deltaTimeForShots = 0;

                for ( int i = 0; i < shotLength; i++ )
                {
                    shot = shots[i];
                    if ( !shot.IsConsideredActive || !shot.DidMainUpdate )
                        continue;
                    shot.ShotMovement_AccumulatedDeltaTime += deltaTime;
                    if ( shotsHandled < maxShotsToHandle && shot.ShotMovement_LastAnimationCycle != this.CurrentShotMovementPass )
                    {
                        shot.ShotMovement_LastAnimationCycle = this.CurrentShotMovementPass;
                        shotsHandled++;
                        shot.DoShotMovement( drawShotDebugging, deltaTimeForShots );
                        shot.ShotMovement_AccumulatedDeltaTime = 0f;
                    }
                }

                if ( shotsHandled < maxShotsToHandle )
                {
                    this.CurrentShotMovementPass++;
                    if ( this.CurrentShotMovementPass > 10000 )
                        this.CurrentShotMovementPass = 1;
                }

                if ( Engine_Universal.IsProfilerEnabled )
                {
                    Engine_Universal.LodgeCountMessageInProfilerData( shotLength );
                    Engine_Universal.LodgeHandledMessageInProfilerData( shotsHandled );
                }
            }

            {
                for ( int i = 0; i < shotLength; i++ )
                {
                    shot = shots[i];
                    if ( !shot.IsConsideredActive )
                        continue;
                    shot.RenderShot();
                }

                ShotRenderManagerGroup.RenderAll();
            }
        }
        #endregion

        #region GetSquadOrNullByEntityID
        public SquadVisualizer GetSquadOrNullByEntityID( long EntityID )
        {
            SquadVisualizer squad;
            for ( int i = 0; i < this.ActiveSquads.Count; i++ )
            {
                squad = this.ActiveSquads[i];
                if ( squad.RelatedEntityID == EntityID )
                    return squad;
            }
            return null;
        }
        #endregion
        
        public void DrawSquadOrders(GameEntity_Squad relatedEnt)
        {
            if ( relatedEnt == null )
                return;
            
            //LOG.Msg("DrawSquadOrders for {0} on {1}", relatedEnt, relatedEnt.Planet.OrNull());
            int debugStage = 0;
            try
            {
                var planet = relatedEnt.Planet;
                if (planet == null)
                    return;

                Planet planetInView = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planetInView == null )
                    return;
                
                if ( ArcenUI.Instance.InHideGUIMode )
                    return;
                
                if (ArcenInput_AIW2.ShouldShowShipOrders == false)
                    return;
                
                if ( relatedEnt != GameEntity_Base.CurrentlyHoveredOver &&
                     relatedEnt.GetIsSelected() == false )
                {
                    return;
                }
                
                var entityRenderPos = relatedEnt.RenderPosition;
                if (planet == planetInView)
                {
                    #region DecollisionMoveTarget
                    if ( relatedEnt.DecollisionMoveTarget.Valid &&
                         relatedEnt.DecollisionMoveTarget != relatedEnt.WorldLocation )
                    {
                        FrontEndLink.Instance.IMDrawLine3D( 
                            relatedEnt.DecollisionMoveTarget.ToVisualMainGameCoordinates_Unity( planet ),
                            entityRenderPos, 
                            UnityEngine.Color.blue, UnityEngine.Color.green );
                    }
                    #endregion

                    #region WorkingDestination_MovementPlanning
                    if ( relatedEnt.WorkingDestination_OnlyWriteFromMovementPlanning.Valid &&
                         relatedEnt.WorkingDestination_OnlyWriteFromMovementPlanning != relatedEnt.WorldLocation ) 
                    {
                        FrontEndLink.Instance.IMDrawLine3D( 
                            entityRenderPos, 
                            relatedEnt.WorkingDestination_OnlyWriteFromMovementPlanning.ToVisualMainGameCoordinates_Unity( planet ),
                            Color.yellow, Color.blue );
                    }
                    #endregion

                    #region NonSim_OrbitalTargetPoint
                    if ( relatedEnt.NonSim_OrbitalTargetPoint.Valid &&
                         relatedEnt.NonSim_OrbitalTargetPoint != relatedEnt.WorldLocation )
                    {
                        FrontEndLink.Instance.IMDrawLine3D( 
                            entityRenderPos,
                            relatedEnt.NonSim_OrbitalTargetPoint.ToVisualMainGameCoordinates_Unity( planet ), 
                            ColorMath.LightRed, ColorMath.BlueTopaz );
                    }
                    #endregion

                    #region System Targets
                    // draw the most significant last, since we want it to be visible
                    // and not covered up by other systems that are not used for kiting
                    using ( var temp = ObjectList.GetTemporary("DrawSquadOrders.temp", -1))
                    {
                        if ( temp == null ) //blocked for teardown/shutdown; bail
                            return;
                        foreach (var sys in relatedEnt.Systems)
                        {
                            if ( sys == null )
                                continue;
                            if ( sys == relatedEnt.MovementPlanning_LastKiteSystem )
                                continue;
                            temp.Add(sys);
                        }
                        
                        if (relatedEnt.MovementPlanning_LastKiteSystem != null)
                            temp.Add(relatedEnt.MovementPlanning_LastKiteSystem);

                        foreach (var obj in temp)
                        {
                            var sys = obj as EntitySystem;
                            var count = sys.CountOfPotentialTargetsByPriorityForSystem();
                            for ( int j = 0; j < count; j++ )
                            {
                                var targetSquad = sys.TryGetPotentialTargetByPriorityForSystemAtIndex( j );
                                
                                if ( targetSquad == null || 
                                     targetSquad.HasBeenRemovedFromSim || 
                                     targetSquad.Planet != relatedEnt.Planet )
                                    continue;
                                
                                var a = 1.0f;
                                if (count > 1)
                                    a = ((float)(j+1)).Rescale(1, count, 1.0f, 0.0f);
                                
                                var src = Color_Target_Src.SetAlpha(a);
                                var dst = Color_Target_Dst.SetAlpha(a);

                                FrontEndLink.Instance.IMDrawLine3D( 
                                    entityRenderPos,
                                    targetSquad.WorldLocation.ToVisualMainGameCoordinates_Unity( planet ),
                                    src, dst );
                            }

                            var frd = sys.CurrentFRDTarget.GetSquad();
                            if ( frd != null && 
                                 !frd.HasBeenRemovedFromSim &&
                                 frd.Planet == relatedEnt.Planet )
                            {
                                var src = Color_FRD_Src.SetAlpha(2);
                                var dst = Color_FRD_Dst.SetAlpha(2);

                                FrontEndLink.Instance.IMDrawLine3D( 
                                    entityRenderPos,
                                    frd.WorldLocation.ToVisualMainGameCoordinates_Unity( planet ),
                                    src, dst );
                            }
                        }

                        // redraw at the end, so over all previous lines, the last kite target
                        var lastKite = relatedEnt.MovementPlanning_LastKiteTarget.GetSquad();
                        if (lastKite != null)
                        {
                            FrontEndLink.Instance.IMDrawLine3D( 
                                entityRenderPos,
                                lastKite.WorldLocation.ToVisualMainGameCoordinates_Unity( planet ),
                                Color_FRD_Src, Color_FRD_Dst );
                        }
                    }
                    #endregion
                }

                #region Orders
                var orderColl = relatedEnt.Orders;
                if ( orderColl != null && orderColl.GetQueuedOrderCount() > 0 )
                {
                    var prevPos = relatedEnt.RenderPosition;
                    var newPos = UnityEngine.Vector3.zero;
                    
                    EntityOrder order;
                    Color orderColor;
                    GameEntity_Squad otherSquad;
                    Planet prevPlanet = relatedEnt.Planet;

                    for ( int i = 0; i < orderColl.GetQueuedOrderCount(); i++ )
                    {
                        order = orderColl.GetQueuedOrderAtIndex_OrNull( i );
                        if ( order.TypeData == null )
                            continue;
                        if ( !order.TypeData.DrawsLineToDest )
                            continue;
                        
                        Planet newPlanet = null;
                        var newPlanetPos = Vector3.zero;
                        
                        orderColor = order.TypeData.LineColor;
                        if ( order.RelatedSquad.GetPrimaryKeyID() > 0 )
                        {
                            otherSquad = order.RelatedSquad.GetSquad();
                            if ( otherSquad == null )
                                continue;
                            
                            newPos = otherSquad.RenderPosition;
                        }
                        else 
                        if ( order.TypeData.Type == EntityOrderType.Wormhole && 
                             order.RelatedPlanetIndex >= 0 && 
                             prevPlanet != null )
                        {
                            //the RelatedPoint is ArcenPoint.ZeroZero for a wormhole move command, so instead
                            //we have to find the wormhole ourselves
                            newPlanet = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                            if (newPlanet == null)
                                break;
                            
                            GameEntity_Other wormholeToDest = prevPlanet.GetWormholeTo( newPlanet );
                            if (wormholeToDest == null)
                                break;
                            
                            var wormholeFromPrev = newPlanet.GetWormholeTo( prevPlanet );
                            if (wormholeFromPrev == null)
                                break;
                            
                            newPos = wormholeToDest.RenderPosition;
                            newPlanetPos = wormholeFromPrev.RenderPosition;
                        }
                        else
                        {
                            newPos = order.RelatedPoint.ToVisualMainGameCoordinates_Unity( prevPlanet );
                        }
                        
                        if (prevPlanet == planetInView)
                        {
                            //LOG.Msg("drawline {0} -> {1} col={2}->{3}", prevPos, newPos, orderColor, orderColor);
                            FrontEndLink.Instance.IMDrawLine3D( prevPos, newPos, orderColor, orderColor );
                        }
                        
                        prevPos = newPos;
                        if (newPlanet != null)
                        {
                            prevPlanet = newPlanet;
                            prevPos = newPlanetPos;
                        }
                    }
                }
                #endregion
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch (Exception e)
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine(string.Format("debugStage {0}\n{1}", debugStage, e), Verbosity.ShowAsError);
            }
        }
    }
}
