using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class AbstractWaveBase : ConcurrentPoolable<AbstractWaveBase>
    {
        //If true, the player will not get a warning in the UI of this wave incoming
        //this is basically equivalent to setting secondsAdvanceWarningToGive to 0
        //but it's a bit neater this way. Once there's a toggle for letting the player
        //tweak the amount of warning they'll get it may be best to remove this flag
        public bool disableWaveWarnings;

        //Note that we keep the planet Idx here, not a Planet object. This is because when doing Deserialization (aka load game)
        //special factions are deserialized before the Planet list.
        //We need to GetPlanetByIndex whenever we want to actually use the Planet
        public Int16 planetWithWarpGateIdx;

        public Int16 targetPlanetIdx;

        public GameEntity_Squad overrideEntityToSpawnAt; //this is used only by hacking waves and isn't synchonrized to disk

        //the composition of the wave (ie how many of which ship to send)
        public readonly Dictionary<GameEntityTypeData, int> FinalComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 400, "PlannedWave-FinalComposition" );

        public int gameTimeInSecondsForLaunchWave;

        //This value controls whether a Wave appears directly on the target planet,
        //or whether it spawns at the warp gate then travels to the target
        public bool spawnWaveDirectlyOnTarget;

        public bool isReconquestWave; //this always spawns a Usurper
        //this is using the wave warning for spawning a cross planet attack (CPA) under the hood
        public bool isActuallyACrossPlanetAttack;

        //how much advance warning to give the player
        public Int16 secondsAdvanceWarningToGive;

        public bool playerBeingAlerted; //whether the player is currently being alerted (ie there's
        //less than secondsAdvanceWarningToGive seconds until the wave spawns)

        public int aiCostBudgetForWave; //for ease of refunding
        public int StrengthOfWave; //this isn't sync'd to disk, since it's easily recalculated
        //If a wave's Warp Gate is destroyed, the AI gets bonuses to the next wave and the next Wormhole Invasion
        //TODO: perhaps this should probably be tuned based on the AI difficulty setting
        public FInt cancelRefundRatioForNextWave;
        public FInt cancelRefundRatioForNextWormholeInvasion;

        //for handing this PlannedWave object in PerSimStep
        public bool deQueueWave;

        public bool isExogalacticWormholeWave; //for tooltip only, we can't say "warp gate" in this case

        public bool sendWaveThisSimStep;

        public bool NonSimIsAgainstAHumanHomeworld = false;
        public string NonSimPlanetName = string.Empty;

        public Int16 SendingFactionIndex = 0;
        public Int16 TargetFactionIndex = 0; //waves can be sent against non-player factions, and we might want to let the player know
        public bool IsAstroTrainWave; //the astro trains can cause waves to be sent. This is used for the tooltip only

        public string DebugString = string.Empty; //used to store debug information about how the wave was generated

        //IMPORTANT: any additions to the above need to have a clear entry in SetDefaults!

        protected void SetDefaults()
        {
            this.disableWaveWarnings = false;
            this.planetWithWarpGateIdx = -1;
            this.targetPlanetIdx = -1;
            this.overrideEntityToSpawnAt = null;
            this.FinalComposition.Clear();
            this.gameTimeInSecondsForLaunchWave = 0;

            this.spawnWaveDirectlyOnTarget = true;
            this.isReconquestWave = false;
            this.isActuallyACrossPlanetAttack = false;

            this.secondsAdvanceWarningToGive = 120;
            this.playerBeingAlerted = false;

            this.aiCostBudgetForWave = 0;
            this.StrengthOfWave = 0;

            this.cancelRefundRatioForNextWave = FInt.Zero;
            this.cancelRefundRatioForNextWormholeInvasion = FInt.Zero;

            this.deQueueWave = false;
            this.isExogalacticWormholeWave = false;
            this.sendWaveThisSimStep = false;

            this.NonSimIsAgainstAHumanHomeworld = false;
            this.NonSimPlanetName = string.Empty;

            this.SendingFactionIndex = 0;
            this.TargetFactionIndex = 0;
            this.IsAstroTrainWave = false;

            this.DebugString = string.Empty;
        }

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                //this function is called to display the wave for the UI elements
                Faction sendingFaction = World_AIW2.Instance.GetFactionByIndex( this.SendingFactionIndex );
                if ( sendingFaction == null || sendingFaction.Type == FactionType.NaturalObject )
                    return; //this happens right before the tooltip disappears
                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( this.targetPlanetIdx );
                Planet warpGatePlanetOrNull = World_AIW2.Instance.GetPlanetByIndex( this.planetWithWarpGateIdx );
                debugCode = 300;

                if ( !this.isActuallyACrossPlanetAttack )
                {
                    debugCode = 500;
                    AISentinelsCoreData sentinelsExternalOrNull = sendingFaction.GetAISentinelsCoreData().SentinelInfo;
                    debugCode = 700;

                    debugCode = 800;
                    if ( sentinelsExternalOrNull == null )
                        buffer.AddFactionColoredString( "AI（未知）", sendingFaction );
                    else
                    {
                        bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
                        if ( secretFactionDetails )
                        {
                            buffer.AddFactionNameInItsColor( sendingFaction );
                        }
                        else
                        {
                            debugCode = 900;
                            if ( sentinelsExternalOrNull.WasRandomAIType && !GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" ) )
                                buffer.AddFactionColoredString( "AI（随机）", sendingFaction );
                            else if ( sentinelsExternalOrNull.AdaptiveAIDifficulty != TypeDifficulty.Unset && !GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" ) )
                                buffer.AddFactionColoredString( "AI（自适应）", sendingFaction );
                            else
                                buffer.StartColor( sendingFaction.FactionCenterColor.TeamColorBrighter ).Add( "AI（" ).Add( sentinelsExternalOrNull.AIType.DisplayName ).Add( "）" ).EndColor();
                        }
                    }

                    buffer.Add( " 发送 " );

                    debugCode = 1100;
                    buffer.WriteWaveStrength( this );
                    buffer.Add( "，包含" );

                    debugCode = 1500;
                    //A CPA doesn't have a composition like a normal wave, so don't try to display it
                    bool isFirst = true;
                    foreach ( KeyValuePair<GameEntityTypeData, int> kv in this.FinalComposition )
                    {
                        if ( isFirst )
                            isFirst = false;
                        else
                            buffer.Add( ", " );

                        debugCode = 1600;
                        int numSquads = kv.Value;
                        debugCode = 1700;
                        buffer.Add( " " ).Add( numSquads, "7699ff" ).Add( " " ).Add( kv.Key.GetDisplayName(), "cfd988" ).Add( " 艘" );
                    }
                }

                debugCode = 3100;
                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave ) )
                {
                    buffer.Add( ". Wave budget " + this.aiCostBudgetForWave + ". " );
                }

                debugCode = 3300;
                if ( this.isActuallyACrossPlanetAttack )
                {
                    buffer.Clear();
                    debugCode = 3400;
                    buffer.Add( "大规模跨星球攻击将在全星系范围内生成，" )
                        .Add( Engine_Universal.ToHoursAndMinutesString( this.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond ), "a1ffa1" ).Add( " 后开始。" );
                }
                else if ( this.overrideEntityToSpawnAt == null && this.spawnWaveDirectlyOnTarget )
                {
                    debugCode = 4100;
                    //Wave is spawning on target
                    buffer.Add( " 生成于 " );
                    if ( targetPlanet != null )
                    {
                        debugCode = 4200;
                        //default case
                        buffer.StartColor( targetPlanet.GetControllingFaction().FactionCenterColor.TeamColor );
                        debugCode = 4300;
                        buffer.Add( targetPlanet.Name );
                        buffer.EndColor();
                    }
                    else
                    {
                        buffer.Add( " <null target planet (this is a bug )> " );
                    }
                    debugCode = 4400;
                    if ( this.isExogalacticWormholeWave )
                        buffer.Add( "，来自河外虫洞在 " );
                    else
                        buffer.Add( "，来自虫洞门在 " );
                    debugCode = 4500;
                    buffer.StartColor( sendingFaction.FactionCenterColor.TeamColor );
                    debugCode = 4600;
                    if ( warpGatePlanetOrNull == null )
                        buffer.Add( " <null warp gate planet (this is a bug )> " );
                    else
                        buffer.Add( warpGatePlanetOrNull.Name );
                    debugCode = 4700;
                    buffer.EndColor();
                    int secondsRemaining = this.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond;
                    if ( secondsRemaining > 0 )
                        buffer.Add( "，在 " ).Add( Engine_Universal.ToHoursAndMinutesString( secondsRemaining ), "a1ffa1" ).Add( " 后。" );
                    else
                        buffer.Add( " " ).Add( "随时可能", "a1ffa1" ).Add( "...但具体时间可能是个惊喜。" );
                }
                else if ( this.overrideEntityToSpawnAt == null )
                {
                    debugCode = 6100;
                    //Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( this.targetPlanetIdx );
                    buffer.Add( " 在 AI 虫洞门生成于 " );
                    debugCode = 6200;
                    buffer.StartColor( sendingFaction.FactionCenterColor.TeamColor );
                    debugCode = 6300;
                    if ( warpGatePlanetOrNull == null )
                        buffer.Add( " <null warp gate planet (this is a bug )> " );
                    else
                        buffer.Add( warpGatePlanetOrNull.Name );
                    buffer.EndColor();
                    buffer.Add( "，然后将穿越正常空间进行攻击" );
                    debugCode = 6400;
                    int secondsRemaining = this.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond;
                    if ( secondsRemaining > 0 )
                        buffer.Add( "，在 " ).Add( Engine_Universal.ToHoursAndMinutesString( secondsRemaining ), "a1ffa1" ).Add( " 后。" );
                    else
                        buffer.Add( " " ).Add( "随时可能", "a1ffa1" ).Add( "...但具体时间可能是个惊喜。" );
                }
                else
                    buffer.Add( " 立即前往特定位置（破解波）" ); // currently ToString is not called for hacking waves, but just in case
                debugCode = 8100;
                if ( this.isReconquestWave )
                {
                    debugCode = 8200;
                    buffer.Add( "。此波将允许 AI 重新夺回一个星球。" );
                }

                debugCode = 9100;
                //If we have a target faction pre-set, or this planet is owned by a non-player faction, give an extra message in the tooltip
                Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( this.TargetFactionIndex );
                debugCode = 9200;
                if ( (targetFaction == null || targetFaction.Type == FactionType.NaturalObject) && //if we want to see if there's a better target faction
                     targetPlanet != null && //and there's a target planet 
                     targetPlanet.GetControllingFactionType() == FactionType.NaturalObject && //that's not owned by player or AI
                     targetPlanet.GetControllingOrInfluencingFaction().Type != FactionType.NaturalObject && //and it is owned by a minor faction
                     !targetPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.AIShouldNeverHaveHunterAgainstThis ) //and the AI can send hunter against this faction
                {
                    debugCode = 9300;
                    targetFaction = World_AIW2.Instance.GetFactionByIndex( targetPlanet.PrimaryInfluencingFaction );
                }

                debugCode = 9400;
                if ( targetFaction != null && targetFaction.Type != FactionType.Player && targetFaction.Type != FactionType.NaturalObject )
                {
                    debugCode = 9500;
                    buffer.Add( " 此波攻击目标是 " );
                    buffer.StartColor( targetFaction.FactionCenterColor.TeamColor );
                    debugCode = 9600;
                    buffer.Add( targetFaction.GetDisplayName() );
                    buffer.EndColor();
                    buffer.Add( "." );
                }

                debugCode = 11200;
                if ( this.IsAstroTrainWave )
                {
                    debugCode = 11300;
                    buffer.Add( " 星运列车致以问候。" );
                }
                debugCode = 12100;
                if ( this.isActuallyACrossPlanetAttack )
                {
                    debugCode = 12200;
                    bool useTsunami = World_AIW2.Instance.Setup.GetBoolBySetting( "TsunamiCPA" );
                    debugCode = 12300;
                    if ( !useTsunami )
                        buffer.Add( "\n" ).Add( "跨星球攻击一次性在大范围星系内解放大量 AI 守卫舰船。它不会生成新舰船，而是将原本守卫各星球的舰船转变为对您的活跃威胁。在亲眼看到之前，您不会知道 CPA 中有什么，甚至不知道它有多强。一般来说，AIP 越高，CPA 越强。这些舰船转变为威胁后，最可能的结果是加入猎手舰队并在更晚的时候袭击您，但您永远无法确定。如果您想要更刺激和有趣的 CPA，请考虑在星系选项中启用'海啸 CPA'选项。" );
                    else
                        buffer.Add( "\n" ).Add( "跨星球攻击一次性在大范围星系内解放大量 AI 守卫舰船。它不会生成新舰船，而是将原本守卫各星球的舰船转变为一波汹涌的攻击者，它们将从四面八方在不同时间到达。在亲眼看到之前，您不会知道 CPA 中有什么，甚至不知道它有多强。一般来说，AIP 越高，CPA 越强。由于您正在使用激动人心的海啸 CPA 选项，您将有机会在自己的防御中击落大量 AI 舰船...但同时它们可能会制造危险的缺口供猎手利用，或者凭借纯粹的数量优势碾压您的防御。" );
                }
                else
                {
                    debugCode = 13100;
                    buffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
                    debugCode = 13200;
                    buffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldAndClickToViewDetailsOfContents" ) )
                            .Add( "</color> <color=#3f6c9e>并点击此处查看波次中所有舰船类型的详细信息。</color>  " );
                    buffer.Add( "</size>" );
                }
                debugCode = 13300;
                if ( GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) )
                    buffer.Add( this.DebugString );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "WaveDisplay.AppendStateForInterfaceDisplay exception hit. debugCode " + debugCode + " exception " + e, Verbosity.ShowAsError );
            }
        }

        public SortedNotificationPriorityLevel CalculatePriorityForNotification()
        {
            if ( this.isActuallyACrossPlanetAttack )
            {
                return SortedNotificationPriorityLevel.Major;
            }
            else
            {
                Int16 targetFactionIndex = this.TargetFactionIndex;
                if ( targetFactionIndex > 0 )
                {
                    Faction targetFaction = World_AIW2.Instance.Factions[targetFactionIndex];
                    //any waves against not-the-player
                    if ( targetFaction != null && targetFaction.Type != FactionType.Player )
                        return SortedNotificationPriorityLevel.Minor;
                }
                if ( this.isReconquestWave )
                    return SortedNotificationPriorityLevel.Major;

                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( this.targetPlanetIdx );
                if ( targetPlanet == null )
                    return SortedNotificationPriorityLevel.Minor;

                Faction ownerOfPlanet = targetPlanet.GetControllingFaction();
                if ( ownerOfPlanet == null || !ownerOfPlanet.GetIsFriendlyToLocalFaction() )
                    return SortedNotificationPriorityLevel.Minor;

                int friendlyStrength = targetPlanet.GetStrengthOfFactions_FriendlyTo( ownerOfPlanet, true );

                Faction sendingFaction = World_AIW2.Instance.GetFactionByIndex( this.SendingFactionIndex );
                if ( this.StrengthOfWave <= 0 )
                    this.CalculateStrengthOfWave( sendingFaction );
                if ( this.StrengthOfWave < friendlyStrength * 2 / 3 )
                    return SortedNotificationPriorityLevel.Minor;
                if ( this.StrengthOfWave > friendlyStrength * 3 / 2 )
                {
                    //TODO: planets with Ark Empire Kings are not going to get OMG notifications
                    if ( targetPlanet.PopulationType == PlanetPopulationType.HumanHomeworld )
                    {
                        if ( this.StrengthOfWave > friendlyStrength + friendlyStrength )
                            return SortedNotificationPriorityLevel.OMG;
                    }
                    return SortedNotificationPriorityLevel.Major;
                }
                return SortedNotificationPriorityLevel.Medium;
            }
        }

        public int CalculateStrengthOfWave( Faction factionOfAI )
        {
            if ( this.FinalComposition == null || factionOfAI == null || this.StrengthOfWave != 0 )
                return this.StrengthOfWave;
            this.StrengthOfWave = CalculateStrengthOfWaveCompsition( this.FinalComposition, factionOfAI );
            return this.StrengthOfWave;
        }

        public static int CalculateStrengthOfWaveCompsition( Dictionary<GameEntityTypeData, int> Composition, Faction factionOfAI )
        {
            if ( Composition == null || factionOfAI == null )
                return -1;
            int strength = 0;
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in Composition )
            {
                if ( kv.Key == null || kv.Value <= 0 )
                    continue;
                strength += (kv.Key.MarkStatsFor( factionOfAI.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership * kv.Value);
            }
            return strength;
        }
    }
}
