using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public partial class AISentinelsFactionBaseInfo
    {
        //Set immediately before finalSpawnPoints.Sort(...) so the sort comparison can be a
        //non-capturing static delegate.  [ThreadStatic] because AI wave planning runs on worker threads.
        [ThreadStatic]
        private static Faction cb_waveSortAttachedFaction;

        private const int MAX_WAVE_TIME_FOR_NORMAL_WAVES = 180; //may as well hardcode this

        #region PlanWave_OrGetNull
        public PlannedWave PlanWave_OrGetNull( ArcenHostOnlySimContext Context, int budget, PlannedWaveOptions Options )
        {
            int debugCode = 0;
            try
            {
                #region Tracing
                //                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave ) && !Options.isReconquestWave;
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISentinelsFactionBaseInfo-PlanWave_OrGetNull-tracing", 10f ) : null;
                #endregion
                debugCode = 100;

                string factionName = "default";
                if ( Options.targetFaction != null )
                {
                    factionName = Options.targetFaction.SpecialFactionData.InternalName;

                    if ( AttachedFaction.GetIsHostileTowards(Options.targetFaction) == false )
                    {
                        // Uhhhhh... why is someone planning a wave from us targeting our allies??
                        tracingBuffer.Add( "Planning a wave with budget " + budget + " isReconquestWave: " + Options.isReconquestWave + " targetFaction: " + factionName + " requiredWaveToDefenseRatio: " + Options.requiredWaveToDefenseRatio ).Add( "But we aren't hostile to that faction so aborting!\n" );
                        return null;
                    }
                }

                PlannedWave wave = PlannedWave.GetFromPoolOrCreate();
                
                if ( tracing )
                    tracingBuffer.Add( "Planning a wave with budget " + budget + " isReconquestWave: " + Options.isReconquestWave + " targetFaction: " + factionName + " requiredWaveToDefenseRatio: " + Options.requiredWaveToDefenseRatio ).Add( "\n" );

                wave.DebugString += "Planning a wave with budget " + budget + " isReconquestWave: " + Options.isReconquestWave + " targetFaction: " + factionName + " requiredWaveToDefenseRatio: " + Options.requiredWaveToDefenseRatio.ReadableString;

                debugCode = 200;
                var aip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
                int waveInterval = this.GetSpecificBudgetSpendingInterval( AIBudgetType.Wave, aip );
                if ( Options.overrideLaunchTime > 0 )
                    waveInterval = Options.overrideLaunchTime - World_AIW2.Instance.GameSecond;
                //if in reconquest mode, make sure we get lots of time for warning
                if ( Options.isReconquestWave )
                    waveInterval = this.GetSpecificBudgetSpendingInterval( AIBudgetType.Reconquest, aip );

                debugCode = 250;
                if ( tracing )
                    tracingBuffer.Add( "Finding wave warning time" ).Add( "\n" );
                if ( waveInterval > MAX_WAVE_TIME_FOR_NORMAL_WAVES )
                    waveInterval = MAX_WAVE_TIME_FOR_NORMAL_WAVES;

                wave.gameTimeInSecondsForLaunchWave = waveInterval + World_AIW2.Instance.GameSecond;
                wave.SendingFactionIndex = AttachedFaction.FactionIndex;
                string waveWarning = World_AIW2.Instance.Setup.GetStringBySetting( "WaveWarning" );
                Helper_DetermineWaveWarningTime( ref wave, waveWarning, waveInterval );

                //the refund ratio could be altered to have AI type/difficulty modifiers if desired
                wave.cancelRefundRatioForNextWave = FInt.FromParts( 0, 750 );
                wave.cancelRefundRatioForNextWormholeInvasion = FInt.FromParts( 0, 100 );
                debugCode = 400;

                if ( Options.ForceTargetPlanetIndex != -1 && 
                     Options.ForcePlanetWithWarpGateIndex != -1 )
                {
                    debugCode = 401;
                    //This is a wave from the tutorial


                    if ( Options.ForceCompositionIfFilled.Count == 0 )
                    {
                        debugCode = 402;
                        PlanetWaveComposition comp = Helper_GetWaveCompositionWrapper( Context, budget, Options, World_AIW2.Instance.GetPlanetByIndex( Options.ForceTargetPlanetIndex ), tracing, tracingBuffer );
                        wave.FinalComposition.ClearAndCopyFrom( comp.Composition );
                        comp.ReturnToPool(); //prevent a leak
                    }
                    else
                    {
                        debugCode = 403;
                        wave.FinalComposition.ClearAndCopyFrom( Options.ForceCompositionIfFilled );
                    }
                    
                    debugCode = 404;
                    wave.planetWithWarpGateIdx = Options.ForcePlanetWithWarpGateIndex;
                    debugCode = 405;
                    wave.targetPlanetIdx = Options.ForceTargetPlanetIndex;
                }
                else 
                if ( Options.overrideEntityToSpawnAt == null )
                {
                    debugCode = 410;
                    if ( tracing ) tracingBuffer.Add( "Finding target planet" ).Add( "\n" );
                    
                    //for normal waves
                    KeyValuePair<PlanetWaveComposition, Planet> pair = this.ChooseWaveTarget( Context, budget, Options );
                    
                    PlanetWaveComposition waveComp = pair.Key;
                    Planet wavePlanet = pair.Value;
                    
                    debugCode = 411;
                    if ( wavePlanet == null )
                    {
                        debugCode = 412;
                        if ( Options.targetFaction == null )
                        {
                            if ( tracing ) tracingBuffer.Add( "No valid targets were found." );
                        }
                        else
                        {
                            if ( tracing ) tracingBuffer.Add( "No valid targets for chosen faction." );
                        }
                        
                        if ( tracing )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                            tracingBuffer = null;
                        }
                        
                        debugCode = 413;
                        waveComp?.ReturnToPool();
                        
                        return null;
                    }
                    
                    if ( waveComp == null )
                    {
                        if ( tracing ) 
                        {
                            tracingBuffer.Add( "waveComp was null for some reason!." );
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                        }
                        
                        return null;
                    }
                    
                    debugCode = 414; 
                    wave.FinalComposition.ClearAndCopyFrom( waveComp.Composition );
                    debugCode = 415;
                    wave.aiCostBudgetForWave = waveComp.spentBudget;
                    debugCode = 416;
                    wave.planetWithWarpGateIdx = waveComp.FromPlanet.Index;
                    debugCode = 417;
                    wave.targetPlanetIdx = wavePlanet.Index;
                    
                    debugCode = 420;
                    
                    //Check the owning faction of the target planet. If the owning faction is
                    //the AI then this is a threat fleet. This is likely because the humans have no Warp Gates
                    //adjacent to any of their planets. 
                    Faction owningFaction = wavePlanet.GetControllingFaction();
                    if ( owningFaction != null && 
                         owningFaction.Type == FactionType.AI && 
                         owningFaction.GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        if ( tracing ) tracingBuffer.Add( "The AI owns the target planet (" + wavePlanet.Name + "), so threat fleet. Source planet " + waveComp?.FromPlanet?.Name ).Add( "\n" );
                        wave.spawnWaveDirectlyOnTarget = true;
                    }
                    else
                    {
                        debugCode = 425;
                        if ( tracing )
                            tracingBuffer.Add( "Determine wave type" ).Add( "\n" );

                        Helper_DetermineWaveType( ref wave, Context );
                        
                        debugCode = 426;
                        
                        //signal the target faction. This is used only for notifications.
                        //If the planet is owned by the player,
                        //it is always the target (this is for the case where a minor faction calls for
                        //a wave, and it is going to a planet with both the minor faction and the player)
                        if ( wavePlanet.GetControllingFactionType() == FactionType.Player )
                        {
                            wave.TargetFactionIndex = wavePlanet.GetControllingFaction().FactionIndex;
                        }
                        else if ( Options.targetFaction != null )
                        {
                            wave.TargetFactionIndex = Options.targetFaction.FactionIndex;
                        }
                        else
                        {
                            Faction controllingFaction = wavePlanet.GetControllingFaction();
                            if ( controllingFaction.GetIsHostileTowards( AttachedFaction ) && !controllingFaction.SpecialFactionData.AIShouldNeverHaveHunterAgainstThis )
                                wave.TargetFactionIndex = controllingFaction.FactionIndex;
                        }
                    }

                    //final cleanup
                    waveComp.ReturnToPool(); //prevent a leak
                }
                else
                {
                    debugCode = 460;
                    
                    //for hacking waves and exo wormhole waves
                    
                    wave.overrideEntityToSpawnAt = Options.overrideEntityToSpawnAt;
                    if ( tracing ) tracingBuffer.Add( "Using Override Entity " ).Add( wave.overrideEntityToSpawnAt.ToString() ).Add( "\n" );
                    
                    debugCode = 461;
                    PlanetWaveComposition comp = Helper_GetWaveCompositionWrapper(  Context, budget, Options, Options.overrideEntityToSpawnAt.Planet, tracing, tracingBuffer );

                    debugCode = 462;
                    wave.FinalComposition.ClearAndCopyFrom( comp.Composition );
                    debugCode = 463;
                    wave.aiCostBudgetForWave = comp.spentBudget;
                    debugCode = 464;
                    wave.planetWithWarpGateIdx = comp.FromPlanet.Index;
                    debugCode = 465;
                    wave.targetPlanetIdx = comp.FromPlanet.Index;
                    debugCode = 466;
                    //final cleanup
                    comp.ReturnToPool(); //prevent a leak
                }
                
                debugCode = 500;

                Planet sourcePlanet = null;
                Planet targetPlanet = null;
                if ( Options.overrideEntityToSpawnAt != null )
                {
                    targetPlanet = Options.overrideEntityToSpawnAt.Planet;
                    sourcePlanet = targetPlanet;
                }
                else
                {
                    targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                    sourcePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                }

                if ( wave.secondsAdvanceWarningToGive > waveInterval )
                    wave.secondsAdvanceWarningToGive = (Int16)waveInterval; //don't give longer warning than the wave will take
                
                if ( tracing ) tracingBuffer.Add( "Sending wave with budget " + budget + " at " + wave.gameTimeInSecondsForLaunchWave + " (interval " + waveInterval + " current time " + World_AIW2.Instance.GameSecond + ") and gving " + wave.secondsAdvanceWarningToGive + " seconds of advanced warning" ).Add( "\n" );

                if ( targetPlanet == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Bug: PlanWave had no target planet", Verbosity.DoNotShow );
                
                debugCode = 700;

                if ( wave.FinalComposition == null ) 
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                    return null; //this is a weird bug
                }

                wave.isReconquestWave = Options.isReconquestWave;
                debugCode = 800;
                
                //AI Waves during the civil war need to be more interesting
                Planet targetPlanetHere = null;
                if ( Options.overrideEntityToSpawnAt != null )
                    targetPlanetHere = Options.overrideEntityToSpawnAt.Planet;
                else
                    targetPlanetHere = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                if ( targetPlanetHere == null )
                {
                    tracingBuffer.Add( "BUG: no target planet" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                    return null;
                }
                
                Faction factionOwningPlanet = targetPlanetHere.GetControllingFaction();
                debugCode = 810;
                if ( AttachedFaction.GetIsHostileTowards( factionOwningPlanet ) && factionOwningPlanet.Type == FactionType.AI &&
                    AttachedFaction.InCivilWarMode )
                {
                    //This is against a hostile AI faction, make the wave more dangerous
                    //                  int old = wave.aiCostBudgetForWave;
                    wave.aiCostBudgetForWave = (wave.aiCostBudgetForWave * 3) / 2;
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("Anti-AI wave headed toward " + targetPlanet.Name + " is increased in power from " + old + " to " + wave.aiCostBudgetForWave, Verbosity.DoNotShow );
                }

                if ( tracing )
                {
                    if (wave.FinalComposition == null)
                        tracingBuffer.Add( "BUG: wave.FinalComposition is null" );
                    tracingBuffer.Add( "Planned wave:  advance warning to give " + wave.secondsAdvanceWarningToGive + ". ai cost budget of wave " + wave.aiCostBudgetForWave ).Add( "\n" );
                }

                if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Spawn wave in " + (wave.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond) + " seconds", Verbosity.DoNotShow );

                #region Tracing
                if ( tracing )
                {
                    tracingBuffer.Add( this.TracingName ).Add( " faction " ).Add( AttachedFaction.FactionIndex ).Add( ": " ).Add( this.SentinelInfo.AIType.DisplayName ).Add( " PlanWave ends" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
                    tracingBuffer = null;
                }
                #endregion

                return wave;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PlanWave. DebugCode " + debugCode + ": " + e, Verbosity.DoNotShow );
            }
            return null;
        }
        #endregion

        #region Helper_DetermineWaveWarningTime
        private void Helper_DetermineWaveWarningTime( ref PlannedWave wave, string waveWarning, int waveInterval )
        {
            //To figure out when to give the warning for the next wave we
            //check what setting the user requested, then pick the appropriate value
            //from ExternalConstants. We pick the shortest of "X seconds or Y% of the wave spawn rate"
            //in case you have some very quick wave spawn rates
            // Puffin note: Swapped to use the shortest of the warning times in ExternalConstants, or just the interval of the thing being spawned.
            // These numbers get kind of strange when trying to alter wave timings...plus it led to normal wave intervals being 10 minutes, but Medium (default) had 8 minute
            // warning. They're also really confusing for me...Should still mean Reconquests have the same warnings...unless you're using short, in which case they're mega fast!
            if ( ArcenStrings.Equals( waveWarning, "None" ) )
            {
                wave.secondsAdvanceWarningToGive = (Int16)wavewarningnone;
            }
            else if ( ArcenStrings.Equals( waveWarning, "Short" ) )
            {
                wave.secondsAdvanceWarningToGive = (Int16)Math.Min( wavewarningshort, waveInterval );
            }
            else if ( ArcenStrings.Equals( waveWarning, "Medium" ) )
            {
                wave.secondsAdvanceWarningToGive = (Int16)Math.Min( wavewarningmedium, waveInterval );
            }
            else if ( ArcenStrings.Equals( waveWarning, "Long" ) )
            {
                wave.secondsAdvanceWarningToGive = (Int16)Math.Min( wavewarninglong, waveInterval );
            }
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Unknown wave warning type <" + waveWarning + ">", Verbosity.DoNotShow );
            }
        }
        #endregion end Helper_DetermineWaveWarningTime

        #region Helper_DetermineWaveType
        private void Helper_DetermineWaveType( ref PlannedWave wave, ArcenHostOnlySimContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISentinelsFactionBaseInfo-Helper_DetermineWaveType-tracing", 10f ) : null;
            #endregion
            
            //This is a helper function for determining the wave type (ie. Direct Wave, Cross Planet Wave)
            int randomNumber = Context.RandomToUse.Next( 0, 3 );
            
            bool directWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "DirectWave" );
            bool crossPlanetWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "CrossPlanetWave" );

            if ( randomNumber == 0 )
            {
                if ( directWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a direct wave. " ).Add( "\n" );
                    wave.spawnWaveDirectlyOnTarget = true;
                }
                if ( crossPlanetWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a cross planet wave" ).Add( "\n" );
                    wave.spawnWaveDirectlyOnTarget = false;
                }
            }
            else if ( randomNumber == 1 )
            {
                if ( directWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a direct wave" );
                    wave.spawnWaveDirectlyOnTarget = true;
                }
                if ( crossPlanetWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a cross planet wave" );
                    wave.spawnWaveDirectlyOnTarget = false;
                }
            }
            else
            {
                if ( crossPlanetWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a cross planet wave" );
                    wave.spawnWaveDirectlyOnTarget = false;
                }
                if ( directWavesOn )
                {
                    if ( tracing )
                        tracingBuffer.Add( "spawning a direct wave" );
                    wave.spawnWaveDirectlyOnTarget = true;
                }
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion end Helper_DetermineWaveType

        #region ChooseWaveTarget

        private KeyValuePair<PlanetWaveComposition, Planet> ChooseWaveTarget( ArcenHostOnlySimContext Context, int budget, PlannedWaveOptions Options )
        {
            int debugStage = 0;
            KeyValuePair<PlanetWaveComposition, Planet> spawnLocation = new KeyValuePair<PlanetWaveComposition, Planet>();

            List<KeyValuePair<PlanetWaveComposition, Planet>> basicSpawnPoints = TempCollectionHelper.GetTemporaryKVPairListPlanetWaveCompToPlanet( "AISentinelsFactionBaseInfoWavePlanning-basicSpawnPoints", 10f );
            if ( basicSpawnPoints == null ) //blocked for teardown/shutdown; bail
                return spawnLocation;
            List<KeyValuePair<PlanetWaveComposition, Planet>> preferredSpawnPoints = TempCollectionHelper.GetTemporaryKVPairListPlanetWaveCompToPlanet( "AISentinelsFactionBaseInfoWavePlanning-preferredSpawnPoints", 10f );
            if ( preferredSpawnPoints == null ) //blocked for teardown/shutdown; bail
            {
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( basicSpawnPoints ); //release already-acquired temps before bailing (finally not yet entered)
                return spawnLocation;
            }
            List<KeyValuePair<PlanetWaveComposition, Planet>> finalSpawnPoints = basicSpawnPoints;

            List<PlanetWaveComposition> possibleLaunchPlanets = TempCollectionHelper.GetTemporaryListOfPlanetComposition( "AISentinelsFactionBaseInfoWavePlanning-possibleLaunchPlanets", 10f );
            if ( possibleLaunchPlanets == null ) //blocked for teardown/shutdown; bail
            {
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( basicSpawnPoints ); //release already-acquired temps before bailing (finally not yet entered)
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( preferredSpawnPoints );
                return spawnLocation;
            }
            List<PlanetWaveComposition> preferredPossibleLaunchPlanets = TempCollectionHelper.GetTemporaryListOfPlanetComposition( "AISentinelsFactionBaseInfoWavePlanning-preferredPossibleLaunchPlanets", 10f );
            if ( preferredPossibleLaunchPlanets == null ) //blocked for teardown/shutdown; bail
            {
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( basicSpawnPoints ); //release already-acquired temps before bailing (finally not yet entered)
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( preferredSpawnPoints );
                TempCollectionHelper.ReleaseTemporaryListOfPlanetComposition( possibleLaunchPlanets );
                return spawnLocation;
            }

            try
            {
                debugStage = 1000;
                //figures out where a wave should go
                bool debug = false;
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISentinelsFactionBaseInfo-ChooseWaveTarget-tracing", 10f ) : null;

                debugStage = 2000;

                //List<PlanetWaveComposition> fallbackSpawnPoints = List<PlanetWaveComposition>.Create_WillNeverBeGCed();
                Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
                debugStage = 2100;
                
                //this lookup first has the planet with the Warp Gate, then it has
                //the target Enemy (here Enemy typically means Human) planet.
                //Originally this code just checked for warp gates belonging to this AI faction, but for the multi-AI case we now allow
                //for each AI faction to share warp gates
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    Faction aiFaction = World_AIW2.Instance.AIFactions[i];

                    debugStage = 2200;
                    if ( aiFaction.GetIsHostileTowards( AttachedFaction ) ) //AI factions don't share warp gates in civil war mode
                        continue;

                    debugStage = 2300;
                    foreach ( GameEntity_Squad entity in aiFaction.Squads( EntityRollupType.WarpEntryPoints ) )
                    {
                        debugStage = 2400;
                        
                        //TODO: actually do this properly
                        //currently exogalactic wormholes are the only Warp Beacons, and they need to have waves sent through them by the wormhole invasion, but they also need to not be used for regular waves
                        //This is a very band-aid solution
                        if ( entity.TypeData.IsWarpBeacon )
                            continue;
                        
                        bool addToPossible = false;
                        bool addToPossiblePreferred = false;
                        
                        //bool addToFallbacks = false;
                        if ( !Helper_ContainsPlanet( possibleLaunchPlanets, entity.Planet ) )
                            addToPossible = true;
                        
                        if ( entity.TypeData.ProvidesAIWarpEntryPoint && !Helper_ContainsPlanet( preferredPossibleLaunchPlanets, entity.Planet ) )
                            addToPossiblePreferred = true;

                        debugStage = 3200;
                        if ( addToPossible || addToPossiblePreferred )//|| addToFallbacks )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Finding valid composition for a potential wave coming from " + entity.Planet.Name + ".\n" );
                            
                            PlanetWaveComposition comp = Helper_GetWaveCompositionWrapper( Context, budget, Options, entity.Planet, tracing, tracingBuffer );
                            if ( comp.FromPlanet == null ) //if couldn't find anything for whatever reason
                                continue;

                            if ( addToPossible )
                                possibleLaunchPlanets.Add( comp );
                            
                            if ( addToPossiblePreferred )
                                preferredPossibleLaunchPlanets.Add( comp );
                            
                            //if ( addToFallbacks )
                            //    fallbackSpawnPoints.Add( comp );
                        }
                    }
                }

                debugStage = 8000;

                for ( int i = 0; i < possibleLaunchPlanets.Count; i++ )
                {
                    //TODO: combine this logic and the preferredPossibleLaunchPlanets logic into one function that can be used in both places
                    //we've seen bugs from this logic sometimes being inconsistent
                    PlanetWaveComposition comp = possibleLaunchPlanets[i];
                    debugStage = 9000;
                    if ( comp.FromPlanet == null )
                        throw new Exception( "Null FromPlanet on PlanetWaveComposition!" );
                    
                    debugStage = 9100;
                    foreach ( Planet neighbor in comp.FromPlanet.LinkedNeighbors( false ) )
                    {
                        int linkedNeighborDebugLevel = 1;
                        try
                        {
                            if ( neighbor == null )
                                continue;

                            linkedNeighborDebugLevel = 100;

                            if ( tracing )
                            {
                                linkedNeighborDebugLevel = 101;
                                tracingBuffer.Add( "checking " + comp.FromPlanet.Name );
                                linkedNeighborDebugLevel = 102;
                                tracingBuffer.Add( " ==> " );
                                linkedNeighborDebugLevel = 103;
                                tracingBuffer.Add( neighbor.Name );
                                tracingBuffer.Add( " as possible launch planet.\n" );
                                linkedNeighborDebugLevel = 104;
                                if ( Options.targetFaction == null )
                                    tracingBuffer.Add( " target faction null. " );
                                else
                                {
                                    linkedNeighborDebugLevel = 106;
                                    tracingBuffer.Add( "target faction " ).Add( Options.targetFaction.ToString() ).Add( ".\n" );
                                }
                            }
                            linkedNeighborDebugLevel = 107;
                            if ( Options.targetFaction != null )
                            {
                                linkedNeighborDebugLevel = 110;
                                //if we have specified a required faction
                                if ( neighbor.GetControllingFaction() != Options.targetFaction &&
                                          neighbor.GetFactionWithSpecialInfluenceHere() != Options.targetFaction )
                                {
                                    if ( tracing ) tracingBuffer.Add( "Exit A, wave was against " + Options.targetFaction.GetDisplayName() + "\n" );
                                    continue;
                                }
                            }
                            linkedNeighborDebugLevel = 200;
                            //if we can choose any planet, but it must be the a planet we can take
                            if ( AttachedFaction.GetIsFriendlyTowards( neighbor.GetControllingFaction() ) )
                            {
                                if ( tracing ) tracingBuffer.Add( "Exit B (friendly toward controlling faction)\n" );
                                continue;
                            }

                            linkedNeighborDebugLevel = 300;
                            var myFaction = neighbor.GetStanceDataForFaction( AttachedFaction );
                            linkedNeighborDebugLevel = 310;
                            int hostileTurretStrength = myFaction[FactionStance.Hostile].TurretStrength;
                            linkedNeighborDebugLevel = 320;
                            Faction influencerOrNull = null;
                            if ( neighbor.PrimaryInfluencingFaction != -1 )
                                influencerOrNull = World_AIW2.Instance.GetFactionByIndex( neighbor.PrimaryInfluencingFaction );
                            linkedNeighborDebugLevel = 330;
                            Faction controllingFaction = neighbor.GetControllingFaction();

                            linkedNeighborDebugLevel = 400;
                            if ( AttachedFaction.GetIsNeutralTowards( controllingFaction ) )
                            {
                                linkedNeighborDebugLevel = 410;
                                if ( !(controllingFaction.Type == FactionType.NaturalObject && Options.isReconquestWave) &&
                                     !(hostileTurretStrength > 300 && AttachedFaction.InCivilWarMode) )
                                {
                                    linkedNeighborDebugLevel = 420;
                                    //if there are static defenses here left over from a previous assault, this is still a valid wave target
                                    //for civil war mode
                                    //likewise if this is a reconquest wave and the planet is neutral, it is a valid  target
                                    if ( influencerOrNull == null || !AttachedFaction.GetIsHostileTowards( influencerOrNull ) )
                                    {
                                        if ( tracing ) tracingBuffer.Add( "Exit C1 (neutral toward owner and no or non-hostile influencer)\n" );
                                        continue;
                                    }

                                    linkedNeighborDebugLevel = 430;
                                    if ( influencerOrNull != null && !influencerOrNull.SpecialFactionData.AICanSendWavesAgainstThis )
                                    {
                                        if ( tracing ) tracingBuffer.Add( "Exit C2 (neutral toward owner and influencer is not a valid wave target)\n" );
                                        continue;
                                    }
                                }
                            }

                            linkedNeighborDebugLevel = 500;
                            if ( !controllingFaction.SpecialFactionData.AICanSendWavesAgainstThis )
                            {
                                if ( tracing ) tracingBuffer.Add( "Exit C3 (controlling faction is not a valid target for ai waves)\n" );
                                continue;
                            }

                            linkedNeighborDebugLevel = 600;
                            if ( Options.requiredWaveToDefenseRatio != FInt.Zero )
                            {
                                linkedNeighborDebugLevel = 610;
                                FInt hostileStrength = (FInt)myFaction[FactionStance.Hostile].TotalStrength;
                                linkedNeighborDebugLevel = 620;
                                if ( hostileStrength > 0 )
                                {
                                    linkedNeighborDebugLevel = 630;
                                    int waveStrength = WaveDisplay.CalculateStrengthOfWaveCompsition( comp.Composition, AttachedFaction );
                                    linkedNeighborDebugLevel = 640;
                                    FInt computedRatio = waveStrength / hostileStrength;
                                    linkedNeighborDebugLevel = 650;

                                    if ( tracing ) tracingBuffer.Add( "hostile strength " + hostileStrength + " wave strength " + waveStrength + " ratio " + computedRatio + " required ratio " + Options.requiredWaveToDefenseRatio );

                                    linkedNeighborDebugLevel = 660;
                                    if ( Options.requiredWaveToDefenseRatio > computedRatio )
                                    {
                                        if ( tracing ) tracingBuffer.Add( "Exit D\n" );
                                        continue;
                                    }
                                }
                            }

                            linkedNeighborDebugLevel = 700;

                            if ( Options.isReconquestWave &&
                                 (neighbor.LastReconquestAttempt > 0 &&
                                  neighbor.LastReconquestAttempt + ExternalConstants.Instance.ReconquestAttemptInterval > World_AIW2.Instance.GameSecond) )
                            {
                                if ( tracing ) tracingBuffer.Add( "Exit E\n" );
                                continue; //don't try to try to recapture the same planet repeatedly (otherwise the humans can bait us in, then recapture over and over
                            }

                            if ( Options.isReconquestWave &&
                                 neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                                 neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                            {
                                //if the Architrave is in a civil war (and thus friendly to the AI temporarily), we're not allowed to reconquer it
                                continue;
                            }

                            if ( Options.isReconquestWave &&
                                 neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.IsImmuneToReconquestWaves )
                            {
                                //Dyson spheres and similar can't be attacked with reconquest waves
                                continue;
                            }

                            linkedNeighborDebugLevel = 710;
                            KeyValuePair<PlanetWaveComposition, Planet> pair = new KeyValuePair<PlanetWaveComposition, Planet>( comp, neighbor );

                            linkedNeighborDebugLevel = 720;
                            basicSpawnPoints.Add( pair );

                            if ( tracing ) tracingBuffer.Add( "\n\tChooseWaveTarget: adding possibleSpawnPoint to Possible list: " + comp.FromPlanet.Name + " ==> " + pair.Value.Name + "\n" );
                            //                    ArcenDebugging.ArcenDebugLogSingleLine("Adding " + pair.Key.Name + " --> " + pair.Value.Name + " to possibleList", Verbosity.DoNotShow );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Exception in linked planet examination A, linkedNeighborDebugLevel: " + linkedNeighborDebugLevel + " Full Error: " + e, Verbosity.ShowAsError );
                        }
                    }
                }

                debugStage = 58000;

                for ( int i = 0; i < preferredPossibleLaunchPlanets.Count; i++ )
                {
                    PlanetWaveComposition comp = preferredPossibleLaunchPlanets[i];
                    debugStage = 59000;
                    if ( comp.FromPlanet == null )
                        throw new Exception( "Null FromPlanet on PlanetWaveComposition!" );
                    
                    debugStage = 59100;
                    foreach ( Planet neighbor in comp.FromPlanet.LinkedNeighbors( false ) )
                    {
                        int linkedNeighborDebugLevel = 1;
                        try
                        {
                            if ( neighbor == null )
                                continue;

                            linkedNeighborDebugLevel = 100;
                            if ( Options.targetFaction != null )
                            {
                                linkedNeighborDebugLevel = 110;
                                if ( neighbor.GetControllingFaction() != Options.targetFaction &&
                                      neighbor.GetFactionWithSpecialInfluenceHere() != Options.targetFaction )
                                    continue;
                            }
                            else
                            if ( Options.requiredWaveToDefenseRatio != FInt.Zero )
                            {
                                linkedNeighborDebugLevel = 200;

                                //this is mostly for reconquest waves, which can target unowned planets
                                if ( AttachedFaction.GetIsFriendlyTowards( neighbor.GetControllingFaction() ) )
                                    continue;
                                else
                                {
                                    if ( AttachedFaction.GetIsNeutralTowards( neighbor.GetControllingFaction() ) && !Options.isReconquestWave )
                                        continue;
                                }

                                linkedNeighborDebugLevel = 210;
                                PlanetFaction myFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );

                                linkedNeighborDebugLevel = 220;
                                FInt hostileStrength = (FInt)myFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                                linkedNeighborDebugLevel = 230;
                                if ( hostileStrength > 0 )
                                {
                                    linkedNeighborDebugLevel = 240;
                                    int waveStrength = WaveDisplay.CalculateStrengthOfWaveCompsition( comp.Composition, AttachedFaction );
                                    linkedNeighborDebugLevel = 250;
                                    FInt computedRatio = waveStrength / hostileStrength;

                                    linkedNeighborDebugLevel = 260;
                                    if ( hostileStrength > 0 && Options.requiredWaveToDefenseRatio > computedRatio )
                                        continue;
                                }
                            }
                            else
                            {
                                linkedNeighborDebugLevel = 300;

                                //don't target friendly planets
                                if ( AttachedFaction.GetIsFriendlyTowards( neighbor.GetControllingFaction() ) )
                                    continue;

                                linkedNeighborDebugLevel = 310;
                                if ( !(AttachedFaction.GetIsHostileTowards( neighbor.GetControllingFaction() ) &&
                                       AttachedFaction.GetIsHostileTowards( neighbor.GetFactionWithSpecialInfluenceHere() )) )
                                    continue;
                            }

                            if ( Options.isReconquestWave &&
                                 (neighbor.LastReconquestAttempt > 0 &&
                                  neighbor.LastReconquestAttempt + ExternalConstants.Instance.ReconquestAttemptInterval > World_AIW2.Instance.GameSecond) )
                            {
                                if ( tracing ) tracingBuffer.Add( "Exit E\n" );
                                continue; //don't try to try to recapture the same planet repeatedly (otherwise the humans can bait us in, then recapture over and over
                            }

                            if ( Options.isReconquestWave &&
                                 neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                                 neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                            {
                                //if the Architrave is in a civil war (and thus friendly to the AI temporarily), we're not allowed to reconquer it
                                continue;
                            }

                            if ( Options.isReconquestWave &&
                                 neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.IsImmuneToReconquestWaves )
                            {
                                //Dyson spheres and similar can't be attacked with reconquest waves
                                continue;
                            }

                            linkedNeighborDebugLevel = 400;
                            KeyValuePair<PlanetWaveComposition, Planet> pair = new KeyValuePair<PlanetWaveComposition, Planet>( comp, neighbor );

                            linkedNeighborDebugLevel = 410;
                            preferredSpawnPoints.Add( pair );

                            if ( tracing ) tracingBuffer.Add( "ChooseWaveTarget: adding possibleSpawnPoint to Preferred list: " + comp.FromPlanet.Name + " ==> " + pair.Value.Name + "\n" );
                            //                  ArcenDebugging.ArcenDebugLogSingleLine("Adding " + pair.Key.Name + " --> " + pair.Value.Name + " to preferredList", Verbosity.DoNotShow );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Exception in linked planet examination B, linkedNeighborDebugLevel: " + linkedNeighborDebugLevel + " Full Error: " + e, Verbosity.ShowAsError );
                        }
                    }
                }

                debugStage = 108000;

                if ( preferredSpawnPoints.Count > 0 && Context.RandomToUse.NextBool() )
                {
                    if ( tracing ) tracingBuffer.Add( "chooseSpawnLoaction: Using preferredSpawnPoints\n" );
                    finalSpawnPoints = preferredSpawnPoints;
                }
                
                debugStage = 108100;

                if ( finalSpawnPoints.Count <= 0 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "ChooseWaveTarget: No valid targets were found are adjacent to an enemy planet. ratio " + Options.requiredWaveToDefenseRatio.ToString() + " isreconquest " + Options.isReconquestWave + "\n" );
                    if ( tracing )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    
                    return new KeyValuePair<PlanetWaveComposition, Planet>(); //no fallbacks at all, now!  We're trying to use too much intelligence for that.  Chris 7/16/19

                    //If we must go for a particular faction and can't, do nothing
                    //if ( Options.targetFaction != null)
                    //    return null;
                    //if( Options.requiredWaveToDefenseRatio != FInt.Zero ) //if we required a sufficiently vulnerable target and none was found, don't use fallbacks
                    //    return null;

                    //possibleSpawnPoints = fallbackSpawnPoints;
                }

                debugStage = 108200;

                if ( finalSpawnPoints.Count == 0 )
                {
                    if ( Options.requiredWaveToDefenseRatio != FInt.Zero && debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "ChooseWaveTarget: No sufficiently weak targets to attack", Verbosity.DoNotShow );
                    else if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "ChooseWaveTarget: No Warp Gates left in the galaxy. I think the AI might be dead", Verbosity.DoNotShow );
                    return new KeyValuePair<PlanetWaveComposition, Planet>();
                }

                debugStage = 108300;

                if ( finalSpawnPoints.Count > 0 )
                {
                    debugStage = 108320;
                    if ( Options.requiredWaveToDefenseRatio != FInt.Zero )
                    {
                        debugStage = 108400;
                        cb_waveSortAttachedFaction = AttachedFaction;
                        finalSpawnPoints.Sort( static delegate ( KeyValuePair<PlanetWaveComposition, Planet> Left, KeyValuePair<PlanetWaveComposition, Planet> Right )
                        {
                            var leftMyFaction = Left.Value.GetStanceDataForFaction( cb_waveSortAttachedFaction );
                            int leftHostileStrength = leftMyFaction[FactionStance.Hostile].TotalStrength;
                            int leftFriendlyStrength = leftMyFaction[FactionStance.Self].TotalStrength + leftMyFaction[FactionStance.Friendly].TotalStrength;

                            var rightMyFaction = Right.Value.GetStanceDataForFaction( cb_waveSortAttachedFaction );
                            int rightHostileStrength = rightMyFaction[FactionStance.Hostile].TotalStrength;
                            int rightFriendlyStrength = rightMyFaction[FactionStance.Self].TotalStrength + rightMyFaction[FactionStance.Friendly].TotalStrength;

                            int LeftResistance = leftHostileStrength - leftFriendlyStrength;
                            int RightResistance = rightHostileStrength - rightFriendlyStrength;
                            return LeftResistance.CompareTo( RightResistance );
                        } );
                        
                        debugStage = 108500;
                        for ( int i = 0; i < finalSpawnPoints.Count; i++ )
                        {
                            KeyValuePair<PlanetWaveComposition, Planet> pair = finalSpawnPoints[i];
                            if ( tracing )
                                tracingBuffer.Add( "ChooseWaveTarget: " + i + ": " + pair.Key.FromPlanet.Name + " --> " + pair.Value.Name );
                        }
                        
                        debugStage = 108600;
                        bool foundTarget = false;
                        for ( int i = 0; i < finalSpawnPoints.Count; i++ )
                        {
                            //allow some randomness in choosing a target
                            if ( Context.RandomToUse.NextBool() )
                                continue;
                            foundTarget = true;
                            spawnLocation = finalSpawnPoints[i];
                            break;
                        }
                        
                        if ( !foundTarget )
                            spawnLocation = finalSpawnPoints[0];
                        
                        debugStage = 108700;
                    }
                    else
                    {
                        debugStage = 108800;
                        spawnLocation = finalSpawnPoints[Context.RandomToUse.Next( 0, finalSpawnPoints.Count )];
                    }
                }

                foreach ( KeyValuePair<PlanetWaveComposition,Planet> kv in basicSpawnPoints )
                {
                    if ( kv.Key != spawnLocation.Key )
                        kv.Key.ReturnToPool(); //prevent a leak
                }

                foreach ( KeyValuePair<PlanetWaveComposition, Planet> kv in preferredSpawnPoints )
                {
                    if ( kv.Key != spawnLocation.Key )
                        kv.Key.ReturnToPool(); //prevent a leak
                }

                debugStage = 108900;

                if ( tracing )
                {
                    tracingBuffer.Add( "ChooseWaveTarget: warp gate planet " + spawnLocation.Key.FromPlanet.Name + " target planet " + spawnLocation.Value.Name ).Add( "\n" );
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "ChooseWaveTarget exception at debugStage " + debugStage + ", Exception: " + e, Verbosity.ShowAsError );

                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( basicSpawnPoints );
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( preferredSpawnPoints );
                TempCollectionHelper.ReleaseTemporaryListOfPlanetComposition( possibleLaunchPlanets );
                TempCollectionHelper.ReleaseTemporaryListOfPlanetComposition( preferredPossibleLaunchPlanets );
            }
            finally
            {
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( basicSpawnPoints );
                TempCollectionHelper.ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( preferredSpawnPoints );
                TempCollectionHelper.ReleaseTemporaryListOfPlanetComposition( possibleLaunchPlanets );
                TempCollectionHelper.ReleaseTemporaryListOfPlanetComposition( preferredPossibleLaunchPlanets );
            }
            return spawnLocation;
        }
        
        #endregion

        /// <summary>
        /// This static variable will persist between savegames, but it is just for debugging and does not matter.
        /// </summary>
        public static bool DebugWaveAndCPASpawns = false;

        #region Helper_ContainsPlanet
        private static bool Helper_ContainsPlanet( List<PlanetWaveComposition> List, Planet planetToFind )
        {
            for ( int i = 0; i < List.Count; i++ )
            {
                if ( List[i].FromPlanet == planetToFind )
                    return true;
            }
            return false;
        }
        #endregion

        #region Helper_GetWaveCompositionWrapper
        private PlanetWaveComposition Helper_GetWaveCompositionWrapper( ArcenHostOnlySimContext Context, int budget, PlannedWaveOptions Options,
            Planet planetWeAreTryingToSpawnFrom, bool tracing, ArcenCharacterBuffer tracingBuffer )
        {
            if ( planetWeAreTryingToSpawnFrom == null )
                return PlanetWaveComposition.GetFromPoolOrCreate(); //this one will be blank

            Planet planetToUseForSpawningTypes = planetWeAreTryingToSpawnFrom;
            planetToUseForSpawningTypes = WavesHelper.Instance.GetPlanetToUseForWavesFromPlanet( planetToUseForSpawningTypes, Options.forceUseAdjacentPlanet, Options.forceUseRandomPlanetThatIsNotMark7, Context );

            int budgetToUse = budget;
            GameEntityTypeData mustIncludeOneOf = null; //for reconquest waves

            if ( Options.isReconquestWave && Options.requiredWaveToDefenseRatio != FInt.Zero)
            {
                //Old logic: We only want to send enough forces to capture a planet. This allows the AI to take back planets more quickly,
                //since it can spread its budget across multiple targets
                int maxAdjacentFriendlyStrength = 0;
                Planet strongestAdjacentPlanet = null;

                foreach ( Planet neighbor in planetWeAreTryingToSpawnFrom.LinkedNeighbors( false ) )
                {
                    int adjacentFriendlyStrength = neighbor.GetStanceDataForFaction( AttachedFaction )[FactionStance.Self].TotalStrength + neighbor.GetStanceDataForFaction( AttachedFaction )[FactionStance.Friendly].TotalStrength;
                    if ( strongestAdjacentPlanet == null || adjacentFriendlyStrength > maxAdjacentFriendlyStrength )
                    {
                        strongestAdjacentPlanet = neighbor;
                        maxAdjacentFriendlyStrength = adjacentFriendlyStrength;
                    }
                }

                if ( strongestAdjacentPlanet == null )
                    return PlanetWaveComposition.GetFromPoolOrCreate(); //this one will be blank

                int hostileStrength = strongestAdjacentPlanet.GetStanceDataForFaction( AttachedFaction )[FactionStance.Hostile].TotalStrength;

                int minimumToAttackMultiplier = 3;
                int maximumToAttackMultiplier = 6;
                mustIncludeOneOf = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIReclaimer" );
                FInt strengthOverUsurperAlone = FInt.FromParts( 1, 400 ); //don't send usurper-only waves; make sure there's more strength
                if ( mustIncludeOneOf == null )
                    throw new Exception( "No AI Reclaimer units are defined, so the AI cannot recapture planets" );
                
                int minimumNeeded = Math.Max( (hostileStrength * minimumToAttackMultiplier) - (maxAdjacentFriendlyStrength), (mustIncludeOneOf.MarkStatsFor( AttachedFaction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership * strengthOverUsurperAlone).IntValue );
                if ( tracing )
                    tracingBuffer.Add( "minimum required strength for reconquest wave: " + minimumNeeded + " and budget " + budget ).Add( "\n" );
                
                if ( minimumNeeded > budget )
                {
                    return PlanetWaveComposition.GetFromPoolOrCreate(); //this one will be blank
                }
                
                int maximumToSpend = minimumNeeded;
                if ( hostileStrength > FInt.Zero )
                    maximumToSpend = (hostileStrength * maximumToAttackMultiplier) - maxAdjacentFriendlyStrength;
                budgetToUse = Math.Min( maximumToSpend, budget );
            }
            
            if ( mustIncludeOneOf == null && //no ship that currently must be included
                 this.SentinelInfo.AIType.SpawnOneOfTagInWaves != string.Empty && //we've requested a ship in waves
                 Options.spawnBonusShipFromAIType ) //we want to send one of this AI Type's bonus ships
            {
                mustIncludeOneOf = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, this.SentinelInfo.AIType.SpawnOneOfTagInWaves );
                if ( mustIncludeOneOf == null )
                    throw new Exception("Could not find entity with tag " + this.SentinelInfo.AIType.SpawnOneOfTagInWaves);
                //ArcenDebugging.LogSingleLine("spawning one of " + mustIncludeOneOf.GetDisplayName() + " options " + Options.spawnBonusShipFromAIType, Verbosity.DoNotShow );

                if ( mustIncludeOneOf.CostForAIToPurchase < budgetToUse )
                    budgetToUse += mustIncludeOneOf.CostForAIToPurchase; //just in case, make this wave stronger to make sure we can afford this and some other goodies too
            }

            //send a variable number of types of units. Done to illustrate the mechanism, not for balance
            int numUnitTypes = Context.RandomToUse.Next( 2, 4 );
            int maxGuardianTypes = numUnitTypes / 3;
            if ( maxGuardianTypes < 1 )
                maxGuardianTypes = 1;

            int budgetSpent;
            PlanetWaveComposition comp = PlanetWaveComposition.GetFromPoolOrCreate();

            this.SentinelInfo.AIType.Implementation.AdjustWaveOptions( AttachedFaction, Context, ref numUnitTypes, ref maxGuardianTypes, Options, planetToUseForSpawningTypes );

            WavesHelper.Instance.GetWaveComposition( comp.Composition, AttachedFaction, Context, budgetToUse,
                out budgetSpent, numUnitTypes, maxGuardianTypes, Options, planetToUseForSpawningTypes, tracing, tracingBuffer, mustIncludeOneOf );

            comp.Initialize( planetWeAreTryingToSpawnFrom, budgetSpent );

            return comp;
        }
        #endregion
    }
}
