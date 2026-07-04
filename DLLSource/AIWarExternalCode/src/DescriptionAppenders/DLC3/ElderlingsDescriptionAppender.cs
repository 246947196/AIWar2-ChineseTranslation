using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ElderlingsDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings ));
                if ( RelatedEntityOrNull == null || RelatedEntityTypeData == null )
                {
                    debugStage = 200;
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }
                
                debugStage = 400;
                Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
                if ( faction == null )
                {
                    Buffer.Add( "没有阵营数据？" );
                    return;
                }

                debugStage = 500;
                if ( faction.SpecialFactionData.InternalName == "MaddenedElderlings" )
                {
                    Buffer.Add( "此长者已被其同伴逼疯。\n" );
                    return;
                }

                debugStage = 600;
                ElderlingsFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<ElderlingsFactionBaseInfo>();
                if ( globaldata == null )
                {
                    // This is an elderling unit, but not a member of the elderling faction. It doesn't lay eggs or do anything that needs to be described below.
                    // Read: an ai copy of an elderling, maybe outguard, maybe nanocaust, its not a problem.
                    return;
                }
                
                debugStage = 300;
                ElderlingsPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( data == null )
                {
                    Buffer.Add( "没有单位数据？" );
                    return;
                }

                debugStage = 700;
                bool verbose = (GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetDetailModeByDefault" ) ||
                                 GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetMediumModeByDefault" ) ||
                                 InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1() || debug);

                debugStage = 800;
                int intensity = globaldata.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                debugStage = 900;
                if ( intensity <= 0 )
                    throw new Exception( "Got invalid intensity " + intensity + " from " + faction.GetDisplayName() + " for " + RelatedEntityOrNull.ToStringWithPlanetAndOwner() );
                debugStage = 1100;
                ElderlingsDifficulty diff = ElderlingsDifficultyTable.Instance.GetRowByIntensity( globaldata.Intensity, faction );
                debugStage = 1200;
                if ( data.SuicideMode )
                {
                    debugStage = 1300;
                    Buffer.Add( "<color=#ffa1a1>此长者已被其同伴逼疯并正在暴走</color>。\n" );
                }
                else
                {
                    debugStage = 1400;
                    if ( data.SanityRemaining > 0 && verbose && faction.SpecialFactionData.InternalName != "MaddenedElderlings" )
                    {
                        debugStage = 1500;
                        Buffer.Add( "此长者剩余 " ).Add( data.SanityRemaining, "a1ffa1" ).Add( " 点理智。当理智耗尽时，它将暴走！\n" );
                    }
                    debugStage = 1600;
                    if ( data.HatchTime > 0 )
                    {
                        debugStage = 1700;
                        int time = data.HatchTime - World_AIW2.Instance.GameSecond;
                        debugStage = 1800;
                        string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time ); //timerColor gets more red the closer the planet is to succumbing
                        Buffer.Add( "此蛋将在 " ).Add( time.ToString(), timerColor ).Add( " 秒后孵化。" );
                    }
                    debugStage = 2100;
                    Planet lurePlanet = data.LurePlanet;
                    if ( lurePlanet != null )
                    {
                        debugStage = 2200;
                        Buffer.Add( "此长者正被引诱至 " ).Add( lurePlanet.Name, "ffa1a1" ).Add( "。\n" );
                    }
                    debugStage = 2300;
                    bool showData = (data.TrackedByPlayer || debug || globaldata.PlayerAllied);
                    debugStage = 3100;
                    if ( data.NextEggLayingTime > 0 && showData )
                    {
                        debugStage = 3200;
                        int time = data.NextEggLayingTime - World_AIW2.Instance.GameSecond;
                        debugStage = 3300;
                        string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time ); //timerColor gets more red the closer the planet is to succumbing
                        debugStage = 3400;
                        if ( time > 0 )
                            Buffer.Add( "此长者将在 " ).Add( time.ToString(), timerColor ).Add( " 秒后能够产卵。" );
                        else
                        {
                            debugStage = 3500;
                            Planet planetForEgg = data.PlanetForEgg;
                            debugStage = 3600;
                            if ( planetForEgg != null && lurePlanet == null )
                            {
                                debugStage = 3700;
                                Buffer.Add( "此长者正前往 " ).Add( planetForEgg.Name, "a1ffa1" ).Add( " 产卵。" );
                            }
                            else
                                Buffer.Add( "此长者可以在其选择时产卵。" );
                        }
                    }
                    debugStage = 4050;
                    if ( data.NumberOfTimesLeveledUp > 0 ) {
                        Buffer.Add( " 此长者已升级 " ).Add( data.NumberOfTimesLeveledUp, "a1ffa1" ).Add( " 次。" );
                    }
                    debugStage = 4100;
                    if ( data.ExperienceRequired > 0 && showData )
                    {
                        debugStage = 4200;
                        string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.ExperienceRequired ); //timerColor gets more red the closer the planet is to succumbing
                        debugStage = 4300;
                        Buffer.Add( " 此长者需要 " ).Add( data.ExperienceRequired.ToString(), timerColor ).Add( " 经验值才能升级。" );
                        if ( verbose )
                            Buffer.Add( " 长者从多种来源获取经验，包括时间流逝和其领地被入侵。当此长者超过标记等级7时，它可能会变形为更强大的形态。" );
                        Buffer.Add( "\n" );
                    }
                    debugStage = 5100;
                    if ( !data.FullyUpgraded && data.ExperienceRequired <= 0 && showData && !RelatedEntityTypeData.GetHasTag( "HighElderling" ) )
                    {
                        debugStage = 5200;
                        Buffer.Add( " 此长者将在脱离战斗后立即改变形态。" );
                    }
                    debugStage = 5300;
                    if ( data.GetsAFreeTerritoryIfPossible && showData )
                    {
                        debugStage = 5500;
                        Buffer.Add( " 此长者正试图扩张其领地。" );
                    }
                    debugStage = 5600;
                    if ( data.Territory.Count > 0 && showData )
                    {
                        debugStage = 5700;
                        Buffer.Add( "\n长者的领地：\n" );
                        for ( int i = 0; i < data.Territory.Count; i++ )
                        {
                            debugStage = 5800;
                            Buffer.Add( "\t" ).Add( data.Territory[i].Name, "ffa1ff" ).Add( "\n" );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ElderlingsDescriptionAppender error at debugStage " + debugStage + ", Exception " + e, Verbosity.ShowAsError );
            }
        }
    }
}
