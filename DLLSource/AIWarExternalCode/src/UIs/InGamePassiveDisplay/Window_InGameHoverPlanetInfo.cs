
using System;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class Window_InGameHoverPlanetInfo : WindowControllerAbstractBase
    {
        public static Window_InGameHoverPlanetInfo Instance;
        public Window_InGameHoverPlanetInfo()
        {
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
            Instance = this;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return false;
            if ( bPanel.Instance == null )
                return false;
            //if ( bPanel.Instance.GetShouldBeHidden() )
            //    return false;
            return true;
        }

        public static bool IsDrawing = false;

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public ArcenUI_ImageButton MyElement;
            private SubTextGroup SubTexts;
            private string NextTextToShow = string.Empty;
            private string WrappedNextTextToShow = string.Empty;
            private string LastTextToShow = string.Empty;
            private bool NeedsToResize = true;
            private float LastRequestedWidth;
            private float LastRequestedHeight;
            private bool hasSetCanvasOffset = false;
            private bool lastTextWasForPlanetLink = false;

            public void ClearMyself()
            {
                this.LastTextToShow = string.Empty;
                this.NextTextToShow = string.Empty;
                this.NeedsToResize = true;
                lastTextWasForPlanetLink = false;
            }

            private ArcenDoubleCharacterBuffer textBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameHoverPlanetInfo-textBuffer" );

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
                if ( !this.GetShouldBeHidden() )
                {
                    textBuffer.EnsureResetForNextUpdate();
                    bool isForPlanetLink = false;
                    this.GetTextToRender( textBuffer, ref isForPlanetLink );
                    string newText = textBuffer.GetStringAndResetForNextUpdate();

                    if ( newText == null )
                        newText = string.Empty;
                    if ( newText != this.LastTextToShow )
                    {
                        this.NextTextToShow = newText;
                        this.NeedsToResize = true;
                        lastTextWasForPlanetLink = isForPlanetLink;
                    }
                }

                Window_InGameHoverPlanetInfo.Instance.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );

                this.SubTexts = _SubTexts;
                this.DoResizeIfNeeded();
            }

            //because of... complicated factors... the delay is actually amplified more than we would expect based on actual time.
            //this lets us adjust back down to closer to the correct timing expectation, but it's still not perfect
            private const float OVERALL_CORRECTION_MULITPLIER = 0.2f;

            private float lastTimeWasHiddenByMerits = 0;

            public override bool GetShouldBeHidden()
            {
                #region Try Hovering Over Wormholes
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                {
                    if ( GameEntity_Base.CurrentlyHoveredOver != null )
                    {
                        GameEntityTypeData typeData = GameEntity_Base.CurrentlyHoveredOver?.TypeData;
                        if ( typeData != null && typeData.Category == GameEntityCategory.NaturalObject )
                        {
                            //this is a wormhole!
                            GameEntity_Other other = GameEntity_Base.CurrentlyHoveredOver as GameEntity_Other;
                            if ( other != null )
                            {
                                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                                    return true;

                                Planet otherPlanet = World_AIW2.Instance.GetPlanetByIndex( other.LinkedPlanetIndex );
                                if ( otherPlanet != null )
                                {
                                    //and we found the planet on the other side!
                                    if ( ArcenTime.TimeSinceStartF - lastTimeWasHiddenByMerits < GameSettings.Current.GetFloatBySetting( "MainViewTooltipDelay" ) * OVERALL_CORRECTION_MULITPLIER )
                                        return true;
                                    return false;
                                }
                            }
                        }
                    }
                    lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                    return true;
                }
                #endregion

                if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                {
                    lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                    return true;
                }

                if ( GameEntityTypeData.CurrentlyHoveredOver != null )
                {
                    lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                    return true;
                }
                if ( GameEntity_Base.CurrentlyHoveredOver != null )
                {
                    lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                    return true;
                }
                if ( Planet.CurrentlyHoveredOver == null && GalaxyMapPlanetLink.CurrentlyHoveredOver == null )
                {
                    lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                    return true;
                }

                if ( ArcenTime.TimeSinceStartF - lastTimeWasHiddenByMerits < GameSettings.Current.GetFloatBySetting( "MainViewTooltipDelay" ) * OVERALL_CORRECTION_MULITPLIER )
                    return true;

                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return true;
                return false;
            }

            public void UpdateTextIfNeeded()
            {
                string nextText = this.WrappedNextTextToShow;
                if ( this.LastTextToShow.Length <= 0 && nextText.Length <= 0 )
                {
                    this.ClearMyself();
                    return;
                }

                try
                {
                    if ( this.LastTextToShow != nextText )
                    {
                        this.LastTextToShow = nextText;
                        ArcenDoubleCharacterBuffer buffer = this.SubTexts[0].Text.StartWritingToBuffer();
                        buffer.Add( this.LastTextToShow );
                        this.SubTexts[0].Text.FinishWritingToBuffer();
                        this.NeedsToResize = true;
                        this.DoResizeIfNeeded();
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in UpdateContentFromVolatile for the single text element:" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            private readonly SortedDictionary<string, int> workingMajorAIStructuresCounts = SortedDictionary<string, int>.Create_WillNeverBeGCed( 40, "Window_InGameHoverPlanetInfo-workingMajorAIStructuresCounts" );

            #region GetTextToRender
            public void GetTextToRender( ArcenDoubleCharacterBuffer buffer, ref bool IsForPlanetLink )
            {
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return;

                IsForPlanetLink = false;

                int debugStage = 0;
                try
                {
                    IsDrawing = false;
                    debugStage = 1;
                    debugStage = 2;
                    Planet relatedPlanet = Planet.CurrentlyHoveredOver;
                    GalaxyMapPlanetLink link = GalaxyMapPlanetLink.CurrentlyHoveredOver;
                    if ( relatedPlanet == null )
                    {
                        if ( link != null )
                        {
                            GetTextForPlanetLink( buffer, link );
                            IsForPlanetLink = true;
                            return;
                        }
                        else
                        {
                            #region Try Hovering Over Wormholes
                            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                            {
                                if ( GameEntity_Base.CurrentlyHoveredOver != null )
                                {
                                    GameEntityTypeData typeData = GameEntity_Base.CurrentlyHoveredOver?.TypeData;
                                    if ( typeData != null && typeData.Category == GameEntityCategory.NaturalObject )
                                    {
                                        //this is a wormhole!
                                        GameEntity_Other other = GameEntity_Base.CurrentlyHoveredOver as GameEntity_Other;
                                        if ( other != null )
                                        {
                                            Planet otherPlanet = World_AIW2.Instance.GetPlanetByIndex( other.LinkedPlanetIndex );
                                            if ( otherPlanet != null )
                                            {
                                                //and we found the planet on the other side!
                                                relatedPlanet = otherPlanet;
                                            }
                                            else
                                                return;
                                        }
                                        else
                                            return;
                                    }
                                    else
                                        return;
                                }
                                else
                                    return;
                            }
                            else
                                return;
                            #endregion
                        }
                    }

                    TooltipDetail detailLevel = EntityText.Detail;
                    bool isMinimalFogOfWar = AIWar2GalaxySettingQuickAccess.GalaxyMinimalFogOfWar;

                    IsDrawing = true;
                    debugStage = 3;
                    buffer.Add( "星球 <b>" );

                    debugStage = 4;
                    buffer.Add( relatedPlanet.Name );

                    debugStage = 24;
                    Faction owner = relatedPlanet.GetControllingFaction();
                    if ( owner != null && owner.Type == FactionType.AI &&
                         relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        buffer.Add( " - " );
                        relatedPlanet.MarkLevelForAIOnly.WriteStartColorHexTo( buffer );
                        buffer.Add( relatedPlanet.MarkLevelForAIOnly.Abbreviation );
                        relatedPlanet.MarkLevelForAIOnly.WriteEndColorHexTo( buffer );
                    }
                    buffer.Add( "</b>" );

                    Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;

                    debugStage = 3;
                    if ( relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        buffer.Add( "<pos=350>" );
                        if ( owner != null && owner.Type == FactionType.NaturalObject )
                        {
                            if ( relatedPlanet.PrimaryInfluencingFaction == -1 )
                                buffer.Add( "中立领土" );
                            else
                            {
                                Faction influencer = World_AIW2.Instance.GetFactionByIndex( relatedPlanet.PrimaryInfluencingFaction );
                                if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( " 所属 " );
                                else
                                    buffer.Add( "所有者：" );
                                buffer.Add( "<color=#" ).Add( influencer.FactionCenterColor.ColorHexBrighter ).Add( ">" );
                                buffer.Add( influencer.GetDisplayName() );
                                buffer.Add( "</color>" );
                            }
                        }
                        else
                        {
                            if ( owner != null && owner.SpecialFactionData != null )
                            {
                                if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( "所属 " );
                                else
                                    buffer.Add( "所有者：" );
                                buffer.Add( "<color=#" ).Add( owner.FactionCenterColor.ColorHexBrighter ).Add( ">" );
                                buffer.Add( owner.GetDisplayName() );
                                buffer.Add( "</color>" );
                            }
                        }
                    }

                    if ( relatedPlanet.PlayerNotes != null && relatedPlanet.PlayerNotes.Length > 0 )
                        buffer.Add( "\n" ).StartColor( ColorMath.Gray ).Add( "<size=70%>" ).Add( relatedPlanet.PlayerNotes ).Add( "</size></color>" );

                    debugStage = 41;
                    if ( relatedPlanet.TypeData.Type == PlanetType.Nomad )
                    {
                        buffer.Add("\n这是一个游牧星球");
                        if ( relatedPlanet.IsDisabledNomad )
                            buffer.Add(" 已损坏，不会再移动。\n");
                        else
                        {
                            Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex(relatedPlanet.NomadTargetPlanetIdx);
                            if ( targetPlanet != null )
                            {
                                string crashTimerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime(relatedPlanet.SecondsTillNomadCrashes); //moveTimerColor gets more red the closer the planet is to moving
                                buffer.Add(" 将撞向 " ).Add(targetPlanet.Name, targetPlanet.GetControllingFaction().FactionCenterColor.ColorHexBrighter).Add("，还有 " ).AddHoursAndMinutes(relatedPlanet.SecondsTillNomadCrashes, crashTimerColor ).Add( " 撞毁，摧毁两个星球。如果游牧枢纽被摧毁，游牧星球将永久停止移动");
                            }

                            int timeTillNextMove = relatedPlanet.TimeForNextMove - World_AIW2.Instance.GameSecond;
                            string moveTimerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime(timeTillNextMove); //moveTimerColor gets more red the closer the planet is to moving
                            buffer.Add("。它将在 ").Add( Engine_Universal.ToHoursAndMinutesString(timeTillNextMove), moveTimerColor).Add(" 后再次移动。 ");
                            buffer.Add("\n");
                        }
                    }
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowPlanetLocations" ) )
                    {
                        buffer.Add( "Planet is at (" ).Add( relatedPlanet.GalaxyLocation.X, "a1ffa1" ).Add(", ").Add( relatedPlanet.GalaxyLocation.Y, "a1ffa1" ).Add("). Angle ").Add( relatedPlanet.GalaxyLocation.GetAngleToDegrees(Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter).ToString() ).Add(". ");
                    }
                    if ( relatedPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    {
                        buffer.Add( "\n未探索" );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.Add( "\n我们没有这个星球的视野。占领附近的星球或入侵这个星球来获得视野。 " );
                            //buffer.Add( relatedPlanet.IntelLevel );
                        }
                    }
                    else
                    {
                        if ( relatedPlanet.IntelLevel == PlanetIntelLevel.ExploredByNaturalMeans )
                        {
                            int lastVisionTime = World_AIW2.Instance.GameSecond - relatedPlanet.GetGameSecondLastHadVision(); //Chris notes: hmm, if GetDoHumansHaveVision() gives true then this might be odd.  We shall see.
                            buffer.Add( "\n已探索" );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "：我们看到的是这个星球的旧数据；上次查看是在 " );
                                buffer.Add( lastVisionTime );
                                buffer.Add( " 秒前。" );
                            }
                            else
                            {
                                buffer.Add( " " );
                                buffer.Add( lastVisionTime );
                                buffer.Add( "秒前。" );
                            }
                        }
                        else if ( relatedPlanet.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                        {
                            int lastVisionTime = World_AIW2.Instance.GameSecond - relatedPlanet.GetGameSecondLastHadVision(); //Chris notes: hmm, if GetDoHumansHaveVision() gives true then this might be odd.  We shall see.
                            buffer.Add( "\n已探索" );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( " 通过间谍纳米机器人" );
                                buffer.Add( "：我们看到的是这个星球的旧数据；上次通过入侵探索是在 " );
                                buffer.Add( lastVisionTime );
                                buffer.Add( " 秒前。" );
                            }
                            else
                            {
                                buffer.Add( " " );
                                buffer.Add( lastVisionTime );
                                buffer.Add( "秒前。" );
                            }
                        }
                        if ( relatedPlanet.IntelLevel == PlanetIntelLevel.CurrentlyWatched )
                        {
                            buffer.Add( "\n监视中" );
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "：我们正在查看这个星球的当前数据。 " );
                        }
                        if ( relatedPlanet.IntelLevel == PlanetIntelLevel.WatchedUntilReconquered )
                        {
                            if ( relatedPlanet.GetControllingFactionType() == FactionType.AI )
                            {
                                buffer.Add( "\n监视中" );
                                if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( "：除非AI失去然后重新夺回这个星球，否则我们将看到当前数据。 " );
                            }
                            else
                            {
                                buffer.Add( "\n监视中" );
                                if ( detailLevel >= TooltipDetail.Medium ) buffer.Add( "：除非AI重新夺回这个星球，否则我们将看到当前数据。 " );
                            }
                        }
                        if ( relatedPlanet.IntelLevel == PlanetIntelLevel.PermanentlyWatched )
                        {
                            buffer.Add( "\n永久监视" );
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "：我们将始终看到这个星球的当前数据。 " );
                        }
                        debugStage = 6;

                        if ( relatedPlanet.ViewedByPlayerAccounts_DuringGame.Count > 0 )
                        {
                            bool hasDoneFirst = false;
                            for ( int i = 0; i < relatedPlanet.ViewedByPlayerAccounts_DuringGame.Count; i++ )
                            {
                                byte viewer = relatedPlanet.ViewedByPlayerAccounts_DuringGame[i];
                                if ( viewer == PlayerAccount.Local.PlayerPrimaryKeyID )
                                    continue; //don't tell us about ourself
                                if ( !hasDoneFirst )
                                {
                                    hasDoneFirst = true;
                                    buffer.Add( "\n其他人正在查看此星球：" );
                                }
                                else
                                    buffer.Add( ", " );
                                PlayerAccount account = World.Instance.GetPlayerAccountByPrimaryID( viewer );
                                buffer.StartColor( account.GetFactionCenterColor().ColorHexBrighter ).Add( account.Username );
                                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                                {
                                    if ( !account.OnServer_GetIsConnected() )
                                        buffer.Add( "（当前已断开连接）" );
                                }

                                buffer.EndColor();
                            }
                        }
                    }

                    //*******************************************************************************************************************************
                    //*******************************************************************************************************************************
                    //Absolutely Critical Planet Stuff Above This Line!
                    //*******************************************************************************************************************************
                    //*******************************************************************************************************************************

                    var displayModeOrNull = PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayModeSafe()?.Implementation;
                    if ( displayModeOrNull != null && 
                        //only do the game mode tooltips on the actual galaxy map
                        Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    {
                        //if we should skip the rest of everything!
                        if ( displayModeOrNull.GetShouldReplaceNormalPlanetTooltip() )
                        {
                            displayModeOrNull.WriteToPlanetTooltip( relatedPlanet, buffer );
                            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, true, true, null );
                            return;
                        }
                    }

                    if ( relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        debugStage = 7;
                        Faction playerFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        PlanetFaction playerPlanetFactionOrNull = playerFactionOrNull == null ? null : relatedPlanet.GetPlanetFactionForFaction( playerFactionOrNull );
                        if ( playerFactionOrNull != null && playerPlanetFactionOrNull != null )
                        {
                            if ( playerPlanetFactionOrNull.AIPLeftFromCommandStation > 0 )
                            {
                                if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( "占领这个星球将花费 " );
                                else
                                    buffer.Add( "占领成本：" );
                                buffer.Add( ( playerPlanetFactionOrNull.AIPLeftFromCommandStation + playerPlanetFactionOrNull.AIPLeftFromWarpGate ), "FF0000" ).Add( " AIP。" );
                            }
                            if ( playerPlanetFactionOrNull.GetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.DoNotPathThrough ) )
                            {
                                buffer.Add( " 你的飞船将 " ).Add( "不", "FF0000" ).Add( " 通过此星球寻路。" );
                            }
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Add( " 你可以通过右键点击屏幕左上角的星球名称来切换你的飞船是否允许通过此星球寻路。" );
                            }
                        }
                        if ( relatedPlanet.IsFimbulwintered &&
                             detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.StartColor( Color.grey );
                            buffer.Add("\n<size=80%>这个星球受到了寒冬末日的影响。暗夜天顶及其盟友的速度加快了 ").Add( ExternalConstants.Instance.FimbulwinterSpeedupPercent, "1338be" ).Add("%，他们的敌人速度减慢了 ").Add( ExternalConstants.Instance.FimbulwinterSlowdownPercent, "be3813" ).Add("%。</size>");
                            buffer.EndColor();
                        }
                        int threatStrengthInt, hostileStrengthMinusThreatInt, myTotalStrength, myMobileStrength, myAndAlliedTotalStrength, myAndAlliedMobileStrength;
                        Faction hostileFaction;
                        Faction myOrAlliedFaction;
                        Faction alliedFaction;
                        Window_InGameHoverPlanetInfo.GetPlanetFactionalData( relatedPlanet, out threatStrengthInt, out hostileStrengthMinusThreatInt, out myTotalStrength, out myMobileStrength,
                                out myAndAlliedTotalStrength, out myAndAlliedMobileStrength, out myOrAlliedFaction, out alliedFaction, out hostileFaction, isMinimalFogOfWar );

                        var myOrAlliedColor = myOrAlliedFaction?.FactionCenterColor.GetColorHexBrighter(true) ?? QuickColors.White;
                        var alliedColor = alliedFaction?.FactionCenterColor.GetColorHexBrighter(true) ?? QuickColors.White;

                        //Player strength
                        if ( myTotalStrength > 0 )
                        {
                            buffer.StartColor( myOrAlliedColor );
                            buffer.Add( "\n你的机动战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, myOrAlliedColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, myMobileStrength, true, true );
                            buffer.Add( "<pos=250>你的固定战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, myOrAlliedColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, myTotalStrength - myMobileStrength, true, true );
                            buffer.EndColor();
                        }

                        int alliedTotalStrength = myAndAlliedTotalStrength - myTotalStrength;
                        int alliedMobileStrength = myAndAlliedMobileStrength - myMobileStrength;
                        int alliedImmobileStrength = Math.Max( alliedTotalStrength - alliedMobileStrength, 0 );
                        //Allied strength
                        if ( alliedTotalStrength > 0 )
                        {
                            buffer.StartColor( alliedColor );
                            buffer.Add( "\n盟友机动战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, alliedColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, alliedMobileStrength, true, true );
                            buffer.Add( "<pos=250>盟友固定战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, alliedColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, alliedImmobileStrength, true, true );
                            buffer.EndColor();
                        }

                        //hostile strength minus thread
                        if ( hostileStrengthMinusThreatInt > 0 || threatStrengthInt > 0 )
                        {
                            string textColor;
                            if ( hostileFaction != null )
                                textColor = hostileFaction.FactionCenterColor.ColorHexBrighter;
                            else
                                textColor = "ffffff";

                            buffer.StartColor( textColor );
                            buffer.Add( "\n敌人守军战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, textColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, hostileStrengthMinusThreatInt, true, true );
                            if ( !isMinimalFogOfWar && relatedPlanet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                                buffer.Add( "?" );

                            buffer.Add( "<pos=250>敌人威胁战力：" );
                            ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, textColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, threatStrengthInt, true, true );
                            if ( !isMinimalFogOfWar && relatedPlanet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                                buffer.Add( "?" );
                            buffer.EndColor();
                        }

                        debugStage = 71;

                        if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteCounterattackInfoInPlanetTooltips" ) )
                        {
                            buffer.Add( "\nCounterattack Debug Info:" )
                                .Add( "  <color=#999999>Unspent Budget:</color> " ).AddFixedDecimal( relatedPlanet.AICounterattackUnspentBudget.ToFloatNonSim(), 2 )
                                .Add( "  <color=#999999>Timer:</color> " ).Add( relatedPlanet.AICountdownTimerForCounterattack )
                                .Add( "  <color=#999999>Would Be Stalled By:</color> " ).AddFixedDecimal( (relatedPlanet.AICounterattackToBeStalledByPlayerStrengthOf / 1000f), 3 )
                                .Add( "  <color=#999999>Player Strength Here:</color> " ).AddFixedDecimal( (relatedPlanet.PlayerStrengthHereForBlockingAICounterAttacks / 1000f), 3 )
                                .Add( "  <color=#999999>Stalled:</color> " ).Add( relatedPlanet.AICounterAttacksCurrentlyStalled ? "yes" : "no" )
                                .Add( "  <color=#999999>Required To Send:</color> " ).AddFixedDecimal( (relatedPlanet.AIStrengthRequiredToSend / 1000f), 3 )
                                .Add( "  <color=#999999>Insufficient To Send:</color> " ).Add( relatedPlanet.AICounterAttacksNotSufficientToTryToSend ? "yes" : "no" )
                                .Add( "  <color=#999999>Forces stored:</color> " ).AddFixedDecimal( (relatedPlanet.PrecalculatedAICounterattackForcesStrength / 1000f), 3 )
                                .Add( "  <color=#999999>Wave Normal:</color> " ).Add( relatedPlanet.ShipGroup_WavesFromHere_Normal == null ? "null" : relatedPlanet.ShipGroup_WavesFromHere_Normal.InternalName )
                                .Add( "  <color=#999999>Wave Guardians:</color> " ).Add( relatedPlanet.ShipGroup_WavesFromHere_Guardians == null ? "null" : relatedPlanet.ShipGroup_WavesFromHere_Guardians.InternalName )
                                .Add( "  <color=#999999>Wave Dire Guardians:</color> " ).Add( relatedPlanet.ShipGroup_WavesFromHere_DireGuardians == null ? "null" : relatedPlanet.ShipGroup_WavesFromHere_DireGuardians.InternalName )
                                ;
                        }
                        debugStage = 72;

                        if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteAIBudgetInfoInPlanetTooltips" ) )
                        {
                            buffer.Add( "\nAI Budget Debug Info:" );

                            AIDefensePlacer placer = relatedPlanet.GetCurrentDefensePlacer( Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext() );
                            if ( placer == null )
                                buffer.Add( "  <color=#990000>ERROR, NULL AIDefensePlacer.  " );
                            else if ( placer.Implementation == null )
                                buffer.Add( "  <color=#990000>ERROR, NULL AIDefensePlacer Implementation.  " );
                            else
                            {
                                Faction controllingFaction = relatedPlanet.GetControllingFaction();
                                if ( controllingFaction.Type != FactionType.AI )
                                {
                                    buffer.Add( "  <color=#990000>Not owned by an AI.  " );
                                }
                                else
                                {
                                    for ( ReinforcementType reinforcementType = ReinforcementType.None + 1; reinforcementType <= ReinforcementType.NonTurretDefense; reinforcementType++ )
                                    {
                                        int AICostPurchaseCap = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( relatedPlanet, controllingFaction, reinforcementType, false, false ).IntValue;
                                        int purchaseCostPresent = AIUtilityMethods.GetAIToPurchaseCostPresentForBudgetType( relatedPlanet, controllingFaction, reinforcementType );

                                        buffer.Add( "  <color=#999999>" ).Add( EnumNameCache.GetName( reinforcementType ) ).Add( " AI预算上限：</color> " );
                                        buffer.AddNumberMoreReadable( purchaseCostPresent );
                                        buffer.Add( "/" ).AddNumberMoreReadable( AICostPurchaseCap );
                                    }
                                }
                            }
                        }

                        if ( playerType == null || playerType.UsesMetal )
                        {
                            int asteroidCountClaimed = 0;
                            int asteroidCountTotal = 0;
                            foreach ( GameEntity_Squad producer in relatedPlanet.Squads( EntityRollupType.MetalProducers ) )
                            {
                                if ( producer.TypeData.IsAsteroidMine )
                                {
                                    asteroidCountTotal++;
                                    if ( producer.GetFactionTypeSafe() == FactionType.Player && !producer.HasNotYetBeenFullyClaimed )
                                        asteroidCountClaimed++;
                                }
                            }

                            if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                                buffer.Add( "\n小行星采矿发电厂：" ).Add( asteroidCountClaimed ).Add( "/" ).Add( asteroidCountTotal );
                            else
                                buffer.Add( "\n金属采集器：" ).Add( asteroidCountClaimed ).Add( "/" ).Add( asteroidCountTotal );
                        }
                        if ( relatedPlanet.ResourceOneRemainingForAnyPlayer > 0 )
                        {
                            buffer.Add("\n").Add( localFaction != null && localFaction.Resource1TextColorAndIcon.Length > 0 ? localFaction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon ).Add( "<pos=30>" ).Add( "昆德里拉" ).Add( "</color> " ).Add( "<pos=120>" ).AddNumberMoreReadable( relatedPlanet.ResourceOneRemainingForAnyPlayer.IntValue );
                        }

                        debugStage = 8;
                        for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                        {
                            int resourceOutput = 0;
                            string colorAndIcon = "<color=#ffee8e>";
                            string resourceName = string.Empty;
                            switch ( resource )
                            {
                                case ResourceType.Hacking:
                                    if ( playerFactionOrNull == null )
                                        continue;
                                    if ( playerType != null && playerType.UsesNecromancerHackingAndScience )
                                        continue;
                                    resourceOutput = relatedPlanet.GetHackingLeftForHumans().IntValue;
                                    if ( detailLevel < TooltipDetail.Full && resourceOutput <= 0 )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.HackingTextColorAndIcon;
                                    resourceName = "入侵";
                                    break;
                                case ResourceType.Science:
                                    if ( playerFactionOrNull == null )
                                        continue;
                                    if ( playerType != null && playerType.UsesNecromancerHackingAndScience )
                                        continue;
                                    resourceOutput = relatedPlanet.GetScienceLeftForHumans().IntValue;
                                    if ( detailLevel < TooltipDetail.Full && resourceOutput <= 0 )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.ScienceTextColorAndIcon;
                                    resourceName = "科学";
                                    break;
                                case ResourceType.Energy:
                                    if ( playerPlanetFactionOrNull == null )
                                        continue;
                                    if ( playerType != null && !playerType.UsesEnergyAndFuel )
                                        continue;
                                    foreach ( GameEntity_Squad entity in relatedPlanet.Squads( EntityRollupType.EnergyProducers ) )
                                    {
                                        if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                            continue;
                                        resourceOutput += entity.GetFullyMultipliedEnergyToProduce().IntValue;
                                    }
                                    if ( detailLevel < TooltipDetail.Full && ( playerPlanetFactionOrNull == relatedPlanet.GetControllingPlanetFaction() || resourceOutput <= 0 ) )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.EnergyTextColorAndIcon;
                                    resourceName = "能量";
                                    break;
                                case ResourceType.FuelArgon:
                                    if ( playerPlanetFactionOrNull == null || !World_AIW2.Instance.IsFuelEnabled )
                                        continue;
                                    if ( playerType != null && !playerType.UsesEnergyAndFuel )
                                        continue;
                                    foreach ( GameEntity_Squad entity in relatedPlanet.Squads( EntityRollupType.FuelArgonProducers ) )
                                    {
                                        if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                            continue;
                                        resourceOutput += entity.GetFullyMultipliedFuelArgonToProduce().IntValue;
                                    }
                                    if ( detailLevel < TooltipDetail.Full && ( playerPlanetFactionOrNull == relatedPlanet.GetControllingPlanetFaction() || resourceOutput <= 0 ) )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.FuelArgonTextColorAndIcon;
                                    resourceName = "氩气燃料";
                                    break;
                                case ResourceType.FuelRadon:
                                    if ( playerPlanetFactionOrNull == null || !World_AIW2.Instance.IsFuelEnabled )
                                        continue;
                                    if ( playerType != null && !playerType.UsesEnergyAndFuel )
                                        continue;
                                    foreach ( GameEntity_Squad entity in relatedPlanet.Squads( EntityRollupType.FuelRadonProducers ) )
                                    {
                                        if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                            continue;
                                        resourceOutput += entity.GetFullyMultipliedFuelRadonToProduce().IntValue;
                                    }
                                    if ( detailLevel < TooltipDetail.Full && (playerPlanetFactionOrNull == relatedPlanet.GetControllingPlanetFaction() || resourceOutput <= 0) )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.FuelRadonTextColorAndIcon;
                                    resourceName = "氡气燃料";
                                    break;
                                case ResourceType.FuelXenon:
                                    if ( playerPlanetFactionOrNull == null || !World_AIW2.Instance.IsFuelEnabled )
                                        continue;
                                    if ( playerType != null && !playerType.UsesEnergyAndFuel )
                                        continue;
                                    foreach ( GameEntity_Squad entity in relatedPlanet.Squads( EntityRollupType.FuelXenonProducers ) )
                                    {
                                        if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                            continue;
                                        resourceOutput += entity.GetFullyMultipliedFuelXenonToProduce().IntValue;
                                    }
                                    if ( detailLevel < TooltipDetail.Full && (playerPlanetFactionOrNull == relatedPlanet.GetControllingPlanetFaction() || resourceOutput <= 0) )
                                        continue;
                                    colorAndIcon = ArcenExternalUIUtilities.FuelXenonTextColorAndIcon;
                                    resourceName = "氙气燃料";
                                    break;
                                case ResourceType.Metal:
                                    if ( playerPlanetFactionOrNull == null )
                                        continue;
                                    if ( detailLevel < TooltipDetail.Full && playerPlanetFactionOrNull == relatedPlanet.GetControllingPlanetFaction() )
                                        continue;
                                    if ( playerType != null && !playerType.UsesMetal )
                                        continue;
                                    foreach ( GameEntity_Squad entity in relatedPlanet.Squads( EntityRollupType.MetalProducers ) )
                                    {
                                        if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                            continue;
                                        resourceOutput += (int)entity.GetFullyMultipliedMetalToProduce().IntValue;
                                    }
                                    colorAndIcon = ArcenExternalUIUtilities.MetalTextColorAndIcon;
                                    resourceName = "金属";
                                    break;
                            }
                            buffer.Add( "\n" );
                            buffer.Add( colorAndIcon ).Add( "<pos=30>" ).Add( resourceName ).Add( ":</color> " ).Add( "<pos=120>" ).AddNumberMoreReadable( resourceOutput );

                            switch ( resource )
                            {
                                case ResourceType.Science:
                                    if ( relatedPlanet.IsAdjacentToHumanWorld && AIWar2GalaxySettingQuickAccess.SciencePoints_Adjacence > 0 )
                                        buffer.Add( " (+" ).Add( AIWar2GalaxySettingQuickAccess.SciencePoints_Adjacence )
                                            .Add( " 因与人类世界相邻)" );
                                    break;
                                case ResourceType.Hacking:
                                    if ( relatedPlanet.IsAdjacentToHumanWorld && AIWar2GalaxySettingQuickAccess.HackingPoints_Adjacence > 0 )
                                        buffer.Add( " (+" ).Add( AIWar2GalaxySettingQuickAccess.HackingPoints_Adjacence )
                                            .Add( " 因与人类世界相邻)" );
                                    break;
                            }
                        }
                    } //endif for the planet having to be explored

                    if ( !String.IsNullOrEmpty( relatedPlanet.AdditionalDescriptionTextFromFactions) )
                        buffer.Add("\n").Add(relatedPlanet.AdditionalDescriptionTextFromFactions);
                    if ( relatedPlanet.UnitSlowPercentage > 0 )
                        buffer.Add("此星球上的所有单位因重力增加而减慢了 ").Add( relatedPlanet.UnitSlowPercentage, "a1ffa1" ).Add("%。 ");
                    if ( relatedPlanet.UnitSpeedupPercentage > 0 )
                        buffer.Add("此星球上的所有单位因重力减小而加快了 ").Add( relatedPlanet.UnitSlowPercentage, "a1ffa1" ).Add("%。 ");

                    debugStage = 30;

                    if ( owner != null && owner.Type == FactionType.AI && relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        debugStage = 30100;
                        int reinforcementLocationCount = 0;
                        foreach ( GameEntity_Squad reinforcementPoint in relatedPlanet.Squads( EntityRollupType.ReinforcementLocations ) )
                        {
                            reinforcementLocationCount++;
                        }

                        debugStage = 30400;
                        buffer.Add( "\nAI增援点：" ).Add( reinforcementLocationCount ).Add( "/" ).Add( relatedPlanet.MaxReinforcementPlacesEverSeenHere );
                        if ( relatedPlanet.SentinelsAlertLevel == null )
                            buffer.Add( "\nAI哨兵警戒等级：无" );
                        else
                        {
                            debugStage = 30600;
                            buffer.Add( "\nAI哨兵警戒等级：" ).Add( relatedPlanet.SentinelsAlertLevel.ColorHexStart ).Add( relatedPlanet.SentinelsAlertLevel.DisplayName ).EndColor();
                            if ( relatedPlanet.IsEligibleForDeepStrike )
                            {
                                Faction aiReservesFaction = FactionUtilityMethods.Instance.GetAIReservesFaction();
                                if ( aiReservesFaction != null )
                                    buffer.Add(". ").Add("AI预备队将保卫此星球。", aiReservesFaction.FactionCenterColor.ColorHexBrighter);
                            }
                            debugStage = 30700;
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "\n" ).StartColor( "c1d98f" ) //sickly light gray green
                                    .Add( relatedPlanet.SentinelsAlertLevel.Description ).EndColor();
                            }
                            debugStage = 30800;
                            if ( relatedPlanet.TimeOfLastSentinelReinforcement > 0 && relatedPlanet.TimeOfLastSentinelReinforcement <= World_AIW2.Instance.GameSecond )
                            {
                                buffer.Add( "\nAI哨兵上次增援：" ).StartColor( "5bb936" ).Add( //olive green
                                    Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond - relatedPlanet.TimeOfLastSentinelReinforcement ) ).Add( " 前" ).EndColor();
                                if ( detailLevel >= TooltipDetail.Medium )
                                {
                                    buffer.Add( "\nAI哨兵上次增援战力：" );
                                    buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, relatedPlanet.StrengthOfLastSentinelReinforcements, true, true );
                                    buffer.EndColor();

                                    buffer.Add( "\n此处AI哨兵增援总次数：" ).StartColor( "b8ed48" ) //sickly yellow green
                                        .Add( relatedPlanet.NumberOfSentinelReinforcementsEvents ).EndColor();

                                    buffer.Add( "\n此处AI哨兵增援总战力：" );
                                    buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, relatedPlanet.TotalStrengthOfAllSentinelReinforcements, true, true );
                                    buffer.EndColor();
                                }
                            }
                            else
                            {
                                buffer.Add( "\nAI哨兵上次增援：" ).StartColor( "72856b" ).Add( "未知" ).EndColor(); //dark gray green
                            }
                        }
                    } //end if this was an AI owner

                    debugStage = 31100;
                    if ( relatedPlanet.NumberOfMajorAIStructuresCurrentlyHere > 0 &&
                         relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        debugStage = 31200;
                        buffer.Add( "\n此处主要AI建筑：" ).StartColor( "e974fa" ) //light pink
                            .Add( relatedPlanet.NumberOfMajorAIStructuresCurrentlyHere ).EndColor();
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.Add( "\n主要AI建筑列表：" );
                            workingMajorAIStructuresCounts.Clear();
                            #region First Read Them Into The Dictionary
                            foreach ( GameEntity_Squad ship in relatedPlanet.Squads( EntityRollupType.MajorAIStructure ) )
                            {
                                PlanetFaction pFaction = ship.PlanetFaction;
                                if ( pFaction == null )
                                    continue;
                                Faction faction = pFaction.Faction;
                                if ( faction == null )
                                    continue;
                                if ( faction.Type != FactionType.AI )
                                    continue;

                                if ( workingMajorAIStructuresCounts.ContainsKey( ship.TypeData.DisplayName ) )
                                    workingMajorAIStructuresCounts[ship.TypeData.DisplayName] = workingMajorAIStructuresCounts[ship.TypeData.DisplayName] + 1;
                                else
                                    workingMajorAIStructuresCounts[ship.TypeData.DisplayName] = 1;
                            }
                            #endregion

                            #region Then Sort Them By Name
                            workingMajorAIStructuresCounts.SortIntoList( delegate ( KeyValuePair<string, int> Left, KeyValuePair<string, int> Right )
                            {
                                int val = Left.Key.CompareTo( Right.Key );
                                if ( val != 0 )
                                    return val;
                                return Right.Value.CompareTo( Left.Value );
                            } );
                            #endregion

                            bool isFirst = true;
                            foreach ( KeyValuePair<string, int> Ship in workingMajorAIStructuresCounts )
                            {
                                if ( isFirst )
                                    isFirst = false;
                                else
                                    buffer.Add( ", " );
                                buffer.Add( Ship.Key );
                                if ( Ship.Value > 1 )
                                    buffer.Add( " x" ).Add( Ship.Value );
                            }
                        }
                    }
                    debugStage = 32100;
                    if ( relatedPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        debugStage = 32200;
                        if ( owner != null && owner.Type == FactionType.AI )
                        {
                            if ( relatedPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                                buffer.StartColor( "ff2d9a" ).Add( "\n此星球是AI母星。" ).EndColor();
                            else if ( relatedPlanet.PopulationType == PlanetPopulationType.AIBastionWorld )
                                buffer.StartColor( "ff2d9a" ).Add( "\n此星球是AI堡垒世界。" ).EndColor();
                        }
                        debugStage = 32300;
                        if ( owner == null || owner.Type != FactionType.AI )
                        {
                            //if the owner is NOT an AI
                            if ( relatedPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                                buffer.StartColor( "ff2d9a" ).Add( "\n此星球原本是AI母星！" ).EndColor();
                            else if ( relatedPlanet.PopulationType == PlanetPopulationType.AIBastionWorld )
                                buffer.StartColor( "ff2d9a" ).Add( "\n此星球原本是AI堡垒世界！" ).EndColor();
                        }
                        debugStage = 32400;
                        if ( owner == null || owner.Type != FactionType.Player )
                        {
                            //if the owner is NOT a human player
                            if ( relatedPlanet.PopulationType == PlanetPopulationType.HumanHomeworld )
                                buffer.StartColor( "477fff" ).Add( "\n此星球原本是人类母星！" ).EndColor();
                            if ( relatedPlanet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld )
                                buffer.StartColor( "477fff" ).Add( "\n此星球是人类方舟首次出现的地方！" ).EndColor();

                            if ( AIReservesFactionBaseInfo.Instance != null )
                            {
                                int gracePeriod = AIReservesFactionBaseInfo.Instance.GetEffectiveGracePeriod();
                                int gracePeriodAgoInGameTime = World_AIW2.Instance.GameSecond - gracePeriod;
                                if ( relatedPlanet.TimeLastControlledByHumans > 0 && gracePeriod > 0 && relatedPlanet.TimeLastControlledByHumans > gracePeriodAgoInGameTime )
                                    buffer.Add( "\n人类上次拥有此星球：" ).StartColor( "5bb936" ) //olive green
                                        .AddHoursAndMinutes( World_AIW2.Instance.GameSecond - relatedPlanet.TimeLastControlledByHumans ).Add( " 前" ).EndColor()
                                        .Add( "  （深度打击报复，如果相关，将在 " ).Add( gracePeriodAgoInGameTime ).Add( " 秒后生效）" );
                                else if ( relatedPlanet.TimeLastControlledByHumans > 0 )
                                    buffer.Add( "\n人类上次拥有此星球：" ).StartColor( "708b65" )  //drab olive green
                                        .AddHoursAndMinutes( World_AIW2.Instance.GameSecond - relatedPlanet.TimeLastControlledByHumans ).Add( " ago" ).EndColor();
                            }
                        }


                        debugStage = 32500;
                        buffer.StartColor( relatedPlanet.GravWellSize.ColorHex ).Add( "\n<size=80%>重力井规模：" ).Add( relatedPlanet.GravWellSize.DisplayName );
                        if ( detailLevel >= TooltipDetail.Full )
                            buffer.Add( "\n" ).Add( relatedPlanet.GravWellSize.Description );
                        buffer.EndColor().Add( "</size>" );
                    }


                    debugStage = 33100;
                    //buffer.StartColor( "477fff" ).Add( "\nPlanet Population Type: " ).EndColor().Add( relatedPlanet.PopulationType );
                    //buffer.StartColor( "477fff" ).Add( "\nOriginalHopsToHumanHomeworld: " ).EndColor().Add( relatedPlanet.OriginalHopsToHumanHomeworld );
                    //buffer.StartColor( "477fff" ).Add( "\nOriginalHopsToAIHomeworld: " ).EndColor().Add( relatedPlanet.OriginalHopsToAIHomeworld );
                    //buffer.StartColor( "477fff" ).Add( "\nOriginalHopsToAnyHomeworld: " ).EndColor().Add( relatedPlanet.OriginalHopsToAnyHomeworld );
                    //buffer.Add( relatedPlanet.AfterMapGenDebugReport );

                    if ( relatedPlanet.IsBlockedToPlayerTravelViaWormholes || relatedPlanet.IsBlockedToNPCTravelViaWormholes )
                    {
                        if ( relatedPlanet.IsBlockedToNPCTravelViaWormholes && relatedPlanet.IsBlockedToPlayerTravelViaWormholes )
                            buffer.StartColor( "708b65" ).Add( "\n旅行警告：玩家和NPC单位均无法使用虫洞进入此星球" ).EndColor();
                        else if ( relatedPlanet.IsBlockedToNPCTravelViaWormholes )
                            buffer.StartColor( "708b65" ).Add( "\n旅行警告：NPC单位无法使用虫洞进入此星球" ).EndColor();
                        else if ( relatedPlanet.IsBlockedToPlayerTravelViaWormholes )
                            buffer.StartColor( "708b65" ).Add( "\n旅行警告：玩家单位无法使用虫洞进入此星球" ).EndColor();
                    }

                    debugStage = 34100;
                    if ( displayModeOrNull != null )
                        displayModeOrNull.WriteToPlanetTooltip( relatedPlanet, buffer );
                    debugStage = 34200;
                    if ( relatedPlanet.Missions.Count > 0 )
                    {
                        buffer.Add("\n");
                        for ( int i = 0; i < relatedPlanet.Missions.Count; i++ )
                        {
                            buffer.Add( relatedPlanet.Missions[i].ToStringForDisplay() ).Add("\n");
                        }
                    }
                    debugStage = 40;
                    EntityText.Write_Tooltip_Hotkeys_Footer( buffer, true, true, null );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in planet tooltip text generation at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            private void GetTextForPlanetLink( ArcenDoubleCharacterBuffer buffer, GalaxyMapPlanetLink link )
            {
                if ( link == null )
                    return;
                GalaxyMapPlanet firstMapPlanet = link.PlanetOne;
                GalaxyMapPlanet secondMapPlanet = link.PlanetTwo;
                if ( firstMapPlanet == null || secondMapPlanet == null )
                    return;
                Planet firstPlanet = firstMapPlanet.RelatedPlanet;
                Planet secondPlanet = secondMapPlanet.RelatedPlanet;
                if ( firstPlanet == null || secondPlanet == null )
                    return;
                Planet aiPlanetOrNull;
                bool hasGate;
                GalaxyMapLinkUtils.DetectAiAndWarpGateAdjacencyToPlayer( firstPlanet, secondPlanet, out aiPlanetOrNull, out hasGate );

                if ( aiPlanetOrNull != null )
                {
                    Planet humanPlanet = aiPlanetOrNull == secondPlanet ? firstPlanet : secondPlanet;
                    Faction aiFaction = aiPlanetOrNull.GetControllingFaction();
                    string aiName = aiFaction.GetDisplayName();
                    string aiColor = aiFaction.FactionCenterColor.ColorHex;
                    if ( hasGate )
                    {
                        bool foundWavesAgainstThisPlanet = false;
                        foreach ( PlannedWave wave in WaveUtils.KnownWavesAgainstHumanWorlds )
                        {
                            Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                            Planet sourcePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                            if ( targetPlanet == humanPlanet && sourcePlanet == aiPlanetOrNull )
                            {
                                if ( !foundWavesAgainstThisPlanet )
                                {
                                    buffer.StartColor( Color.red ).Add( $"即将到来的攻击波！" ).EndColor().NewLine();
                                }

                                foundWavesAgainstThisPlanet = true;

                                wave.AppendStateForInterfaceDisplay( buffer );
                                buffer.NewLine();
                            }
                        }

                        if ( !foundWavesAgainstThisPlanet )
                        {
                            buffer.StartColor( Color.yellow ).Add( $"在 {aiPlanetOrNull.Name} 上有活跃的 <color=#{aiColor}>{aiName}</color> 跃迁门！" ).EndColor().NewLine();
                            buffer.Add( $"<size=80%>{aiName}可能从虫洞 {aiPlanetOrNull.Name} 对星球 {humanPlanet.Name} 发动攻击波</size>" ).NewLine();
                        }
                    }
                    else
                    {
                        buffer.Add( $"在 {aiPlanetOrNull.Name} 上的 <color=#{aiColor}>{aiName}</color> 跃迁门已被摧毁。" ).NewLine();
                        buffer.Add( $"<size=80%>从虫洞 {aiPlanetOrNull.Name} 对星球 {humanPlanet.Name} 的攻击波已被抑制。</size>" ).NewLine();
                    }

                    buffer.NewLine();
                }
                buffer.StartColor( "aaaaaa" ).Add( $"连接星球 {firstPlanet.Name} 和 {secondPlanet.Name} 的虫洞" ).EndColor();
            }

            #endregion
            
            public override void OnMainThreadUpdate()
            {
                this.UpdatePositionAndSize();
            }

            public void UpdatePositionAndSize()
            {
                //if ( this.GetShouldBeHidden() )
                //    return;
                if ( this.SubTexts == null )
                    return;
                if ( !this.hasSetCanvasOffset )
                {
                    if ( this.MyElement != null && this.MyElement.Window != null )
                    {
                        this.hasSetCanvasOffset = true;
                        this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always on top!
                    }
                }

                try
                {
                    float screenXPixel = 230 * (Window_InGameHoverEntityInfo.Instance == null ? 1f : Window_InGameHoverEntityInfo.Instance.myXPositionScale);
                    float screenYPixel = 0;

                    if ( this.MyElement != null && this.MyElement.Window != null )
                        this.MyElement.Window.IsAutomaticPositioningDisabled = true;

                    Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );
                    if ( Window_InGameSidebarShips.Instance != null )
                        worldSpacePoint.x = Window_InGameSidebarShips.Instance.GetWorldSpaceMaxX( 5f *
                            (Window_InGameHoverEntityInfo.Instance == null ? 1f : Window_InGameHoverEntityInfo.Instance.myXPositionScale), false );

                    SubText groupZero = this.SubTexts[0];
                    if ( groupZero == null || groupZero.Obj == null || groupZero.Obj.transform == null || groupZero.Obj.transform.parent == null )
                        return;

                    Vector2 sizeDelta = ((RectTransform)groupZero.Obj.transform.parent).GetWorldSpaceSize();
                    float width = sizeDelta.x;
                    float height = sizeDelta.y;

                    float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width;
                    float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;

                    if ( this.lastTextWasForPlanetLink )
                    {
                        float otherMaxY = Window_BottomLeftGalaxyMap.Instance.GetWorldSpaceTopY( 0, true ) + height;
                        if ( otherMaxY > maxYPixel )
                            maxYPixel = otherMaxY;
                    }

                    worldSpacePoint.x = Mathf.Min( worldSpacePoint.x, maxXPixel );
                    worldSpacePoint.y = Mathf.Max( worldSpacePoint.y, maxYPixel );

                    if ( this.MyElement != null && this.MyElement.Window != null )
                        this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );
                }
                catch ( Exception ) { } //be silent on this, I guess

            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.MyElement = (ArcenUI_ImageButton)Element;
                this.MyElement.Window.MaxDeltaTimeBeforeUpdates = 0;
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

            private float _lastScale = -1f;
            private const int BASE_TOOLTIP_WIDTH = 660;

            public void DoResizeIfNeeded()
            {
                float scale = ArcenUI.Instance.Tooltip_Scale("PlanetTooltipScale");
                bool scale_changed = scale != _lastScale;
                if ( scale_changed )
                    this.NeedsToResize = true;

                if ( this.MyElement != null && 
                     this.NeedsToResize && 
                     this.SubTexts != null && 
                     this.SubTexts[1].Obj.activeInHierarchy && 
                     this.NextTextToShow != null && 
                     this.NextTextToShow.Length > 0 )
                {
                    this.NeedsToResize = false;
                    this._lastScale = scale;
                    string text = this.NextTextToShow;

                    int min_width, max_width;
                    ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);
                    
                    float min = min_width;
                    float max = max_width;
                        
                    if ( scale_changed )
                    {
                        this.SubTexts[1].ReferenceText.rectTransform.UI_SetWidth( max_width );
                        //this.SubTexts[0].Obj.transform.parent.localScale = new Vector3( newGeneralTooltipScale, newGeneralTooltipScale, newGeneralTooltipScale );
                    }
                    
                    this.MyElement.gameObject.transform.localScale = new Vector3( scale, scale, scale );

                    var messageSize = ArcenUI.Instance.CalculatePreferredTextObjectDimensions( this.SubTexts[1].ReferenceText, text, min, max );
                    this.WrappedNextTextToShow = this.NextTextToShow;
                    this.LastRequestedWidth = messageSize.x;
                    this.LastRequestedHeight = messageSize.y;

                    float text_margin_h, text_margin_v;
                    ArcenUI.Instance.Tooltip_Margin(out text_margin_h, out text_margin_v);

                    var bg_width = text_margin_h*2 + this.LastRequestedWidth;
                    var bg_height = text_margin_v*2 + this.LastRequestedHeight;
                    
                    var ui_width = text_margin_h*1 + this.LastRequestedWidth;
                    var ui_height = text_margin_v*1 + this.LastRequestedHeight;

                    ArcenUI_Image.SubImage subImage = this.MyElement.SubImages[0];
                    subImage.Img.rectTransform.UI_SetWidth( bg_width );
                    subImage.Img.rectTransform.UI_SetHeight( bg_height );
                    
                    var ui_text = this.SubTexts[0].ReferenceText;
                    ui_text.rectTransform.UI_SetWidth(ui_width);
                    ui_text.rectTransform.UI_SetHeight(ui_height);
                    
                    this.UpdateTextIfNeeded();
                    this.UpdatePositionAndSize();
                }
            }
        }

        #region GetPlanetFactionalData
        public static void GetPlanetFactionalData( 
            Planet planet, 
            out int threatStrengthInt, out int hostileStrengthMinusThreatInt, out int myTotalStrength, out int myMobileStrength, out int myAndAlliedTotalStrength, out int myAndAlliedMobileStrength, 
            out Faction localOrAlliedFaction, out Faction alliedFaction, out Faction hostileFaction, 
            bool isMinimalFogOfWar )
        {
            threatStrengthInt = 0;
            hostileStrengthMinusThreatInt = 0;
            hostileFaction = null;
            localOrAlliedFaction = null;
            alliedFaction = null;
            myTotalStrength = 0;
            myMobileStrength = 0;
            myAndAlliedTotalStrength = 0;
            myAndAlliedMobileStrength = 0;

            Faction localFactionG = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFactionG == null )
                localFactionG = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

            if ( localFactionG == null )
                return;

            PlanetFaction localFaction = planet.GetPlanetFactionForFaction( localFactionG );

            if ( localFaction == null )
                return;

            if ( planet.GetControllingFactionType() == FactionType.Player )
            {
                threatStrengthInt = localFaction.DataByStance[FactionStance.Hostile].TotalStrengthVisible; //you have vision here since you own the planet
            }
            else
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    return;

                if ( isMinimalFogOfWar )
                {
                    //Show more detailed information, even if you can't really see it according to the rules
                    threatStrengthInt = localFaction.DataByStance[FactionStance.Hostile].RelativeToHumanTeam_ThreatStrength;
                    hostileStrengthMinusThreatInt = localFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                }
                else
                {
                    threatStrengthInt = localFaction.DataByStance[FactionStance.Hostile].RelativeToHumanTeam_ThreatStrengthVisible;
                    hostileStrengthMinusThreatInt = localFaction.DataByStance[FactionStance.Hostile].TotalStrengthVisible;

                }
                hostileStrengthMinusThreatInt -= threatStrengthInt;
            }

            myTotalStrength = localFaction.DataByStance[FactionStance.Self].TotalStrength;
            myMobileStrength = localFaction.DataByStance[FactionStance.Self].MobileStrength;
            myAndAlliedTotalStrength = myTotalStrength + localFaction.DataByStance[FactionStance.Friendly].TotalStrengthVisible;
            myAndAlliedMobileStrength = myMobileStrength + localFaction.DataByStance[FactionStance.Friendly].MobileStrength;


            //ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + ": " + planet.IntelLevel + " threatStrengthInt: " + threatStrengthInt +
            //    " hostileStrengthMinusThreatInt: " + hostileStrengthMinusThreatInt +
            //    " myAndAlliedTotalStrength: " + myAndAlliedTotalStrength +
            //    " myAndAlliedMobileStrength: " + myAndAlliedMobileStrength, Verbosity.DoNotShow );

            if ( threatStrengthInt <= 0 && hostileStrengthMinusThreatInt <= 0 && myAndAlliedTotalStrength <= 0 )
                return;

            if ( myTotalStrength > 0 )
                localOrAlliedFaction = localFaction.Faction;

            if ( myAndAlliedTotalStrength > 0 )
            {
                //calculate the strongest ally's color; if we have no strength here, also use that for "my" color
                int strongestAlliedStrength = 0;
                Faction strongestAlliedFaction = null;
                for ( int j = 0; j < planet.Factions.Count; j++ )
                {
                    var planetFaction = planet.Factions[j];
                    if ( !planetFaction.GetIsFriendlyTowards( localFaction ) || planetFaction == localFaction ) //don't count ourselves as an allied faction
                        continue;
                    if ( planetFaction.DataByStance[FactionStance.Self].TotalStrength <= 0 )
                        continue;
                    if ( planetFaction.DataByStance[FactionStance.Self].TotalStrength >  strongestAlliedStrength )
                    {
                        strongestAlliedStrength = planetFaction.DataByStance[FactionStance.Self].TotalStrength;
                        strongestAlliedFaction = planetFaction.Faction;
                    }
                }

                alliedFaction = strongestAlliedFaction;

                localOrAlliedFaction = localFaction.Faction;
                if ( myTotalStrength == 0 )
                    localOrAlliedFaction = alliedFaction;
            }

            if ( hostileStrengthMinusThreatInt > 0 || threatStrengthInt > 0 )
            {
                PlanetFaction enemyFaction;
                for ( int j = 0; j < planet.Factions.Count; j++ )
                {
                    enemyFaction = planet.Factions[j];
                    if ( !enemyFaction.GetIsHostileTowards( localFaction ) )
                        continue;

                    if ( isMinimalFogOfWar )
                    {
                        if ( enemyFaction.DataByStance[FactionStance.Self].TotalStrength <= 0 )
                            continue;
                    }
                    else
                    {
                        if ( enemyFaction.DataByStance[FactionStance.Self].TotalStrengthVisible <= 0 )
                            continue;
                    }
                    if ( planet.GetControllingPlanetFaction() == enemyFaction )
                    {
                        hostileFaction = enemyFaction.Faction;
                        break;
                    }
                    if ( planet.PrimaryInfluencingFaction != -1 )
                    {
                        Faction influencer = World_AIW2.Instance.GetFactionByIndex(planet.PrimaryInfluencingFaction);
                        if ( planet.GetPlanetFactionForFaction(influencer) == enemyFaction )
                        {
                            hostileFaction = influencer;
                            break;
                        }
                    }
                }
                if ( hostileFaction == null )
                {

                    int strengthOfStrongestEnemy = 0;
                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        enemyFaction = planet.Factions[j];
                        if ( !enemyFaction.GetIsHostileTowards( localFaction ) )
                            continue;
                        int strengthToCheck = enemyFaction.DataByStance[FactionStance.Self].TotalStrengthVisible;
                        if ( isMinimalFogOfWar )
                            strengthToCheck = enemyFaction.DataByStance[FactionStance.Self].TotalStrength;

                        if ( strengthToCheck < strengthOfStrongestEnemy )
                            continue;
                        hostileFaction = enemyFaction.Faction;
                        strengthOfStrongestEnemy = strengthToCheck;
                    }
                }
            }
        }
        #endregion
    }
}
