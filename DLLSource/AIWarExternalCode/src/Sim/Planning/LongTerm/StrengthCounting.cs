using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{ 
    public sealed class StrengthCounting : ArcenLongTermContinuousPlanningClientOrHostContext, IBetweenMapGenPoolable<StrengthCounting>
    {
        public override int GetSecondsToLiveEachCycle()
        {
            return 10;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 6;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 10f;
        }

        private static ReferenceTracker RefTracker;
        private StrengthCounting()
            : base( "_PSec.StrengthCounting", ArcenSimContextType.LongTermContinuous )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "StrengthCounting" );
            RefTracker.IncrementObjectCount();
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            CleanupAll();
        }

        private static readonly BetweenMapGenPool<StrengthCounting> Pool = BetweenMapGenPool<StrengthCounting>.Create_WillNeverBeGCed( "StrengthCounting", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new StrengthCounting(); } );

        public static StrengthCounting GetFromPoolOrCreate()
        {
            StrengthCounting context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return this.GetIsWorkDone() && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }

        private void CleanupAll()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        protected override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_STRENGTH_COUNTING )
                return;

            RunStrengthCounting();
        }

        private void RunStrengthCounting()
        {
            float startTime = ArcenTime.TimeSinceStartF;

            int outerDebugStage = 0;
            try
            {
                outerDebugStage = 10;

                outerDebugStage = 500;
                bool shareAlliedNPCVision = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ShareAlliedNPCVision" );

                outerDebugStage = 600;

                #region Reinitialize
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    DoQuickFactionLogic( World_AIW2.Instance.Factions[i] );
                #endregion

                outerDebugStage = 700;

                //ArcenDebugging.ArcenDebugLogSingleLine( "StrengthCounting" + ObjectID + ": " + this.CurrentStage + ": TID" + Thread.CurrentThread.ManagedThreadId +
                //    " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );

                #region Stage1_ResetFactionInfo
                {
                    outerDebugStage = 710;

                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction pFaction = planet.Factions[i];
                            for ( FactionStance stance = 0; stance < FactionStance.Length; stance++ )
                            {
                                StrengthData_PlanetFaction_Stance data = pFaction.Construction_DataByStance[stance];
                                data.ResetForNewCounting( stance != FactionStance.Self );
                            }
                        }
                    }
                }
                #endregion

                outerDebugStage = 800;

                #region Stage2_Counting
                {
                    outerDebugStage = 810;

                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction pFaction = planet.Factions[i];
                            Faction fac = pFaction.Faction;
                            if ( fac == null )
                                continue;
                            StrengthData_PlanetFaction_Stance stanceData = pFaction.Construction_DataByStance[FactionStance.Self];
                            foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                            {
                                DoCombatStep_StrengthCounting_NoCrossPlanet( planet, squad, pFaction, fac, stanceData, shareAlliedNPCVision );
                            }
                        }
                    }
                }
                #endregion

                outerDebugStage = 900;

                #region Stage3_CountingAddendums
                {
                    outerDebugStage = 910;

                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction pFaction = planet.Factions[i];
                            StrengthData_PlanetFaction_Stance stanceData = pFaction.Construction_DataByStance[FactionStance.Self];
                            if ( stanceData.TotalStrength > 0 )
                                stanceData.SecondsSinceHadAnyStrength = 0;
                            else
                                stanceData.SecondsSinceHadAnyStrength++;

                            foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                            {
                                DoCombatStep_StrengthCounting_CrossPlanet( squad, pFaction );
                            }
                        }
                    }
                }
                #endregion

                outerDebugStage = 1000;

                #region Stage4_VisionCalculations
                {
                    outerDebugStage = 1010;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        bool hasShipsGrantingVision = TimeWasRecent( planet.StrengthCountingOnly_Working_LastGameSecondHadNonCrippledFullyFunctionalPlayerShipsOnPlanet );
                        if ( !hasShipsGrantingVision && shareAlliedNPCVision )
                            hasShipsGrantingVision = TimeWasRecent( planet.StrengthCountingOnly_Working_LastGameSecondHadPlayerNonVassalAllyShipsOnPlanet ); //if we have no ships, do we have a non-vassal ally with ships?
                        if ( !hasShipsGrantingVision )
                            hasShipsGrantingVision = TimeWasRecent( planet.StrengthCountingOnly_Working_LastGameSecondHadPlayerVassalShipsOnPlanet ); //if we have no ships, do we have a vassal with ships?

                        if ( hasShipsGrantingVision && planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        {
                            planet.SetIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                        }

                        if ( planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched && !hasShipsGrantingVision && !planet.GetDoHumansHaveVision_CoreOnlyForIntelLevelSetting() )
                            planet.SetIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                        if ( (planet.IntelLevel == PlanetIntelLevel.ExploredByNaturalMeans ||
                            planet.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking) && hasShipsGrantingVision )
                            planet.GrantIntel( PlanetIntelLevel.CurrentlyWatched );

                        if ( planet.IntelLevel > PlanetIntelLevel.CurrentlyWatched ||
                            (planet.IntelLevel == PlanetIntelLevel.CurrentlyWatched && hasShipsGrantingVision) )
                            planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                    }
                }
                #endregion

                outerDebugStage = 1100;

                #region Stage5_IncomingWaves
                {
                    outerDebugStage = 1110;
                    int debugStage = 0;
                    try
                    {
                        debugStage = 100;
                        
                        //For each AI faction, check for PlannedWaves and update the IncomingStrength when a wave is 20 seconds out
                        //This will let the AI better coordinate between the Threat Fleet and waves
                        for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                        {
                            debugStage = 200;
                            Faction faction = World_AIW2.Instance.AIFactions[j];
                            
                            debugStage = 300;
                            ProtectedList<PlannedWave> QueuedWaves = faction.GetAISentinelsCoreData().WaveList;
                            
                            debugStage = 400;
                            if ( QueuedWaves == null )
                                continue;
                            
                            debugStage = 500;
                            for ( int k = 0; k < QueuedWaves.Count; k++ )
                            {
                                debugStage = 600;
                                PlannedWave wave = QueuedWaves[k];
                                
                                debugStage = 700;
                                Planet planet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                                if ( planet == null )
                                    continue;
                                
                                debugStage = 800;
                                int timeTillLaunch = wave.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond;
                                
                                debugStage = 900;
                                if ( timeTillLaunch < 20 )
                                {
                                    debugStage = 1000;
                                    //get the planetFaction, then update
                                    int strength = wave.CalculateStrengthOfWave( faction );
                                    debugStage = 1100;
                                    PlanetFaction pFaction = planet.Factions[faction.FactionIndex];
                                    debugStage = 1150;
                                    StrengthData_PlanetFaction_Stance myData = pFaction.Construction_DataByStance[FactionStance.Self];
                                    debugStage = 1200;
                                    //add for myself
                                    myData.IncomingStrength += strength;
                                }
                            }
                        }
                    } 
                    catch ( ArcenPleaseStopThisThreadException e )
                    { 
                        //this is normal
                        throw e; 
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "RunStrengthCounting exception in wave IncomingStrength calculations, debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                    }
                }
                #endregion

                outerDebugStage = 1200;

                #region Stage6_Aggregations
                {
                    outerDebugStage = 1210;

                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction pFaction = planet.Factions[i];
                            DoPlanetFactionStepLogic_Aggregations( planet, pFaction );
                        }
                    }
                }
                #endregion

                outerDebugStage = 1300;

                #region Stage7_Combinations
                {
                    outerDebugStage = 1310;

                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction faction = planet.Factions[i];

                            for ( int j = 0; j < planet.Factions.Count; j++ )
                            {
                                PlanetFaction otherFaction = planet.Factions[j];
                                FactionStance stance = faction.GetStanceTowards( otherFaction );
                                if ( stance == FactionStance.Self )
                                    continue;
                                StrengthData_PlanetFaction_Stance theirData = otherFaction.Construction_DataByStance[FactionStance.Self];
                                StrengthData_PlanetFaction_Stance myData = faction.Construction_DataByStance[stance];
                                theirData.AddInto( myData );
                            }
                        }
                    }
                }
                #endregion

                outerDebugStage = 1400;

                #region Stage8_CopyOvers
                {
                    outerDebugStage = 1410;

                    //overwrite all the data for all the factions, on every planet!
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int i = 0; i < planet.Factions.Count; i++ )
                        {
                            PlanetFaction pFaction = planet.Factions[i];
                            StrengthData_PlanetFaction_Stance stanceData;
                            for ( FactionStance stance = 0; stance < FactionStance.Length; stance++ )
                            {
                                stanceData = pFaction.Construction_DataByStance[stance];
                                //because this is just a direct set, this is okay
                                stanceData.Overwrite( pFaction.DataByStance[stance] );
                            }
                        }
                    }
                }
                #endregion

                outerDebugStage = 1500;

                #region Stage9_AllDone
                {
                    outerDebugStage = 1510;

                    //if we're going to debug dump stance data, then this is when we do it
                    //otherwise don't even think about it
                    //most of Step10 actually happens elsewhere
                    if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.StanceData ) )
                    {
                        outerDebugStage = 1520;
                        foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                        {
                            if ( planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                            {
                                DebugDumpStanceData( planet );
                            }
                        }
                    }

                    outerDebugStage = 1540;
                }
                #endregion

            } catch ( ArcenPleaseStopThisThreadException )
            { }//this is normal
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in StrengthCounting.RunStrengthCounting at outerDebugStage " + outerDebugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            } finally
            {
                //hey, we finished; we do that every time, actually
                {
                    float finishedTime = ArcenTime.TimeSinceStartF;

                    //try to run this every 1 seconds, or with 0.5 second gaps, whichever is slower
                    float timeToWaitBeforeNextStart = 1f - (finishedTime - startTime);
                    if ( timeToWaitBeforeNextStart < 0.5f )
                        timeToWaitBeforeNextStart = 0.5f;
                    this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
                }
            }
        }

        private bool TimeWasRecent( int LastGameSecond )
        {
            if ( LastGameSecond <= 0 )
                return false; //has never been set
            return World_AIW2.Instance.GameSecond - LastGameSecond < 2; //if is within the last 2 seconds
        }

        private void DoQuickFactionLogic( Faction faction )
        {
            if ( faction == null )
                return;
            switch ( faction.Type )
            {
                case FactionType.AI:
                    {
                        AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        if ( factionExternal != null )
                        {
                            faction.CounterattackMinStrengthFromExternal = factionExternal.AIDifficulty.CounterattackMinStrength;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// called after all factions on this planet have had phase 1 called
        /// </summary>
        private void DoPlanetFactionStepLogic_Aggregations( Planet planet, PlanetFaction faction )
        {
            int debugStep = 1;
            try
            {
                debugStep = 100;

                int unengagedMobileStrength = faction.Construction_DataByStance[FactionStance.Self].NonGuardMobileStrength;
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    if ( unengagedMobileStrength <= FInt.Zero )
                        return;
                    debugStep = 110;
                    PlanetFaction otherFaction = planet.Factions[i];
                    debugStep = 120;
                    FactionStance stance = faction.GetStanceTowards( otherFaction );
                    debugStep = 130;
                    if ( stance != FactionStance.Hostile )
                        continue;
                    debugStep = 140;
                    unengagedMobileStrength -= otherFaction.Construction_DataByStance[FactionStance.Self].TotalStrength;
                }
                debugStep = 400;
                if ( unengagedMobileStrength <= FInt.Zero )
                    return;
                debugStep = 600;

                int NumberOfHops = StrengthData_PlanetFaction_Stance.MAX_HOPS_FOR_UNENGAGED_MOBILE_STRENGTH_TRACKING;
                debugStep = 800;
                for ( int i = 0; i < planet.PlanetIndicesSortedByHopCount.Count; i++ )
                {
                    debugStep = 810;
                    Int16 planetIndex = planet.PlanetIndicesSortedByHopCount[i];
                    debugStep = 820;
                    int hopCount = planet.NumberOfHopsToOtherPlanets[planetIndex];
                    if ( NumberOfHops >= 0 && hopCount > NumberOfHops )
                        break;
                    debugStep = 830;
                    Planet otherPlanet = World_AIW2.Instance.GetPlanetByIndex( planetIndex );
                    debugStep = 840;
                    if ( otherPlanet == null )
                        continue;
                    debugStep = 850;
                    PlanetFaction otherPlanetFaction = otherPlanet.GetPlanetFactionForFaction( faction.Faction );
                    if ( otherPlanetFaction == null )
                    {
                        debugStep = 8510;
                        ArcenDebugging.ArcenDebugLog( "StrengthCounting.DoPlanetFactionStepLogic_Phase2: missing planetfaction for planet " +
                            otherPlanet.Name + ", faction " + faction?.Faction.GetDisplayName() + " (" + faction?.Faction.FactionIndex + ")", Verbosity.ShowAsError );
                        continue;
                    }
                    debugStep = 851;
                    StrengthData_PlanetFaction_Stance data = otherPlanetFaction.Construction_DataByStance[FactionStance.Self];
                    debugStep = 860;
                    if ( hopCount < 0 || hopCount >= data.UnengagedMobileStrengthByHopCount.Length )
                        continue;
                    data.UnengagedMobileStrengthByHopCount[hopCount] += unengagedMobileStrength;
                }
            } catch ( ArcenPleaseStopThisThreadException e )
            { throw e; }//this is normal
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in StrengthCounting.DoPlanetFactionStepLogic_Aggregations at debugStep " + debugStep + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void DoCombatStep_StrengthCounting_NoCrossPlanet( 
            Planet planet, 
            GameEntity_Squad entity, 
            PlanetFaction pFaction, 
            Faction faction, 
            StrengthData_PlanetFaction_Stance StanceData,
            bool ShareAlliedNPCVision )
        {
            if ( entity.SecondsSpentAsRemains >= 0 )
                return;
            
            int debugCode = 0;
            try
            {
                bool isReinforcementPoint = false;
                int strengthBase = entity.GetStrengthOfSelfAndContents();
                int aiToPurchaseCost = entity.TypeData.CostForAIToPurchase + entity.GetCostForAIToPurchaseOfContentsIfAny();
                int strengthContentsOnly = 0;
                int aiToPurchaseCostContentsOnly = 0;
                debugCode = 10;
                if ( entity.TypeData.IsReinforcementLocation )
                {
                    debugCode = 20;
                    isReinforcementPoint = true;
                    strengthBase = entity.GetStrengthOfStack();
                    strengthContentsOnly = entity.GetStrengthOfContentsIfAny();
                    aiToPurchaseCost = entity.TypeData.CostForAIToPurchase;
                    aiToPurchaseCostContentsOnly = entity.GetCostForAIToPurchaseOfContentsIfAny();
                }

                #region AI-only data
                debugCode = 30;
                if ( faction.Type == FactionType.AI )
                {
                    debugCode = 40;
                    switch ( entity.TypeData.SpecialType )
                    {
                        case SpecialEntityType.GuardPost:
                        case SpecialEntityType.DireGuardPost:
                            StanceData.NumGuardPosts++;
                            break;
                    }
                }

                #endregion
                debugCode = 100;
                
                // We only care about the strength of things that can fight - don't count things not fully built, not fully claimed, not crippled
                if ( entity.SelfBuildingMetalRemaining <= 0 &&
                     entity.GetMetalToClaimRemaining() <= 0 &&
                     !entity.GetIsCrippled() &&
                     !entity.GetIsRemains() &&
                     !entity.GetIsNonFunctional() )
                {
                    debugCode = 200;
                    if ( isReinforcementPoint )
                    {
                        StanceData.TotalStrengthIncludingNonMilitary += (strengthBase + strengthContentsOnly);
                        StanceData.StrengthInReinforcementPoints += strengthContentsOnly;
                    } else
                        StanceData.TotalStrengthIncludingNonMilitary += strengthBase;
                    StanceData.TotalUnits += 1 + entity.ExtraStackedSquadsInThis;

                    bool shouldBeVisible = entity.GetShouldBeVisibleBasedOnPlanetIntel();

                    GameEntity_Squad center = null;
                    if ( entity.TypeData.OrbitsFlagshipAtRange >= 0 )
                        center = entity.FleetMembership?.Fleet?.Centerpiece.GetSquad();
                    else if ( entity.TypeData.OrbitsParentAtRange >= 0 || entity.TypeData.OrbitsFlagshipAtRange >= 0 )
                        center = entity.ParentGameEntity.GetSquad();
                    bool isAttacker = (entity.GetEffectiveOrders().Behavior == EntityBehaviorType.Attacker_Full) ||
                        faction.SpecialFactionData.IsAlwaysADirectAttackAgainstPlayers || (center != null && center.GetEffectiveOrders().Behavior == EntityBehaviorType.Attacker_Full);
                    bool countAsMobile = entity.IsMobileOrCountsAsMobileDueToOrbitingSomethingMobile();

                    //if a combatant or transport or contains units, do this stuff
                    if ( entity.TypeData.IsCombatant || entity.AIReinforcementPointContents != null || entity.TypeData.IsTransport )
                    {
                        debugCode = 300;
                        int strengthEffective = entity.TypeData.IsCombatant ? strengthBase : 0;

                        debugCode = 310;
                        if ( isReinforcementPoint )
                        {
                            StanceData.TotalStrength += (strengthEffective + strengthContentsOnly);
                            if ( shouldBeVisible )
                                StanceData.TotalStrengthVisible += (strengthEffective + strengthContentsOnly);
                        }
                        else
                        {
                            StanceData.TotalStrength += strengthEffective;
                            if ( shouldBeVisible )
                                StanceData.TotalStrengthVisible += (strengthEffective);
                        }
                        
                        debugCode = 315;
                        if ( entity.TypeData.TargetTypeForPlayer != PlayerTargetType.AutotargetAlways ) 
                        {
                            StanceData.NonTargetableStrength += strengthEffective;
                        }
                        
                        debugCode = 320;
                        if ( faction.Type == FactionType.Player )
                        {
                            debugCode = 330;
                            //for this one, also include contents
                            StanceData.TotalPlayerStrength += strengthEffective + strengthContentsOnly;

                            //We want to know here whether the player has deployed ships from their flagship, and not count ships being transported
                            //This is for checking player deepstrikes, so only trigger on player
                            if ( entity.TypeData.IsMobileFleetFlagship )
                                StanceData.TotalPlayerStrengthNotInTransports += entity.GetStrengthPerSquad();
                            else
                                StanceData.TotalPlayerStrengthNotInTransports += strengthEffective;
                        }
                        
                        debugCode = 340;
                        if ( faction.SpecialFactionData.IsConsideredWarden )
                        {
                            //this is for the UI only
                            StanceData.TotalWardenStrength += strengthEffective;
                            if ( shouldBeVisible )
                                StanceData.TotalWardenStrengthVisible += strengthEffective;
                        }
                        
                        debugCode = 350;
                        if ( !countAsMobile )
                        {
                            debugCode = 352;
                            StanceData.TurretStrength += strengthEffective;
                            if ( isReinforcementPoint )
                                StanceData.MobileStrength += strengthContentsOnly;
                        } 
                        else
                        {
                            debugCode = 353;
                            StanceData.MobileStrength += strengthEffective;
                            if ( faction.Type == FactionType.Player || isAttacker )
                                StanceData.NonGuardMobileStrength += strengthEffective;
                        }
                        
                        debugCode = 360;
                        if ( entity.GetMightPossiblyBeCloaked() )
                            StanceData.CloakedStrength += strengthEffective;

                        debugCode = 365;
                        //Composition histograms: bucket this combatant's strength by the same target stats the
                        //damage-modifier counter system keys off of (read via the same getters DamageModifier uses,
                        //so the bins line up with real counters). Reinforcement-point contents are intentionally not
                        //bucketed here -- they're a bag of mixed types we can't classify by the container's stats.
                        if ( strengthEffective > 0 )
                        {
                            FInt mass = entity.TypeData.Mass_tX;
                            if ( mass <= CompositionBins.MassEdge_LightMax )
                                StanceData.MassStrength_Light += strengthEffective;
                            else if ( mass >= CompositionBins.MassEdge_HeavyMin )
                                StanceData.MassStrength_Heavy += strengthEffective;
                            else
                                StanceData.MassStrength_Medium += strengthEffective;

                            int armor = entity.TypeData.Armor_mm;
                            if ( armor <= CompositionBins.ArmorEdge_LowMax )
                                StanceData.ArmorStrength_Low += strengthEffective;
                            else if ( armor >= CompositionBins.ArmorEdge_HighMin )
                                StanceData.ArmorStrength_High += strengthEffective;
                            else
                                StanceData.ArmorStrength_Mid += strengthEffective;

                            FInt albedo = entity.DataForMark.Albedo;
                            if ( albedo <= CompositionBins.AlbedoEdge_DarkMax )
                                StanceData.AlbedoStrength_Dark += strengthEffective;
                            else if ( albedo >= CompositionBins.AlbedoEdge_BrightMin )
                                StanceData.AlbedoStrength_Bright += strengthEffective;
                            else
                                StanceData.AlbedoStrength_Mid += strengthEffective;

                            int energy = entity.TypeData.EnergyUsage;
                            if ( energy <= CompositionBins.EnergyEdge_LowMax )
                                StanceData.EnergyStrength_Low += strengthEffective;
                            else if ( energy >= CompositionBins.EnergyEdge_HighMin )
                                StanceData.EnergyStrength_High += strengthEffective;
                            else
                                StanceData.EnergyStrength_Mid += strengthEffective;
                        }
                        
                        bool handledHunter = false;
                        bool isAfterNonHumanTeam = false;
                        if ( faction.Type != FactionType.Player )
                        {
                            debugCode = 400;
                            bool isThreat = isAttacker && entity.CalculateIsThreatInGeneral();
                            if ( isThreat )
                            {
                                debugCode = 500;
                                StanceData.ThreatStrength += strengthEffective;

                                if ( shouldBeVisible )
                                    StanceData.ThreatStrengthVisible += strengthEffective;
                                
                                bool threatFromAI = false;
                                if ( faction.Type == FactionType.AI ||
                                    faction.SpecialFactionData.AlliedToAIByDefault ||
                                    faction.SpecialFactionData.IsConsideredHunter ||
                                    faction.SpecialFactionData.IsAlwaysADirectAttackAgainstPlayers )
                                {
                                    threatFromAI = true;
                                }
                                
                                debugCode = 600;
                                if ( !entity.ShouldNotBeConsideredAsThreatToHumanTeam && threatFromAI )
                                {
                                    debugCode = 610;
                                    bool threatIsGoingAfterNonHumanFaction = entity.CalculateIsThreatAgainstNotTheHumans();
                                    
                                    if ( !threatIsGoingAfterNonHumanFaction )
                                    {
                                        debugCode = 620;
                                        //go ahead and mark the entity as having become threat now, since this is the most convenient place to know it
                                        if ( entity.BecameThreatfleetAtGameSecond <= 0 )
                                            entity.BecameThreatfleetAtGameSecond = World_AIW2.Instance.GameSecond;
                                        
                                        StanceData.RelativeToHumanTeam_ThreatStrength += strengthEffective;
                                        
                                        if ( shouldBeVisible )
                                        {
                                            StanceData.RelativeToHumanTeam_ThreatStrengthVisible += strengthEffective;
                                        }

                                        debugCode = 630;
                                        if ( faction.SpecialFactionData.IsConsideredHunter )
                                        {
                                            handledHunter = true;
                                            
                                            //this is for the UI only
                                            StanceData.TotalHunterStrength_AgainstHumans += strengthEffective;
                                            if ( shouldBeVisible )
                                                StanceData.TotalHunterStrength_AgainstHumansVisible += strengthEffective;
                                        }
                                    } 
                                    else
                                    {
                                        debugCode = 640;
                                        
                                        isAfterNonHumanTeam = true;
                                        //I guess skip this?
                                        //if ( entity.BecameThreatfleetAtGameSecond <= 0 )
                                        //    entity.BecameThreatfleetAtGameSecond = World_AIW2.Instance.GameSecond;
                                        StanceData.RelativeToOtherFaction_ThreatStrength += strengthEffective;
                                        
                                        if ( shouldBeVisible )
                                        {
                                            StanceData.RelativeToOtherFaction_ThreatStrengthVisible += strengthEffective;
                                        }

                                        if ( faction.SpecialFactionData.IsConsideredHunter )
                                        {
                                            handledHunter = true;
                                            
                                            //this is for the UI only
                                            StanceData.TotalHunterStrength_AgainstOtherFactions += strengthEffective;
                                            if ( shouldBeVisible )
                                                StanceData.TotalHunterStrength_AgainstOtherFactionsVisible += strengthEffective;
                                        }
                                        
                                        debugCode = 650;
                                    }
                                }
                            } 
                            else
                            {
                                if ( isReinforcementPoint )
                                {
                                    StanceData.GuardStrength += strengthContentsOnly;
                                } 
                                else
                                {
                                    StanceData.GuardStrength += strengthEffective;
                                }
                            }
                        }
                        
                        debugCode = 700;
                        if ( !faction.SpecialFactionData.AIDoesNotGenerateThreatAgainstThisFaction )
                            StanceData.TotalThreatProvokingStrength += strengthEffective;

                        if ( !handledHunter )
                        {
                            debugCode = 710;
                            if ( faction.SpecialFactionData.IsConsideredHunter )
                            {
                                //this is for the UI only
                                StanceData.TotalHunterStrength_AgainstHumans += strengthEffective;
                                if ( shouldBeVisible )
                                    StanceData.TotalHunterStrength_AgainstHumansVisible += strengthEffective;
                            }
                        }

                        entity.IsAfterANonHumanTeam_NonSim = isAfterNonHumanTeam;
                    } 
                    else //!IsCombatant
                    {
                        debugCode = 800;
                        if ( !countAsMobile )
                        {
                            debugCode = 810;
                            if ( entity.TypeData.ProjectsForcefield )
                            {
                                StanceData.ForcefieldStrength += strengthBase;
                            } 
                            else if ( entity.TypeData.IsNonTurretDefense )
                            {
                                StanceData.NonTurretDefenseStrength += strengthBase;
                            }
                        }
                        
                        if ( isReinforcementPoint )
                        {
                            StanceData.GuardStrength += strengthContentsOnly;
                        }
                    }
                }

                debugCode = 1000;
                
                if ( entity.TypeData.IsKingUnit )
                    StanceData.HasKingUnitPresent = true;
                
                debugCode = 1010;
                
                //CalculatedAttackerDamageTotal_InProgress gets calculated in there
                entity.Orders.CalculateStrengthCountingData_ClientAndHost();

                debugCode = 1020;
                
                if ( faction.Type == FactionType.Player && 
                     !entity.GetIsCrippled() && 
                     !entity.GetIsNonFunctional() )
                {
                    debugCode = 1030;
                    if ( planet.StrengthCountingOnly_Working_LastGameSecondHadNonCrippledFullyFunctionalPlayerShipsOnPlanet < World_AIW2.Instance.GameSecond )
                        Interlocked.Exchange( ref planet.StrengthCountingOnly_Working_LastGameSecondHadNonCrippledFullyFunctionalPlayerShipsOnPlanet, World_AIW2.Instance.GameSecond );
                }
                
                debugCode = 1040;
                
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false ) )
                {
                    Interlocked.Exchange( ref planet.StrengthCountingOnly_Working_LastGameSecondHadPlayerVassalShipsOnPlanet, World_AIW2.Instance.GameSecond );
                    debugCode = 1041;
                } 
                else if ( ShareAlliedNPCVision &&
                          faction.Type == FactionType.SpecialFaction &&
                          !entity.TypeData.DoesNotGiveAlliesVision &&
                          planet.StrengthCountingOnly_Working_LastGameSecondHadPlayerNonVassalAllyShipsOnPlanet < World_AIW2.Instance.GameSecond )
                {
                    if ( faction.BaseInfo.Allegiance == "Friendly To Players" || 
                         faction.SpecialFactionData.AlwaysFriendlyToPlayers )
                    {
                        Interlocked.Exchange( ref planet.StrengthCountingOnly_Working_LastGameSecondHadPlayerNonVassalAllyShipsOnPlanet, World_AIW2.Instance.GameSecond );
                    }
                    debugCode = 1060;
                }
            } 
            catch ( ArcenPleaseStopThisThreadException e )
            {
                //this is normal
                throw e; 
            }
            catch ( Exception e )
            {
                LOG.Err( "exception in {0} at debugCode={1}\n{2}", this.TypeNameAndMethod(), debugCode, e);
            }
        }

        private void DoCombatStep_StrengthCounting_CrossPlanet( GameEntity_Squad entity, PlanetFaction pFaction )
        {
            if ( entity.SecondsSpentAsRemains >= 0 )
                return;
            int debugCode = 0;
            try
            {
                // We only care about the strength of things that can fight - don't count things not fully built, not fully claimed, not crippled
                if ( !entity.HasDoneOnDeathSinceLastClaimed && !entity.HasNotYetBeenFullyClaimed && !entity.GetIsCrippled() &&
                entity.SelfBuildingMetalRemaining <= 0 && !entity.GetIsNonFunctional() )
                {
                    #region Logic that was once only for the LRP threads
                    if ( entity.TypeData.IsMobileCombatant )
                    {
                        debugCode = 8801000;
                        Planet waitingAgainstPlanet = null;
                        if ( entity.WaitingAgainstPlanetIndex >= 0 )
                            waitingAgainstPlanet = World_AIW2.Instance.GetPlanetByIndex( false, entity.WaitingAgainstPlanetIndex );
                        debugCode = 8802000;
                        if ( waitingAgainstPlanet != null )
                        {
                            debugCode = 8802100; //waiting strength counts against OTHER planets
                            PlanetFaction otherPFaction = waitingAgainstPlanet.Factions[pFaction.FactionIndex];
                            if ( otherPFaction != null )
                            {
                                debugCode = 8802200;
                                StrengthData_PlanetFaction_Stance otherPlanetStanceData = otherPFaction.Construction_DataByStance[FactionStance.Self];
                                debugCode = 8802400;
                                otherPlanetStanceData.WaitingStrength += entity.GetStrengthOfSelfAndContents();
                            }
                            debugCode = 8802400;
                        } else //not waiting
                        {
                            debugCode = 8804100;
                            Int16 nextHopPlanetIndex = entity.CalculateNextHopPlanetIndex_Safe();
                            debugCode = 8804200;
                            Planet nextHopPlanet = null;
                            if ( nextHopPlanetIndex >= 0 )
                            {
                                debugCode = 8804300;
                                nextHopPlanet = World_AIW2.Instance.GetPlanetByIndex( false, nextHopPlanetIndex );
                            }
                            debugCode = 8804400;
                            if ( nextHopPlanet != null )
                            {
                                debugCode = 8804500; //incoming strength counts against OTHER planets
                                PlanetFaction otherPFaction = nextHopPlanet.Factions[pFaction.FactionIndex];
                                if ( otherPFaction != null )
                                {
                                    debugCode = 8804600;
                                    StrengthData_PlanetFaction_Stance otherPlanetStanceData = otherPFaction.Construction_DataByStance[FactionStance.Self];
                                    debugCode = 8804700;
                                    otherPlanetStanceData.IncomingStrength += entity.GetStrengthOfSelfAndContents();
                                }
                                debugCode = 8804800;
                            }
                        }
                    }
                    #endregion
                }
            } catch ( ArcenPleaseStopThisThreadException e )
            { throw e; }//this is normal
            catch ( Exception e )
            {
                LOG.Err( "exception in {0} at debugCode={1}\n{2}", this.TypeNameAndMethod(), debugCode, e);
            }
        }

        private void DebugDumpStanceData( Planet planet )
        {
            for ( int i = 0; i < planet.Factions.Count; i++ )
            {
                PlanetFaction faction = planet.Factions[i];
                switch ( faction.Faction.Type )
                {
                    case FactionType.AI:
                    case FactionType.Player:
                        break;
                    default:
                        continue;
                }
                faction.DebugDumpStanceData( "\n================\n" + planet.Name + " Stance Data \n" );
            }
        }
    }

    public static class GameEntity_SquadExtensions
    {
        public static bool CalculateIsThreatInGeneral( this GameEntity_Squad entity )
        {
            if ( entity == null )
                return false;
            Faction faction = entity.GetFactionOrNull_Safe();
            if ( faction == null )
                return false;
            bool isAttacker = (entity.GetEffectiveOrders().Behavior == EntityBehaviorType.Attacker_Full) ||
                faction.SpecialFactionData.IsAlwaysADirectAttackAgainstPlayers;
            if ( !isAttacker )
                return false;
            if ( faction.Type != FactionType.AI && !faction.SpecialFactionData.AlliedToAIByDefault )
                return false; //only AI ships are threat
            if ( entity.GuardedUnit.GetSquad() != null )
                return false; //this AI unit is explicitly guarding something
            //things that orbit the gravity well are not threat
            if ( entity.TypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                return false;
            //things that orbit a parent are only threat if the parent is threat
            if ( entity.TypeData.OrbitsParentAtRange > 0 )
            {
                GameEntity_Squad parent = entity.ParentGameEntity.GetSquad();
                if ( parent == null )
                    return false; //if null parent, not threat
                if ( !parent.CalculateIsThreatInGeneral() )
                    return false; //if parent is not threat, then neither are we
            }
            if ( entity.TypeData.OrbitsFlagshipAtRange > 0 )
            {
                GameEntity_Squad parent = entity.FleetMembership?.Fleet?.Centerpiece.GetSquad();
                if ( parent == null )
                    return false; //if null parent, not threat
                if ( !parent.CalculateIsThreatInGeneral() )
                    return false; //if parent is not threat, then neither are we
            }
            if ( entity.ShouldNotBeConsideredAsThreatToHumanTeam )
                return false;
            if ( entity.TypeData.ShouldNeverCountAsThreat )
                return false;

            return true; //we are threat!
        }

        public static bool CalculateIsThreatAgainstNotTheHumans( this GameEntity_Squad entity )
        {
            if ( entity.Orders.BehaviorRelatedFactionIndex >= 0 )
            {
                // Only AI Sentinals use BehaviorRelatedFactionIndex
                if ( entity.GetFactionTypeSafe() == FactionType.AI ) {
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( entity.Orders.BehaviorRelatedFactionIndex );
                    if ( faction != null && faction.Type != FactionType.Player &&
                         !faction.SpecialFactionData.AlwaysFriendlyToPlayers)
                        return true;
                }
            }
            if ( entity.FireteamSpecificationOrNull != null && entity.FireteamSpecificationOrNull.IsActive() )
            {
                if ( !String.IsNullOrEmpty( entity.FireteamSpecificationOrNull.AgainstFactionAllegiance ) &&
                        entity.FireteamSpecificationOrNull.AgainstFactionAllegiance != "Friendly To Players" )
                {
                    return true;
                } else if ( entity.FireteamSpecificationOrNull.AgainstFaction != null )
                {
                    if ( entity.FireteamSpecificationOrNull.AgainstFaction.Type != FactionType.Player &&
                         !entity.FireteamSpecificationOrNull.AgainstFaction.SpecialFactionData.AlwaysFriendlyToPlayers)
                        return true;
                }
            }
            return false;
        }

        public static string CalculateFactionIsChasingInsteadOfHumans( this GameEntity_Squad entity )
        {
            if ( entity.Orders.BehaviorRelatedFactionIndex >= 0 )
            {
                // Only AI Sentinals use BehaviorRelatedFactionIndex
                if ( entity.GetFactionTypeSafe() == FactionType.AI ) {
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( entity.Orders.BehaviorRelatedFactionIndex );
                    if ( faction != null && faction.Type != FactionType.Player )
                        return faction.GetDisplayName();
                }
            }
            if ( entity.FireteamSpecificationOrNull != null && entity.FireteamSpecificationOrNull.IsActive() )
            {
                if ( !String.IsNullOrEmpty( entity.FireteamSpecificationOrNull.AgainstFactionAllegiance ) &&
                        entity.FireteamSpecificationOrNull.AgainstFactionAllegiance != "Friendly To Players" )
                {
                    return entity.FireteamSpecificationOrNull.AgainstFactionAllegiance;
                } else if ( entity.FireteamSpecificationOrNull.AgainstFaction != null )
                {
                    if ( entity.FireteamSpecificationOrNull.AgainstFaction.Type != FactionType.Player )
                        return entity.FireteamSpecificationOrNull.AgainstFaction.GetDisplayName();
                }
            }
            return string.Empty;
        }

        public static Int16 CalculateFactionIndexIsChasingInsteadOfHumans( this GameEntity_Squad entity )
        {
            if ( entity.Orders.BehaviorRelatedFactionIndex >= 0 )
            {
                // Only AI Sentinals use BehaviorRelatedFactionIndex
                if ( entity.GetFactionTypeSafe() == FactionType.AI ) {
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( entity.Orders.BehaviorRelatedFactionIndex );
                    if ( faction != null && faction.Type != FactionType.Player )
                        return faction.FactionIndex;
                }
            }
            if ( entity.FireteamSpecificationOrNull != null && entity.FireteamSpecificationOrNull.IsActive() )
            {
                if ( !String.IsNullOrEmpty( entity.FireteamSpecificationOrNull.AgainstFactionAllegiance ) &&
                        entity.FireteamSpecificationOrNull.AgainstFactionAllegiance != "Friendly To Players" )
                {
                    return -2;
                } else if ( entity.FireteamSpecificationOrNull.AgainstFaction != null )
                {
                    if ( entity.FireteamSpecificationOrNull.AgainstFaction.Type != FactionType.Player )
                        return entity.FireteamSpecificationOrNull.AgainstFaction.FactionIndex;
                }
            }
            return -1;
        }
    }
}
