using Arcen.AIW2.Core;
using System;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    //This class is the interface for sending a wormhole invasion
    //on the Planet Linking path. The Exogalactic Wormhole code is in the Sentinels Deep Info
    //If a refactor would be worth doing, the immediate wormhole code could be moved here, but I'm not sure its worth the effort right now

    public static class WormholeInvasionManager
    {
        //This is the primary interface function for people who want to use a wormhole invasion
        //it returns true if the invasion was launched
        public static bool LaunchWormholeInvasion( WormholeInvasionOptions options, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return false;
            int debugCode = 0;
            try
            {
                //int numRetries = 100;
                bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.WormholeInvasion );
                debugCode = 100;
                if ( options.ResponsibleAIFaction == null || options.ResponsibleAIFaction.Type != FactionType.AI )
                    options.ResponsibleAIFaction = World_AIW2.GetRandomAIFaction( Context );
                if ( options.ResponsibleAIFaction == null )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Could not find a living AI faction to send this invasion. Have you won the game?", Verbosity.DoNotShow );
                    return false;
                }
                debugCode = 200;
                if ( options.WaveCount <= 0 )
                    options.WaveCount = Context.RandomToUse.Next( 1, 5 );
                if ( options.WaveInterval <= 0 )
                    options.WaveInterval = Context.RandomToUse.Next( 20, 90 );
                debugCode = 300;
                if ( options.ProjectorAppearanceTime <= 0 )
                    options.ProjectorAppearanceTime = World_AIW2.Instance.GameSecond + 1;
                if ( options.PlanetLinkTime <= 0 )
                    options.PlanetLinkTime = World_AIW2.Instance.GameSecond + 120;
                debugCode = 400;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Launching Wormhole Invasion with assaultStrength " + options.AttackStrength + " guardStrength " + options.GuardStrength + " turretStrength " + options.TurretStrength + " current time " + World_AIW2.Instance.GameSecond + " projector appearance time: " + options.ProjectorAppearanceTime + " link time " + options.PlanetLinkTime, Verbosity.DoNotShow );
                Faction wormholeInvasionFaction = FactionUtilityMethods.Instance.GetWormholeInvasionFaction();
                WormholeInvasionFactionBaseInfo wormholeBase = wormholeInvasionFaction.GetExternalBaseInfoAs<WormholeInvasionFactionBaseInfo>();

                debugCode = 500;

                //bool GiveAnnouncement = false;
                if ( options.StartPlanet == null || options.DestinationPlanet == null )
                    GetStartAndDestinationPlanet( ref options, wormholeBase, Context );
                debugCode = 600;
                if ( debug )
                {
                    debugCode = 700;
                    if ( options.StartPlanet == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "we could not find the StartPlanet for a wormhole invasion", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "Using start planet " + options.StartPlanet.Name, Verbosity.DoNotShow );
                    if ( options.DestinationPlanet == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "we could not find the DestinationPlanet for a wormhole invasion", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "Using dest planet " + options.DestinationPlanet.Name, Verbosity.DoNotShow );
                }
                debugCode = 800;
                if ( options.StartPlanet == null || options.DestinationPlanet == null )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No suitable location found", Verbosity.DoNotShow );
                    return false;
                }
                debugCode = 900;
                WormholeInvasionData data = WormholeInvasionData.GetFromPoolOrCreate();
                data.TurretStrength = options.TurretStrength;
                data.GuardStrength = options.GuardStrength;
                data.InvasionStartPlanet = options.StartPlanet;
                data.InvasionDestinationPlanet = options.DestinationPlanet;
                data.ResponsibleAIFactionIdx = options.ResponsibleAIFaction.FactionIndex;
                data.ResponsibleAIFaction = options.ResponsibleAIFaction;
                debugCode = 1000;
                //The time-related numbers might need to be on a per-AI-Difficulty basis
                data.ProjectorAppearanceTime = options.ProjectorAppearanceTime;
                data.PlanetLinkTime = options.PlanetLinkTime;
                if ( options.WaveCount == 0 )
                    throw new Exception( "Somehow our WaveCount was 0" );
                //and now handle the waves
                debugCode = 1100;
                int strengthPerWave = options.AttackStrength / options.WaveCount;
                if ( strengthPerWave < 30 * 1000 )
                    strengthPerWave = 30 * 1000; //make sure these are never so small as to be boring. Wormhole invasions trigger at 300 AIP for difficulty 5, so I think 30 is a reasonable minimum
                for ( int i = 0; i < options.WaveCount; i++ )
                {
                    debugCode = 1200;
                    WormholeWaveData waveData = WormholeWaveData.GetFromPoolOrCreate();
                    debugCode = 1300;
                    waveData.TimeForWave = data.PlanetLinkTime + (i + 1) * Context.RandomToUse.Next( 30, 180 ); //waves are every so often
                    if ( i == 0 )
                        waveData.TimeForWave = data.PlanetLinkTime + 20;
                    debugCode = 1400;
                    FillCompositionForWave( Context, waveData.ShipsInWave, strengthPerWave, options.ResponsibleAIFaction, options );
                    debugCode = 1500;
                    data.WaveData.Add( waveData );
                }
                debugCode = 2000;
                wormholeBase.IncomingInvasionList.Add( data );
                return true;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in LaunchWormholeInvasion debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }

        public static void GetStartAndDestinationPlanet( ref WormholeInvasionOptions options, WormholeInvasionFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.WormholeInvasion );
            try
            {
                debugCode = 100;
                Planet kingPlanet = GetKingPlanet( options.ResponsibleAIFaction );
                if ( kingPlanet == null )
                    return;
                debugCode = 120;

                List<Planet> potentialStartPlanets = Planet.GetTemporaryPlanetList( "WormholeInvMgr-GetStartAndDestinationPlanet-potentialStartPlanets", 10f );
                if ( potentialStartPlanets == null ) //blocked for teardown/shutdown; bail
                    return;

                FactionUtilityMethods.GetPlanetsWithWarpGates( potentialStartPlanets, options.ResponsibleAIFaction, Context );
                if ( potentialStartPlanets.Count == 0 )
                {
                    Planet.ReleaseTemporaryPlanetList( potentialStartPlanets );
                    return;
                }
                debugCode = 140;

                List<Planet> potentialDestinationPlanets = Planet.GetTemporaryPlanetList( "WormholeInvMgr-GetStartAndDestinationPlanet-potentialDestinationPlanets", 10f );
                if ( potentialDestinationPlanets == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList( potentialStartPlanets );
                    return;
                }

                GetWormholeInvasionTargets( potentialDestinationPlanets, potentialStartPlanets, options.ResponsibleAIFaction, options.AttackStrength, BaseInfo, options, Context );
                if ( potentialDestinationPlanets.Count == 0 )
                {
                    Planet.ReleaseTemporaryPlanetList( potentialStartPlanets );
                    Planet.ReleaseTemporaryPlanetList( potentialDestinationPlanets );
                    return;
                }
                debugCode = 200;
                //default option is best option
                Planet defaultStart = null;
                Planet defaultDest = null;
                debugCode = 300;
                for ( int i = 0; i < potentialDestinationPlanets.Count; i++ )
                {
                    Planet potentialDest = potentialDestinationPlanets[i];
                    debugCode = 400;
                    for ( int j = 0; j < potentialStartPlanets.Count; j++ )
                    {
                        debugCode = 500;
                        Planet potentialStart = potentialStartPlanets[j];
                        if ( potentialStart.IntelLevel <= PlanetIntelLevel.Unexplored ||
                             potentialStart.PopulationType == PlanetPopulationType.AIBastionWorld ||
                             potentialStart.PopulationType == PlanetPopulationType.AIHomeworld )
                            continue; //only launch invasions from explored planets
                        if ( potentialStart == potentialDest || potentialStart.GetHopsTo( potentialDest ) <= 1 || potentialStart.GetIsDirectlyLinkedTo( false, potentialDest ) )
                            continue; // Extra protections to stop wormholes from being created on already existing connections
                        bool skipThisStart = false;
                        foreach ( GameEntity_Squad entity in options.ResponsibleAIFaction.Squads( "WormholeProjector" ) )
                        {
                            if ( entity.Planet == potentialStart )
                            {
                                skipThisStart = true;
                                break;
                            }
                        }

                        for ( int k = 0; BaseInfo.IncomingInvasionList != null && k < BaseInfo.IncomingInvasionList.Count; k++ )
                        {
                            debugCode = 600;
                            if ( BaseInfo.IncomingInvasionList[k].InvasionStartPlanet == potentialStart )
                            {
                                skipThisStart = true;
                                break;
                            }
                            if ( BaseInfo.IncomingInvasionList[k].InvasionDestinationPlanet == potentialStart
                                    && BaseInfo.IncomingInvasionList[k].InvasionStartPlanet == potentialDest) {
                                skipThisStart = true;
                                break;
                            }
                        }
                        if ( skipThisStart )
                            continue; //we already have a projector here
                        debugCode = 700;
                        int maxCrosses = 2;
                        if ( !WormholeInvasionManager.WouldLinkCrossOtherPlanets( potentialStart, potentialDest, maxCrosses ) )
                        {
                            debugCode = 800;
                            if ( defaultStart == null )
                            {
                                defaultStart = potentialStart;
                                defaultDest = potentialDest;
                            }
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "we have as an option " + potentialStart.Name + " and " + potentialDest.Name, Verbosity.DoNotShow );
                            //potential TODO: instead make a weighted list of possible options and choose that way
                            int random = Context.RandomToUse.Next( 0, 100 );
                            if ( random < 40 )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "randomly chosen pair; " + random, Verbosity.DoNotShow );
                                //use these choices
                                options.StartPlanet = potentialStart;
                                options.DestinationPlanet = potentialDest;

                                Planet.ReleaseTemporaryPlanetList( potentialStartPlanets );
                                Planet.ReleaseTemporaryPlanetList( potentialDestinationPlanets );
                                return;
                            }
                        }
                    }
                }
                debugCode = 900;
                if ( options.StartPlanet == null )
                {
                    //well, we didn't pick anything. If we did pick a default though, use that
                    options.StartPlanet = defaultStart;
                    options.DestinationPlanet = defaultDest;
                }

                Planet.ReleaseTemporaryPlanetList( potentialStartPlanets );
                Planet.ReleaseTemporaryPlanetList( potentialDestinationPlanets );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GetStartAndDestinationPlanet debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        //Set immediately before ListToFill.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since this is sim-context planning code.
        [ThreadStatic] private static Dictionary<Planet, int> cb_wormholeScores;
        [ThreadStatic] private static Faction cb_wormholeSortFaction;

        public static void GetWormholeInvasionTargets( List<Planet> ListToFill, List<Planet> potentialStartPlanets, Faction faction, int attackStrength, WormholeInvasionFactionBaseInfo BaseInfo, WormholeInvasionOptions options, ArcenHostOnlySimContext Context )
        {
            ListToFill.Clear();
            AIDifficulty highest = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();

            Dictionary<Planet, int> scoresForPlanets = Planet.GetTemporaryPlanetDictOfInts( "WormholeInvasionManager-GetWormholeInvasionTargets-scoresForPlanets", 10f );
            if ( scoresForPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.WormholeInvasion );
            foreach ( Planet target in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                //some of this logic is borrowed from the wormhole invasion logic
                if ( potentialStartPlanets.Contains( target ) )
                    continue;
                //its a player faction
                if ( FactionUtilityMethods.Instance.HasPlayerKing( target ) &&
                     highest.Difficulty < 9 ) //being at high difficulty is hard
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Path A for " + target.Name + ", too close to player homeworld", Verbosity.DoNotShow );
                    continue;
                }
                Faction controllingFaction = target.GetControllingOrInfluencingFaction();
                if ( !controllingFaction.GetIsHostileTowards( faction ) ||
                     !FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( controllingFaction ) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Path B for " + target.Name + ", this planet is owned by enemies of the humans", Verbosity.DoNotShow );
                    continue; //the faction owning this planet is not allied to a player. Just ignore it
                }

                var pFaction = target.GetStanceDataForFaction( faction );

                int defensiveStrength = pFaction[FactionStance.Hostile].TotalStrength;
                if ( defensiveStrength >= attackStrength / 2 && !options.ForceInvasionLaunch ) //too strong for us, and we are actually factoring in defensive strength
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Path C for " + target.Name + ", defensive strength " + defensiveStrength + " too strong for " + attackStrength + " / 2", Verbosity.DoNotShow );

                    continue;
                }
                //this check is probably more expensive, so save to toward the end
                if ( target.TypeData.Type == PlanetType.Nomad &&
                     !(target.IsDisabledNomad || World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy) ) //no nomads (except disabled ones or when we are in a nomad galaxy)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Path D for " + target.Name + "; nomad", Verbosity.DoNotShow );

                    continue;
                }

                int score = GetInvasionScoreForPlanet( target, attackStrength, faction, Context );
                bool adjustScore = false;
                for ( int j = 0; j < BaseInfo.IncomingInvasionList.Count; j++ )
                {
                    if ( BaseInfo.IncomingInvasionList[j].InvasionDestinationPlanet == target )
                    {
                        adjustScore = true;
                        break;
                    }
                }
                if ( adjustScore )
                {
                    score -= 1;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "adjusting score, since we already have a wormhole to here", Verbosity.DoNotShow );
                }
                if ( options.ForceInvasionLaunch && scoresForPlanets.Count == 0 )
                if ( score <= 0 )
                {
                    if ( options.ForceInvasionLaunch && scoresForPlanets.Count == 0 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Path E for " + target.Name + "; this score (" + score + ") is bad, but we are accepting anyway to make sure we have at least one target", Verbosity.DoNotShow );
                    }
                    else
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Path E for " + target.Name + "; this score (" + score + ") is just too bad", Verbosity.DoNotShow );
                        continue;
                    }
                }
                scoresForPlanets[target] = score;

                ListToFill.Add( target );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Identifying " + target.Name + " as a potential target with score " + scoresForPlanets[target], Verbosity.DoNotShow );
            }
            if ( ListToFill.Count == 0 )
            {
                Planet.ReleaseTemporaryPlanetDictOfInts( scoresForPlanets );
                return;
            }
            cb_wormholeScores = scoresForPlanets;
            cb_wormholeSortFaction = faction;
            ListToFill.Sort( static delegate ( Planet Left, Planet Right )
            {
                int lScore = cb_wormholeScores[Left];
                int rScore = cb_wormholeScores[Right];
                if ( lScore != rScore )
                    return rScore.CompareTo( lScore );
                var lFaction = Left.GetStanceDataForFaction( cb_wormholeSortFaction );
                var rFaction = Right.GetStanceDataForFaction( cb_wormholeSortFaction );
                int lStrength = lFaction[FactionStance.Hostile].TotalStrength;
                int rStrength = rFaction[FactionStance.Hostile].TotalStrength;
                return rStrength.CompareTo( lStrength );
            } );
            Planet.ReleaseTemporaryPlanetDictOfInts( scoresForPlanets );
        }
        public static int GetInvasionScoreForPlanet( Planet planet, int myStrength, Faction faction, ArcenHostOnlySimContext Context )
        {
            int score = 0;
            bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.WormholeInvasion );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "\tCalculating the score for " + planet.Name + ", my attacking strength is " + myStrength, Verbosity.DoNotShow );
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    var pFaction = secondaryPlanet.GetStanceDataForFaction( faction );
                    if ( myStrength / 2 < pFaction[FactionStance.Hostile].TotalStrength )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t\tdo not search through " + secondaryPlanet.Name + ", defensive Strength " + pFaction[FactionStance.Hostile].TotalStrength + " my strength " + myStrength, Verbosity.DoNotShow );
                        return PropogationEvaluation.No;
                    }
                    if ( secondaryPlanet.GetHopsTo( planet ) > 5 )
                        return PropogationEvaluation.No;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet otherPlanet = _phd.Planet;
                var pFaction = otherPlanet.GetStanceDataForFaction( faction );
                int defensiveStrength = pFaction[FactionStance.Hostile].TotalStrength;
                if ( otherPlanet.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    continue;
                if ( myStrength / 2 > defensiveStrength )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\t" + otherPlanet.Name + " counts as a score; defensiveStrength " + defensiveStrength + " and attack strength: " + myStrength, Verbosity.DoNotShow );
                    score++;
                    if ( planet == otherPlanet )
                        score++; //we are particularly excited about this planet
                }
                else if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t\t" + otherPlanet.Name + " does not count; " + defensiveStrength + " and attack strength: " + myStrength, Verbosity.DoNotShow );
            }
            if ( score == 0 )
            {
                //if we found this was a bad target, return a negative enemy-strength to indicate just ohow bad this it was
                var pFactionTarget = planet.GetStanceDataForFaction( faction );
                return -pFactionTarget[FactionStance.Hostile].TotalStrength;
            }
            return score;
        }
        public static Planet GetKingPlanet( Faction faction )
        {
            GameEntity_Squad king = null;
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                king = entity;
                break;
            }
            if ( king == null )
                return null;
            return king.Planet;
        }
        public static bool WouldLinkCrossOtherPlanets( Planet First, Planet Second, int maxCrosses = 1 )
        {
            //stolen with some tweaks from from MapGeneration.cs
            List<Planet> PlanetsToNoNotCrossOver = First.ParentGalaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs();
            bool linkGoesDirectlyOverAnotherPlanet = false;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Checking whether " + First.Name + " and " + Second.Name + " can be linked safely. We allow up to " + maxCrosses + " crossed wormholes", Verbosity.DoNotShow );
            int crosses = 0;
            foreach ( Planet planetToNotHit in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                if ( planetToNotHit == First || planetToNotHit == Second || planetToNotHit.HasPlanetBeenDestroyed )
                    continue;
                if ( Mat.LineIntersectsRectangleContainingCircle( First.GalaxyLocation, Second.GalaxyLocation, planetToNotHit.GalaxyLocation, planetToNotHit.TypeData.IntraStellarRadius ) )
                {
                    //the new link would go directly over another planet
                    linkGoesDirectlyOverAnotherPlanet = true;
                    break;
                }
                ArcenPoint crossP1 = planetToNotHit.GalaxyLocation;
                foreach ( Planet linkedPlanet in planetToNotHit.LinkedNeighbors( false ) )
                {
                    ArcenPoint crossP2 = linkedPlanet.GalaxyLocation;
                    if ( linkedPlanet == First || linkedPlanet == Second )
                        continue;
                    if ( Mat.LineSegmentIntersectsLineSegment( First.GalaxyLocation, Second.GalaxyLocation, crossP1, crossP2, planetToNotHit.TypeData.IntraStellarRadius ) )
                        crosses++;
                    if ( crosses > maxCrosses )
                        break;
                }
                if ( crosses > maxCrosses )
                    break;
            }
            if ( linkGoesDirectlyOverAnotherPlanet )
            {
                return true;
            }
            if ( crosses > maxCrosses )
            {
                return true;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "We can safely link " + First.Name + " and " + Second.Name, Verbosity.DoNotShow );
            return false;
        }
        public static void GetCompositionForWave( Dictionary<GameEntityTypeData, int> composition, ArcenHostOnlySimContext Context, int strength, Faction spawningFaction, WormholeInvasionOptions options )
        {
            FillCompositionForWave( Context, composition, strength, spawningFaction, options );
        }

        public static void FillCompositionForWave( ArcenHostOnlySimContext Context, Dictionary<GameEntityTypeData, int> composition, int strength, Faction spawningFaction, WormholeInvasionOptions options )
        {
            composition.Clear();

            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> fleetshipBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> guardianBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> direGuardianBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> exoleaderBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );

            //AITypeData aiType = spawningFaction.GetSentinelsExternal().AIType;

            bool debug = false;
            if ( debug ) { }
            //I need to ask the faction for their guardians, dire_guardians and normal menus
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( !otherFaction.GetIsFriendlyTowards( spawningFaction ) )
                    continue;
                if ( !(otherFaction.SpecialFactionData.InternalName == "AI") )
                    continue;
                AITypeData otherFactionAIType = otherFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                AIShipGroupCategory shipGroupCat = null;
                AIShipGroup shipGroup = null;
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Wave].NormalAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        fleetshipBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].GuardianAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        guardianBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].DireGuardianAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        direGuardianBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].ExoLeaderAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        exoleaderBagToFill.CopyFrom( shipGroup.DrawBag );
                }

            }
            if ( debug )
            {
                for ( int i = 0; i < fleetshipBagToFill.InternalListSize; i++ )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Considering fleetship " + i + ": " + fleetshipBagToFill.GetInternalListItemAtIndex( i ).InternalName + " cost " + fleetshipBagToFill.GetInternalListItemAtIndex( i ).CostForAIToPurchase + " in FillCompositionForWave", Verbosity.DoNotShow );
                }
                for ( int i = 0; i < guardianBagToFill.InternalListSize; i++ )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Considering guardian " + i + ": " + guardianBagToFill.GetInternalListItemAtIndex( i ).InternalName + " cost " + guardianBagToFill.GetInternalListItemAtIndex( i ).CostForAIToPurchase + " in FillCompositionForWave", Verbosity.DoNotShow );
                }
                for ( int i = 0; i < direGuardianBagToFill.InternalListSize; i++ )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Considering dire guardian " + i + ": " + direGuardianBagToFill.GetInternalListItemAtIndex( i ).InternalName + " cost " + direGuardianBagToFill.GetInternalListItemAtIndex( i ).CostForAIToPurchase + " in FillCompositionForWave", Verbosity.DoNotShow );
                }

                for ( int i = 0; i < exoleaderBagToFill.InternalListSize; i++ )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Considering exoleader " + i + ": " + exoleaderBagToFill.GetInternalListItemAtIndex( i ).InternalName + " cost " + exoleaderBagToFill.GetInternalListItemAtIndex( i ).CostForAIToPurchase + " in FillCompositionForWave", Verbosity.DoNotShow );
                }
            }

            int unusedParameter = 0;
            //We spawn guardians and dire guardians here, with the amounts evenly split
            spawningFaction.FillComposition( Context, options.GuardStrength / 2, unusedParameter, composition, direGuardianBagToFill,
                                             //this is using spawningFaction.CurrentGeneralMarkLevel in order to have these pop out at the mark level of the spawning faction
                                             //should be correct since this is apparently generally used for exogalactic strike forces
                                             spawningFaction.CurrentGeneralMarkLevel, 0 );

            spawningFaction.FillComposition( Context, options.GuardStrength / 2, unusedParameter, composition, guardianBagToFill,
                                             //this is using spawningFaction.CurrentGeneralMarkLevel in order to have these pop out at the mark level of the spawning faction
                                             //should be correct since this is apparently generally used for exogalactic strike forces
                                             spawningFaction.CurrentGeneralMarkLevel, 0 );
        }

    }
}
