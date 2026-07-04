using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class GravityPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<GravityPlanning>
    {
        private static ReferenceTracker RefTracker;
        private GravityPlanning()
            : base( "_ST.GravityPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "GravityPlanning" );
            RefTracker.IncrementObjectCount();
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_GVB = 0;
            Accumulator_GVF = 0;
        }

        private static readonly BetweenMapGenPool<GravityPlanning> Pool = BetweenMapGenPool<GravityPlanning>.Create_WillNeverBeGCed( "GravityPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new GravityPlanning(); } );

        public static GravityPlanning GetFromPoolOrCreate()
        {
            GravityPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        public static int Accumulator_GVB = 0;
        public static int Accumulator_GVF = 0;

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "GravityPlanning", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_GRAVITY_PLAN )
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;

            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                Interlocked.Increment( ref Accumulator_GVB );

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    //do for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.IsBlackHoledAtMoment.ClearConstructionValueForStartingConstruction();
                        entity.CurrentGravitySpeedMultiplier.ClearConstructionValueForStartingConstruction();
                    }

                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        PlanetFaction faction = planet.Factions[j];
                        if ( faction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.GravitySource ) <= 0 )
                            continue; //skip any planets that don't even have any gravity sources, since that is much more efficient

                        NonClosure_FoundHostiles = false;
                        Unrolled_CheckForHostiles( faction );
                        if ( !NonClosure_FoundHostiles )
                        {
                            //normally if we have no hostiles, we have nothing to process
                            //but if we have someting that blackholes even friendly units, then we need to process that
                            foreach ( GameEntity_Squad gravitySource in faction.Entities.Squads( EntityRollupType.GravitySource ) )
                            {
                                if ( gravitySource.TypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                                {
                                    NonClosure_FoundHostiles = true;
                                    break;
                                }
                            }
                            if ( !NonClosure_FoundHostiles )
                                continue;
                        }

                        //ArcenDebugging.ArcenDebugLogSingleLine( "Gravity sources to try checking at: " + planet.Name, Verbosity.DoNotShow );

                        NonClosure_AllPossibleTargetsList.Clear();
                        NonClosure_NeedToFillAllPossibleTargetsList = true;
                        Unrolled_PerGravitySource( faction );
                    }

                    //do for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.IsBlackHoledAtMoment.SwitchConstructionToDisplay();
                        entity.CurrentGravitySpeedMultiplier.SwitchConstructionToDisplay();
                    }
                }

                Interlocked.Increment( ref Accumulator_GVF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in GravityPlanning.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void Unrolled_PerGravitySource( PlanetFaction faction )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = faction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.GravitySource );
            if ( rollup == null )
                return;

            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity != null && !entity.HasBeenRemovedFromSim )
                {
                    NonClosure_PerGravitySource( entity );
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
        private readonly List<SafeSquadWrapper> NonClosure_AllPossibleTargetsList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "GravityPlanning-NonClosure_AllPossibleTargetsList" );
        private bool NonClosure_NeedToFillAllPossibleTargetsList;
        private void NonClosure_PerGravitySource( GameEntity_Squad gravitySource )
        {
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                for ( int i = 0; i < gravitySource.Systems.Count; i++ )
                {
                    EntitySystem system = gravitySource.Systems[i];
                    debugStage = 2000;
                    if ( system.TypeData.GravityHitsEngine_gxLessThan <= 0 )
                        continue;
                    debugStage = 3000;
                    FInt speedMultiplier = system.DataForMark.GravitySpeedMultiplier;
                    if ( speedMultiplier <= 0 )
                        continue;
                    debugStage = 4000;
                    if ( system.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Gravity source: " + entity.TypeData.DisplayName + " disabled: " + system.ForShortTermPlanning_DisabledReason, Verbosity.DoNotShow );
                        continue;
                    }
                    debugStage = 5000;
                    if ( NonClosure_NeedToFillAllPossibleTargetsList )
                    {
                        debugStage = 5100;
                        NonClosure_NeedToFillAllPossibleTargetsList = false;
                        for ( int j = 0; j < gravitySource.Planet.Factions.Count; j++ )
                        {
                            debugStage = 5200;
                            PlanetFaction faction = gravitySource.Planet.Factions[j];
                            if ( !gravitySource.PlanetFaction.GetIsHostileTowards( faction ) )
                                continue;
                            Unrolled_PopulateAllPossibleTargets( faction );
                        }
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Gravity source: " + entity.TypeData.DisplayName + " has targets: " + 
                    //    NonClosure_AllPossibleTargetsList.Count, Verbosity.DoNotShow );

                    debugStage = 6000;
                    List<SafeSquadWrapper> entities = NonClosure_AllPossibleTargetsList;
                    for ( int j = 0; j < entities.Count; j++ )
                    {
                        debugStage = 6100;
                        GameEntity_Squad otherEntity = entities[j].GetSquad();
                        if ( otherEntity == null )
                            continue;
                        if ( otherEntity.TypeData.Engine_gx >= system.TypeData.GravityHitsEngine_gxLessThan )
                            continue;
                        if ( !gravitySource.GetIsWithinRangeOf( otherEntity, system.DataForMark.GravityRange ) )
                            continue;
                        
                        FInt gravSpeed = otherEntity.CurrentGravitySpeedMultiplier.Construction;
                        if ( gravSpeed <= FInt.Zero )
                            gravSpeed = system.DataForMark.GravitySpeedMultiplier;
                        else
                            gravSpeed *= system.DataForMark.GravitySpeedMultiplier;
                        
                        otherEntity.CurrentGravitySpeedMultiplier.Construction = gravSpeed;
                    }
                }

                debugStage = 7000;
                if ( gravitySource.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan > 0 )
                {
                    List<SafeSquadWrapper> entities = NonClosure_AllPossibleTargetsList;
                    for ( int j = 0; j < entities.Count; j++ )
                    {
                        GameEntity_Squad otherEntity = entities[j].GetSquad();
                        if ( otherEntity == null )
                            continue;
                        if ( otherEntity.TypeData.Engine_gx >= gravitySource.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan ||
                            !otherEntity.TypeData.ShipClass.CanBeSubjectedToBlackHoleMachines() || otherEntity.GetIsCrippled() )
                            continue;
                        otherEntity.IsBlackHoledAtMoment.Construction = true;
                    }
                }
                if ( gravitySource.TypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                {
                    //have to loop like this to hit even our friends
                    Planet gravPlanet = gravitySource?.Planet;
                    if ( gravPlanet != null )
                    {
                        foreach ( GameEntity_Squad otherEntity in gravPlanet.Squads() )
                        {
                            otherEntity.IsBlackHoledAtMoment.Construction = true;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NonClosure_PerGravitySource at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void Unrolled_PopulateAllPossibleTargets( PlanetFaction faction )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = faction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.MobileCombatants );
            if ( rollup == null )
                return;

            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity != null && !entity.HasBeenRemovedFromSim )
                {
                    NonClosure_AllPossibleTargetsList.Add( entity );
                }
                wrapper = wrapper.NextItem;
            }
        }
    }
}
