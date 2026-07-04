using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class TractorPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<TractorPlanning>
    {
        private static ReferenceTracker RefTracker;
        private TractorPlanning()
            : base( "_ST.TractorPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "TractorPlanning" );
            RefTracker.IncrementObjectCount();
        }

        public static int Accumulator_TRB = 0;
        public static int Accumulator_TRF = 0;

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "TractorPlanning", 30f );

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_TRB = 0;
            Accumulator_TRF = 0;
        }

        private static readonly BetweenMapGenPool<TractorPlanning> Pool = BetweenMapGenPool<TractorPlanning>.Create_WillNeverBeGCed( "TractorPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TractorPlanning(); } );

        public static TractorPlanning GetFromPoolOrCreate()
        {
            TractorPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return; 
            if ( CentralVars.DEBUG_TURN_OFF_TRACTOR_PLAN )
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;
            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            
            try
            {
                Interlocked.Increment( ref Accumulator_TRB );

                bool drawTractorBeams_Globally = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawTractorBeams );

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    bool drawTractorBeams = drawTractorBeams_Globally;
                    if ( planet.Index != PlayerAccount_AIW2.GetViewingPlanetIndexSafe() )
                        drawTractorBeams = false;

                    //just do for tractor sources
                    foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.TractorSource ) )
                    {
                        entity.CurrentlyStrongestTractorSourceHittingThese.ClearConstructionListForStartingConstruction();
                        entity.OtherEntityPullingMeByMyOwnReverseTractor.ClearConstructionValueForStartingConstruction();
                    }
                    //do for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.CurrentTractorSourcesHittingThis.ClearConstructionListForStartingConstruction();
                    }

                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        PlanetFaction pFaction = planet.Factions[j];
                        if ( pFaction == null || pFaction.Entities.EntitiesOrNull_Squad == null ) //if no squads of our own here, don't even try it!
                            continue;

                        NonClosure_FoundHostiles = false;
                        Unrolled_CheckForHostiles( pFaction );
                        if ( !NonClosure_FoundHostiles )
                            continue;

                        NonClosure_AllPossibleTargetsList.Clear();
                        NonClosure_NeedToFillAllPossibleTargetsList = true;
                        Unrolled_PerTractorSource( pFaction );
                    }

                    //do for all entities
                    foreach ( GameEntity_Squad e in planet.Squads() )
                    {
                        if ( e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull != null )
                            e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull.ClearConstructionListForStartingConstruction();

                        byte currentTractorPullsOnMe = 0;
                        GameEntity_Squad strongestTractorSource = null;
                        int strongestTractorStrength = -1;
                        var tractorsNewlyHittingMe = e.CurrentTractorSourcesHittingThis.GetConstructionList();

                        for ( int i = 0; i < tractorsNewlyHittingMe.Count; i++ )
                        {
                            var tractorSource = World_AIW2.Instance.GetEntityByID_Squad( tractorsNewlyHittingMe[i] );
                            if ( tractorSource == null )
                                continue;

                            if ( e.GetIsProtectedByAnyForcefield() )
                                continue;

                            if ( tractorSource.TypeData.IsReverseTractorSource )
                            {
                                if ( e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull == null )
                                    e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull = DoubleBufferedList<GameEntity_Squad>.Create_WillNeverBeGCed( 40, "Squad-CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull" );

                                e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull.AddToConstructionList( tractorSource );

                                tractorSource.OtherEntityPullingMeByMyOwnReverseTractor.Construction = e;
                                //note: deliberately NOT incrementing currentTractorPullsOnMe here - a reverse tractor pulls
                                //the source toward e, not e toward the source, so e should not be treated as held/tractored
                                //(e.g. CalculateSpeed() would otherwise zero out e's speed, freezing the reverse tractor's target in place)
                            }
                            else
                            {
                                currentTractorPullsOnMe++;

                                int tractorStrength = tractorSource.GetStrengthPerSquad();
                                if (tractorStrength > strongestTractorStrength)
                                {
                                    strongestTractorSource = tractorSource;
                                    strongestTractorStrength = tractorStrength;
                                }
                            }

                            if ( currentTractorPullsOnMe > 100 )
                                break;

                            if ( drawTractorBeams )
                            {
                                var lineType = EntityLineTypeTable.Instance.RowsByHardcodedType[EntityLineHardcodedType.Tractor];
                                if ( lineType != null ) lineType.WriteToDrawBufferForBriefTime( tractorSource, e );
                            }
                        }

                        if (strongestTractorSource != null)
                            strongestTractorSource.CurrentlyStrongestTractorSourceHittingThese.AddToConstructionList(e.PrimaryKeyID);

                        if ( e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull != null )
                            e.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull.SwitchConstructionToDisplay();

                        e.CurrentCountOfTractorsPullingOnThis = currentTractorPullsOnMe;
                        e.CurrentTractorSourcesHittingThis.SwitchConstructionToDisplay();
                    }

                    // Just do for tractor sources.
                    foreach ( GameEntity_Squad e in planet.Squads( EntityRollupType.TractorSource ) )
                    {
                        e.CurrentlyStrongestTractorSourceHittingThese.SwitchConstructionToDisplay();
                        e.OtherEntityPullingMeByMyOwnReverseTractor.SwitchConstructionToDisplay();
                    }

                    // Attribute CC-seconds to fleet metrics for each enemy held in a regular tractor beam this tick.
                    // Each SRP tick that an entity is held counts as 1 CC-second for the tractoring fleet.
                    foreach ( GameEntity_Squad e in planet.Squads() )
                    {
                        if ( e.CurrentCountOfTractorsPullingOnThis <= 0 )
                            continue;
                        var sources = e.CurrentTractorSourcesHittingThis.GetDisplayList();
                        if ( sources == null )
                            continue;
                        for ( int i = 0; i < sources.Count; i++ )
                        {
                            GameEntity_Squad tractorSource = World_AIW2.Instance.GetEntityByID_Squad( sources[i] );
                            if ( tractorSource == null )
                                continue;
                            EntitySystem tractorSystem = null;
                            for ( int s = 0; s < tractorSource.Systems.Count; s++ )
                            {
                                if ( tractorSource.Systems[s].DataForMark.TractorCount > 0 )
                                {
                                    tractorSystem = tractorSource.Systems[s];
                                    break;
                                }
                            }
                            if ( tractorSystem == null )
                                continue;
                            (tractorSource.GetFleetOrNull_Safe()?.BaseInfo as IFleetMetrics)
                                ?.OnFleetCCApplied( 1, e, tractorSystem );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LOG.Err("Error in {0}() exception:\n{1}", this.TypeNameAndMethod(), ex);
            }
            finally
            {
                Interlocked.Increment( ref Accumulator_TRF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
        }

        private void Unrolled_PerTractorSource( PlanetFaction faction )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = faction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.TractorSource );
            if ( rollup == null )
                return;
            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity != null && !entity.HasBeenRemovedFromSim )
                {
                    NonClosure_PerTractorSource( entity );
                }
                wrapper = wrapper.NextItem;
            }
        }

        private void Unrolled_CheckForHostiles( PlanetFaction pFac )
        {
            if ( pFac == null )
                return;
            Planet planet = pFac.Planet;
            if ( planet == null )
                return;
            Faction fac = pFac.Faction;
            if ( fac == null )
                return;

            try
            {
                for ( int i = 0; i < fac.FactionIndicesIAmHostileTo.Count; i++ )
                {
                    if ( planet.Factions[fac.FactionIndicesIAmHostileTo[i]].Entities.GetNumberIn( EntityRollupType.MobileCombatants, false, false ) <= 0 )
                        continue;
                    NonClosure_FoundHostiles = true;
                    break;
                }
            }
            catch { } //client multithreading thing
        }

        private bool NonClosure_FoundHostiles;
        private readonly List<GameEntity_Squad> NonClosure_FilteredTargetList = List<GameEntity_Squad>.Create_WillNeverBeGCed( 3000, "TractorPlanning-NonClosure_FilteredTargetList" );
        private readonly List<SafeSquadWrapper> NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "TractorPlanning-NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring" );
        private readonly List<SafeSquadWrapper> NonClosure_AllPossibleTargetsList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "TractorPlanning-NonClosure_AllPossibleTargetsList" );
        private bool NonClosure_NeedToFillAllPossibleTargetsList;
        private void NonClosure_PerTractorSource( GameEntity_Squad entity )
        {
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                FInt hugeMass = (FInt)999;
                int tractorCount = 0;
                int tractorRange = 0;
                FInt maxAlbedoCanHit = FInt.Zero;
                FInt minAlbedoCanHit = FInt.One;
                int minEngineCanHit = 999;
                int maxEngineCanHit = 0;
                FInt minMassCanHit = hugeMass;
                FInt maxMassCanHit = FInt.Zero;
                debugStage = 2000;
                for ( int i = 0; i < entity.Systems.Count; i++ )
                {
                    debugStage = 2100;
                    EntitySystem system = entity.Systems[i];
                    if ( system.DataForMark.TractorCount <= 0 )
                        continue;
                    debugStage = 2200;
                    if ( system.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                        continue;
                    debugStage = 2300;
                    tractorCount += system.DataForMark.TractorCount;
                    tractorRange = Math.Max( tractorRange, system.DataForMark.TractorRange );
                    minEngineCanHit = Math.Min( minEngineCanHit, system.TypeData.TractorHitsEngine_gxGreaterThan );
                    maxEngineCanHit = Math.Max( maxEngineCanHit, system.TypeData.TractorHitsEngine_gxLessThan );
                    minMassCanHit = Mat.Min( minMassCanHit, system.TypeData.TractorHitsMassGreaterThan );
                    maxMassCanHit = Mat.Max( maxMassCanHit, system.TypeData.TractorHitsMassLessThan );
                    minAlbedoCanHit = Mat.Min( minAlbedoCanHit, system.TypeData.TractorHitsAlbedoGreaterThan );
                    maxAlbedoCanHit = Mat.Max( maxAlbedoCanHit, system.TypeData.TractorHitsAlbedoLessThan );
                }

                debugStage = 3000;
                if ( tractorCount <= 0 )
                    return;

                debugStage = 3100;
                var pfac = entity.PlanetFaction;
                if (pfac == null)
                    return;
                
                debugStage = 4000;
                int maxEntitiesTractorable = tractorCount;
                int currentEntitiesTractored = 0;
                int potentialTractorTargets = 0;
                debugStage = 5000;
                if ( NonClosure_NeedToFillAllPossibleTargetsList )
                {
                    debugStage = 5100;
                    NonClosure_NeedToFillAllPossibleTargetsList = false;
                    for ( int i = 0; i < entity.Planet.Factions.Count; i++ )
                    {
                        debugStage = 5200;
                        PlanetFaction faction = entity.Planet.Factions[i];
                        if (faction == null || faction == pfac)
                            continue;
                        
                        debugStage = 5300;
                        if ( !pfac.GetIsHostileTowards( faction ) )
                            continue;
                        
                        debugStage = 5400;
                        Unrolled_PopulateAllPossibleTargets( faction );
                    }
                }

                debugStage = 6000;
                NonClosure_FilteredTargetList.Clear();
                NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring.Clear();
                NonClosure_TractorSource = entity;
                NonClosure_TractorRange = tractorRange;
                debugStage = 7000;
                int TractorSource_Radius = NonClosure_TractorSource.DataForMark.Radius;
                debugStage = 8000;
                List<SafeSquadWrapper> entities = NonClosure_AllPossibleTargetsList;
                debugStage = 9000;
                for ( int i = 0; i < entities.Count; i++ )
                {
                    GameEntity_Squad otherEntity = entities[i].GetSquad();
                    if ( otherEntity == null )
                        continue;
                    
                    debugStage = 10100;
                    GameEntityTypeData.MarkLevelStats otherData = otherEntity.DataForMark;
                    if ( otherData == null )
                        continue;
                    
                    debugStage = 10110;
                    if ( maxAlbedoCanHit > FInt.Zero && otherData.Albedo >= maxAlbedoCanHit )
                        continue;
                    if ( minAlbedoCanHit > FInt.Zero && minAlbedoCanHit < FInt.One && otherData.Albedo <= minAlbedoCanHit )
                        continue;
                    if ( minEngineCanHit > 0 && minEngineCanHit < 999 && otherEntity.TypeData.Engine_gx <= minEngineCanHit )
                        continue;
                    if ( maxEngineCanHit > 0 && maxEngineCanHit < 999 && otherEntity.TypeData.Engine_gx >= maxEngineCanHit )
                        continue;
                    if ( minMassCanHit > FInt.Zero && minMassCanHit < hugeMass && otherEntity.TypeData.Mass_tX <= minMassCanHit )
                        continue;
                    if ( maxMassCanHit > FInt.Zero && maxMassCanHit < hugeMass && otherEntity.TypeData.Mass_tX >= maxMassCanHit )
                        continue;
                    if ( otherEntity.CurrentStateOfMatter != entity.CurrentStateOfMatter )
                        continue;
                    
                    debugStage = 11100;
                    potentialTractorTargets++;
                    
                    // Do this check last since it's the most expensive.
                    // note: this use to subtract both entity radii from the range .. which doesn't make sense so now it doesn't
                    if ( !NonClosure_TractorSource.GetIsWithinRangeOf( otherEntity, NonClosure_TractorRange) )
                        continue;
                    
                    debugStage = 12100;
                    if ( otherEntity.CurrentTractorSourcesHittingThis.GetConstructionList().Count > 0 )
                        NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring.Add( otherEntity );
                    else
                        NonClosure_FilteredTargetList.Add( otherEntity );
                }

                debugStage = 13100;
                if ( NonClosure_FilteredTargetList.Count <= 0 && NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring.Count > 0 )
                {
                    debugStage = 13200;
                    for ( int i = 0; i < NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring.Count; i++ )
                    {
                        debugStage = 13300;
                        GameEntity_Squad otherEntity = NonClosure_FilteredTargetList_SkippedBecauseOtherEntitiesTractoring[i].GetSquad();
                        if ( otherEntity == null )
                            continue;
                        debugStage = 13400;
                        NonClosure_FilteredTargetList.Add( otherEntity );
                    }
                }

                debugStage = 14100;
                try
                {
                    NonClosure_FilteredTargetList.Sort( DelegateHelper_SortTractorTargets );
                }
                catch { } //cross threading issue when values change.

                debugStage = 15100;
                while ( tractorCount > 0 && NonClosure_FilteredTargetList.Count > 0 )
                {
                    debugStage = 15200;
                    int index = NonClosure_FilteredTargetList.Count - 1;
                    debugStage = 15300;
                    GameEntity_Squad tractorTarget = NonClosure_FilteredTargetList[index];
                    if ( tractorTarget == null )
                    {
                        NonClosure_FilteredTargetList.RemoveAt( index );
                        continue;
                    }
                    debugStage = 15400;
                    tractorCount--;
                    //if a tractor is grappling a stack, then each stacked entity counts as 1/4 of a tractor target for purposes of the grappling capacity of the tractor beam.
                    int stackedComponent = 0;
                    if ( tractorTarget.ExtraStackedSquadsInThis > 3 )
                        stackedComponent = (tractorTarget.ExtraStackedSquadsInThis >> 2);
                    tractorCount -= stackedComponent;
                    tractorTarget.CurrentTractorSourcesHittingThis.AddToConstructionList( entity.PrimaryKeyID );

                    currentEntitiesTractored += 1 + stackedComponent;
                    NonClosure_FilteredTargetList.RemoveAt( index );
                }
                debugStage = 16200;

                entity.ReadyToDragTractoredShipsAway = GetReadyToDragTractoredShipsAway( entity, maxEntitiesTractorable, currentEntitiesTractored, potentialTractorTargets );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NonClosure_PerTractorSource at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("entity " + entity.TypeData.GetDisplayName() + " " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is currently tractoring " + entity.FramePlan_CurrentEntitiesTractored + " out of " + entity.FramePlan_MaxEntitiesTractorable + " max, " + entity.FramePlan_PotentialTractorTargets + " available", Verbosity.DoNotShow );
        }

        #region GetReadyToDragTractoredShipsAway
        public static bool GetReadyToDragTractoredShipsAway( GameEntity_Squad entity, int maxEntitiesTractorable, int currentEntitiesTractored, int potentialTractorTargets)
        {
            //If this ship has tractor beams and has tractored a lot of ships,
            //we want  to drag them off planet and have the defenses on the remote planet kill them
            if ( maxEntitiesTractorable <= 0 )
                return false;

            if ( potentialTractorTargets <= 0 )
                return false;

            //This logic isn't amazing, but it's something? Will likely need improvement later
            if ( currentEntitiesTractored > potentialTractorTargets / 2 )
                return true; //if we've tractored half the potential targets (generally for small numbers of enemy ships)

            if ( currentEntitiesTractored > maxEntitiesTractorable / 2 )
                return true; //if we've got a good number of ships

            int shieldPoints = entity.GetCurrentShieldPoints();
            if ( currentEntitiesTractored > maxEntitiesTractorable / 4 &&
                 shieldPoints == 0 || shieldPoints < entity.GetMaxShieldPoints() )
                return true; //we've got a few ships and we've taken some damage

            return false;
        }
        #endregion

        private void Unrolled_PopulateAllPossibleTargets( PlanetFaction faction )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = faction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.MobileCombatants );
            if ( rollup == null )
                return;
            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity == null || entity.HasBeenRemovedFromSim ) {
                    /* ignore */
                } else if ( entity.GetCurrentCloakingPoints() > 0 ) {
                    /* ignore */
                } else if ( !entity.TypeData.IsMobile ) {
                    /* ignore: this is a mobile orbiter */
                } else if ( !entity.TypeData.ShipClass.CanBeSubjectedToTractorBeams() ) {
                    /* ignore */
                } else if ( entity.GetIsCrippled() ) {
                    /* ignore */
                } else {
                    NonClosure_AllPossibleTargetsList.Add( entity );
                }
                wrapper = wrapper.NextItem;
            }
        }

        private int DelegateHelper_SortTractorTargets( GameEntity_Squad Left, GameEntity_Squad Right )
        {
            int left = Right.GetStrengthPerSquad();
            int right = Left.GetStrengthPerSquad();
            if ( left != right ) return right.CompareTo( left ); // units that are strong go at the end of the list, which goes first

            left = Left.GameSecondEnteredThisPlanet;
            right = Right.GameSecondEnteredThisPlanet;
            if ( left != right ) return right.CompareTo( right ); // lower time on planet goes at beginning of list, which goes last

            return Right.PrimaryKeyID.CompareTo( Left.PrimaryKeyID ); // higher ID goes at beginning of list, which goes last
        }

        private GameEntity_Squad NonClosure_TractorSource;
        private int NonClosure_TractorRange;
    }
}
