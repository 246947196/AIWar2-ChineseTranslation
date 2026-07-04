using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class TachyonPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<TachyonPlanning>
    {
        private static ReferenceTracker RefTracker;
        private TachyonPlanning()
            : base( "_ST.TachyonPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "TachyonPlanning" );
            RefTracker.IncrementObjectCount();
        }

        public static int Accumulator_TAB = 0;
        public static int Accumulator_TAF = 0;

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_TAB = 0;
            Accumulator_TAF = 0;
        }

        private static readonly BetweenMapGenPool<TachyonPlanning> Pool = BetweenMapGenPool<TachyonPlanning>.Create_WillNeverBeGCed( "TachyonPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TachyonPlanning(); } );

        public static TachyonPlanning GetFromPoolOrCreate()
        {
            TachyonPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "TachyonPlanning", 30f );
        private bool _drawBeams;

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_TACHYON_PLAN )
                return;
            if (World.Instance.IsPaused) //If paused, don't even bother targetting things. 
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;

            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                Interlocked.Increment( ref Accumulator_TAB );

                var drawBeams = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawTachyonBeams );

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    _drawBeams = drawBeams;
                    if ( planet.Index != PlayerAccount_AIW2.GetViewingPlanetIndexSafe() )
                        _drawBeams = false;

                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        PlanetFaction faction = planet.Factions[j];
                        if ( faction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.TachyonSource ) <= 0 )
                            continue;

                        NonClosure_FoundHostiles = false;
                        Unrolled_CheckForHostiles( faction );
                        if ( !NonClosure_FoundHostiles )
                            continue;

                        NonClosure_AllPossibleTargetsList.Clear();
                        Unrolled_PerTachyonSource( faction );
                    }
                }
                Interlocked.Increment( ref Accumulator_TAF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in TachyonPlanning.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void Unrolled_PerTachyonSource( PlanetFaction faction )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = faction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.TachyonSource );
            if ( rollup == null )
                return;
            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity != null && !entity.HasBeenRemovedFromSim )
                {
                    NonClosure_PerTachyonSource( entity );
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
                    if ( planet.Factions[fac.FactionIndicesIAmHostileTo[i]].Entities.SquadCount <= 0 )
                        continue;
                    NonClosure_FoundHostiles = true;
                    break;
                }
            }
            catch { } //client multithreading thing
        }

        private bool NonClosure_FoundHostiles;
        private readonly List<GameEntity_Squad> NonClosure_FilteredTargetList = List<GameEntity_Squad>.Create_WillNeverBeGCed( 3000, "TachyonPlanning-NonClosure_FilteredTargetList" );
        private readonly List<SafeSquadWrapper> NonClosure_AllPossibleTargetsList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "TachyonPlanning-NonClosure_AllPossibleTargetsList" );

        private void NonClosure_PerTachyonSource( GameEntity_Squad tachyonEmitterEntity )
        {
            if ( tachyonEmitterEntity == null )
                return;
            PlanetFaction tachyonPFac = tachyonEmitterEntity.PlanetFaction;
            if ( tachyonPFac == null )
                return;

            #region Tracing
            bool trace = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ) && tachyonEmitterEntity == GameEntity_Base.CurrentlyHoveredOver;
            if ( trace ) ArcenDebugging.SingleLineQuickDebug( "Tachyon planning for emitter: " + tachyonEmitterEntity.PrimaryKeyID + ":" + tachyonEmitterEntity.TypeData.InternalName );
            #endregion
            int debugStage = 0;
            try
            {
                int tachyonRange = 0;
                FInt maxAlbedoCanHit = FInt.Zero;
                FInt minAlbedoCanHit = FInt.One;
                int tachyonPoints = 0;
                //tachyonEmitterEntity.DebugText = "";
                debugStage = 1000;
                for ( int i = 0; i < tachyonEmitterEntity.Systems.Count; i++ )
                {
                    debugStage = 2000;
                    EntitySystem system = tachyonEmitterEntity.Systems[i];
                    debugStage = 2200;
                    int thisSystemTachyonRange = system.DataForMark.TachyonRange;
                    debugStage = 2300;
                    if ( thisSystemTachyonRange <= 0 )
                        continue;
                    if ( system.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                        continue;
                    tachyonRange = Math.Max( tachyonRange, thisSystemTachyonRange );
                    tachyonPoints = Math.Max( tachyonPoints, system.DataForMark.TachyonPoints );
                    maxAlbedoCanHit = Mat.Max( maxAlbedoCanHit, system.TypeData.TachyonHitsAlbedoLessThan );
                    minAlbedoCanHit = Mat.Min( minAlbedoCanHit, system.TypeData.TachyonHitsAlbedoMoreThan );

                    //tachyonEmitterEntity.DebugText += system.TypeData.TachyonHitsAlbedoLessThan.ToDoubleNonSim().ToString( "0.000" ) + "a ";
                }
                #region Tracing
                if ( trace ) ArcenDebugging.SingleLineQuickDebug( "Range:" + tachyonRange );
                #endregion

                //tachyonEmitterEntity.DebugText += "tachyonRange: " + tachyonRange + " " +
                //    "tachyonPoints: " + tachyonPoints + " " +
                //    "maxAlbedoCanHit: " + maxAlbedoCanHit.ToDoubleNonSim().ToString( "0.000" ) + " ";

                debugStage = 3000;
                if ( tachyonRange <= 0 )
                    return;

                debugStage = 3100;
                tachyonPoints = (tachyonPoints * tachyonEmitterEntity.Planet.LocalMultiplierWhenNoDeltaTime).GetNearestIntPreferringHigher();
                if ( tachyonPoints < 1 )
                    tachyonPoints = 1;


                debugStage = 4000;
                NonClosure_AllPossibleTargetsList.Clear();
                for ( int i = 0; i < tachyonEmitterEntity.Planet.Factions.Count; i++ )
                {
                    debugStage = 4100;
                    Planet plan = tachyonEmitterEntity.Planet;
                    if ( plan == null )
                        continue;
                    PlanetFaction faction = plan.Factions[i];
                    if ( faction == null )
                        continue;
                    if ( !tachyonPFac.GetIsHostileTowards( faction ) )
                        continue;
                    debugStage = 4200;
                    Unrolled_PopulateAllPossibleTargets( faction );
                }
                debugStage = 4300;
                //tachyonEmitterEntity.DebugText = " tachyonPoints: " + tachyonPoints + 
                //    " NonClosure_AllPossibleTargetsList: " + NonClosure_AllPossibleTargetsList.Count;

                #region Tracing
                if ( trace ) ArcenDebugging.SingleLineQuickDebug( "PossibleTargetCount:" + NonClosure_AllPossibleTargetsList.Count );
                #endregion
                NonClosure_FilteredTargetList.Clear();
                NonClosure_TachyonSource = tachyonEmitterEntity;
                debugStage = 5100;
                List<SafeSquadWrapper> entities = NonClosure_AllPossibleTargetsList;
                for ( int i = 0; i < entities.Count; i++ )
                {
                    debugStage = 5200;
                    GameEntity_Squad otherEntity = entities[i].GetSquad();
                    if ( otherEntity == null )
                        continue;
                    debugStage = 5300;
                    GameEntityTypeData.MarkLevelStats dataForMark = otherEntity.DataForMark;
                    if ( dataForMark  == null )
                        continue;
                    debugStage = 5310;
                    if ( dataForMark.Albedo > maxAlbedoCanHit && maxAlbedoCanHit > FInt.Zero )
                        continue;
                    if ( dataForMark.Albedo < minAlbedoCanHit && minAlbedoCanHit < FInt.One )
                        continue;
                    if ( otherEntity.GetMaxCloakingPoints() <= 0 )
                        continue;
                    if ( otherEntity.CurrentStateOfMatter != tachyonEmitterEntity.CurrentStateOfMatter )
                        continue;
                    if ( !NonClosure_TachyonSource.GetIsWithinRangeOf( otherEntity, tachyonRange ) )
                        continue;
                    otherEntity.Working_TachyonOnly_CloakingPoints = otherEntity.GetCurrentCloakingPoints();
                    NonClosure_FilteredTargetList.Add( otherEntity );
                }
                #region Tracing
                if ( trace ) ArcenDebugging.SingleLineQuickDebug( "FilteredTargetCount:" + NonClosure_FilteredTargetList.Count );
                #endregion


                //tachyonEmitterEntity.DebugText = " tachyonPoints: " + tachyonPoints +
                //    " AllPossibleTargets: " + NonClosure_AllPossibleTargetsList.Count +
                //    " FilteredTargets: " + NonClosure_FilteredTargetList.Count;

                debugStage = 6200;
                for ( int i = 0; i < NonClosure_FilteredTargetList.Count; i++ )
                {
                    GameEntity_Squad tachyonTarget = NonClosure_FilteredTargetList[i]; //this one can't be null, so no need to protect on that one
                    if ( tachyonTarget == null )
                        continue;

                    #region Tracing
                    if ( trace ) ArcenDebugging.SingleLineQuickDebug( "Decloaking target: " + tachyonTarget.PrimaryKeyID + ":" + tachyonTarget.TypeData.InternalName );
                    #endregion
                    {
                        int currentTach = tachyonPoints;
                        if ( tachyonTarget.TypeData?.ShipClass != null )
                            currentTach = tachyonTarget.TypeData.ShipClass.ModifyDebuff( ShipClassData_DebuffType.TachyonBeams, currentTach );
                        tachyonTarget.CloakingPointsLost += currentTach;
                        int maxToLose = tachyonTarget.GetMaxCloakingPoints();
                        if ( tachyonTarget.CloakingPointsLost > maxToLose )
                            tachyonTarget.CloakingPointsLost = maxToLose;

                        tachyonTarget.GameSecondOfLastCloakingPointLoss = World_AIW2.Instance.GameSecond;

                        if (_drawBeams && tachyonTarget.GetCurrentCloakingPoints() > 0)
                        {
                            var line = EntityLineTypeTable.Instance.RowsByHardcodedType[EntityLineHardcodedType.Tachyon];
                            line?.WriteToDrawBufferForBriefTime( tachyonEmitterEntity, tachyonTarget );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NonClosure_PerTachyonSource at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void Unrolled_PopulateAllPossibleTargets( PlanetFaction faction )
        {
            //only look at things that are cloak-able.
            foreach ( GameEntity_Squad entity in faction.Entities.Squads( EntityRollupType.HasAnyInternalCloakingAbility ) )
            {
                //don't care if they are currently cloaked or not
                NonClosure_AllPossibleTargetsList.Add( entity );
            }
        }

        private GameEntity_Squad NonClosure_TachyonSource;
    }    
}
