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
                    Buffer.Add( "No per faction data?" );
                    return;
                }

                debugStage = 500;
                if ( faction.SpecialFactionData.InternalName == "MaddenedElderlings" )
                {
                    Buffer.Add( "This elderling has been driven mad by its fellows.\n" );
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
                    Buffer.Add( "No per unit data?" );
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
                    Buffer.Add( "<color=#ffa1a1>This elderling has been driven mad by its fellows and is rampaging</color>.\n" );
                }
                else
                {
                    debugStage = 1400;
                    if ( data.SanityRemaining > 0 && verbose && faction.SpecialFactionData.InternalName != "MaddenedElderlings" )
                    {
                        debugStage = 1500;
                        Buffer.Add( "This elderling has " ).Add( data.SanityRemaining, "a1ffa1" ).Add( " sanity remaining. When it runs out of sanity it will rampage!\n" );
                    }
                    debugStage = 1600;
                    if ( data.HatchTime > 0 )
                    {
                        debugStage = 1700;
                        int time = data.HatchTime - World_AIW2.Instance.GameSecond;
                        debugStage = 1800;
                        string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time ); //timerColor gets more red the closer the planet is to succumbing
                        Buffer.Add( "This egg will hatch in " ).Add( time.ToString(), timerColor ).Add( " seconds." );
                    }
                    debugStage = 2100;
                    Planet lurePlanet = data.LurePlanet;
                    if ( lurePlanet != null )
                    {
                        debugStage = 2200;
                        Buffer.Add( "This Elderling is being lured to " ).Add( lurePlanet.Name, "ffa1a1" ).Add( ".\n" );
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
                            Buffer.Add( "This elderling will be able to lay an egg in " ).Add( time.ToString(), timerColor ).Add( " seconds." );
                        else
                        {
                            debugStage = 3500;
                            Planet planetForEgg = data.PlanetForEgg;
                            debugStage = 3600;
                            if ( planetForEgg != null && lurePlanet == null )
                            {
                                debugStage = 3700;
                                Buffer.Add( "This elderling is off to lay an egg on " ).Add( planetForEgg.Name, "a1ffa1" ).Add(".");
                            }
                            else
                                Buffer.Add( "This elderling can lay an egg when it chooses to do so." );
                        }
                    }
                    debugStage = 4050;
                    if ( data.NumberOfTimesLeveledUp > 0 ) {
                        Buffer.Add( " This elderling has leveled up " ).Add( data.NumberOfTimesLeveledUp, "a1ffa1" ).Add(" times.");
                    }
                    debugStage = 4100;
                    if ( data.ExperienceRequired > 0 && showData )
                    {
                        debugStage = 4200;
                        string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.ExperienceRequired ); //timerColor gets more red the closer the planet is to succumbing
                        debugStage = 4300;
                        Buffer.Add( " This elderling needs " ).Add( data.ExperienceRequired.ToString(), timerColor ).Add( " experience to level up. " );
                        if ( verbose )
                            Buffer.Add( " Elderlings get experience from a variety of sources, including time passing and invasions of its territory. When this elderling would go above mark level 7, it may transform into a more powerful form." );
                        Buffer.Add( "\n" );
                    }
                    debugStage = 5100;
                    if ( !data.FullyUpgraded && data.ExperienceRequired <= 0 && showData && !RelatedEntityTypeData.GetHasTag( "HighElderling" ) )
                    {
                        debugStage = 5200;
                        Buffer.Add( " This elderling will change form as soon as it is out of combat. " );
                    }
                    debugStage = 5300;
                    if ( data.GetsAFreeTerritoryIfPossible && showData )
                    {
                        debugStage = 5500;
                        Buffer.Add( " This elderling is trying to expand its territory." );
                    }
                    debugStage = 5600;
                    if ( data.Territory.Count > 0 && showData )
                    {
                        debugStage = 5700;
                        Buffer.Add( "\nElderling's Territory:\n" );
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
