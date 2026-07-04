using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class AreaBoostPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<AreaBoostPlanning>
    {
        private static ReferenceTracker RefTracker;
        private AreaBoostPlanning()
            : base( "_ST.AreaBoostPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AreaBoostPlanning" );
            RefTracker.IncrementObjectCount();
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_ABB = 0;
            Accumulator_ABF = 0;
        }

        private static readonly BetweenMapGenPool<AreaBoostPlanning> Pool = BetweenMapGenPool<AreaBoostPlanning>.Create_WillNeverBeGCed( "AreaBoostPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AreaBoostPlanning(); } );

        public static AreaBoostPlanning GetFromPoolOrCreate()
        {
            AreaBoostPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        public static int Accumulator_ABB = 0;
        public static int Accumulator_ABF = 0;

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "AreaBoostPlanning", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_AREABOOST_PLAN )
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;

            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                Interlocked.Increment( ref Accumulator_ABB );

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    //do for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.AreaBoosters_ShotEvaluated.ClearConstructionListForStartingConstruction();
                        entity.AreaBoosting_CurrentCount.ClearConstructionValueForStartingConstruction();
                        
                        // jcf: unused, but could be
                        //entity.AreaBoosters_NotShotEvaluated.ClearConstructionListForStartingConstruction();
                    }

                    foreach ( GameEntity_Squad _ab_e in planet.Squads( EntityRollupType.ProjectsAreaBoost ) )
                        DelegateHelper_AreaBoostPlanning_CheckAreaBooster( _ab_e );

                    //do for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.AreaBoosters_ShotEvaluated.SwitchConstructionToDisplay();
                        entity.AreaBoosting_CurrentCount.SwitchConstructionToDisplay();
                        
                        // jcf: unused, but could be
                        //entity.AreaBoosters_NotShotEvaluated.SwitchConstructionToDisplay();
                    }
                }

                Interlocked.Increment( ref Accumulator_ABF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e)
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in AreaBoostPlanning.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private DelReturn DelegateHelper_AreaBoostPlanning_CheckAreaBooster( GameEntity_Squad areaBooster )
        {
            if ( areaBooster.TypeData.DoesAttractShotsAgainstAllies )
            {
                //areaBooster.AreaBoosting_CurrentCount.ClearConstructionValueForStartingConstruction();
                DelegateHelper_AreaBoostPlanning_CheckAttractsShotsAgainstAllies( areaBooster );
                //areaBooster.AreaBoosting_CurrentCount.SwitchConstructionToDisplay();
            }
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Requested area boost planning for " + areaBooster.TypeData.InternalName +
                    " without knowing how to actually do that.", Verbosity.ShowAsError );
            }
            return DelReturn.Continue;
        }

        private DelReturn DelegateHelper_AreaBoostPlanning_CheckAttractsShotsAgainstAllies( GameEntity_Squad areaBooster )
        {
            int debugStage = 0;
            try
            {
                int radius = 0;
                debugStage = 1000;
                foreach ( EntitySystem sys in areaBooster.Systems )
                {
                    if ( sys == null )
                        continue;
                    EntitySystemTypeData.MarkLevelStats sysDataForMark = sys.DataForMark;
                    if ( sysDataForMark == null )
                        continue;
                    debugStage = 1100;
                    if ( sysDataForMark.AttractRangeForShotsAgainstAllies <= 0 )
                        continue;
                    debugStage = 1200;
                    if ( sys.ComputeDisabledReason() != ArcenRejectionReason.Unknown )
                        continue;
                    debugStage = 1300;
                    debugStage = 1500;
                    radius = sysDataForMark.AttractRangeForShotsAgainstAllies;
                }
                
                //this can validly happen if none of the relevant systems are actually turned on.
                if ( radius <= 0 )
                    return DelReturn.Continue;

                debugStage = 2000;
                FactionRelationship areaBoostRelationship = FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards );

                debugStage = 3000;
                DelegateHelper_AreaBoostPlanning_areaBoostCenter = areaBooster.WorldLocation;
                DelegateHelper_AreaBoostPlanning_radius = radius;
                DelegateHelper_AreaBoostPlanning_boostSource = areaBooster;

                debugStage = 4000;
                PlanetFaction fac;
                for ( int i = 0; i < areaBooster.Planet.Factions.Count; i++ )
                {
                    debugStage = 4100;
                    fac = areaBooster.Planet.Factions[i];
                    debugStage = 4200;
                    if ( fac == null )
                        continue;
                    debugStage = 4300;
                    if ( !areaBooster.PlanetFaction.GetHasRelationshipWith( fac, areaBoostRelationship ) )
                        continue;

                    debugStage = 4400;
                    foreach ( GameEntity_Squad _ab_e in fac.Entities.Squads() )
                        DelegateHelper_AreaBoostPlanning_TryToBoost( _ab_e );
                }
                debugStage = 5000;
                DelegateHelper_AreaBoostPlanning_boostSource = null;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in DelegateHelper_AreaBoostPlanning_CheckAttractsShotsAgainstAllies at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
            
            return DelReturn.Continue;
        }

        private ArcenPoint DelegateHelper_AreaBoostPlanning_areaBoostCenter;
        private GameEntity_Squad DelegateHelper_AreaBoostPlanning_boostSource;
        private int DelegateHelper_AreaBoostPlanning_radius;
        private DelReturn DelegateHelper_AreaBoostPlanning_TryToBoost( GameEntity_Squad otherEntity )
        {
            if (  otherEntity == null )
                return DelReturn.Continue;
            
            if ( otherEntity.TypeData.RollupList.Contains(EntityRollupType.ProjectsAreaBoost) )
                return DelReturn.Continue;
            
            //if ( otherEntity.TypeData.RollupList.Contains(EntityRollupType.ProjectsForcefield) )
                //return DelReturn.Continue;

                //first very raw check, good for weeding out
            if ( !otherEntity.WorldLocation.GetHasAnyChanceOfBeingInRange( DelegateHelper_AreaBoostPlanning_areaBoostCenter, DelegateHelper_AreaBoostPlanning_radius ) )
                return DelReturn.Continue;

            int debugStage = 0;
            try
            {
                debugStage = 100;

                var areaBooster = DelegateHelper_AreaBoostPlanning_boostSource;
                if ( areaBooster == null )
                    return DelReturn.Continue;
                if ( areaBooster == otherEntity )
                    return DelReturn.Continue;
                var otherEntityDataForMark = otherEntity.DataForMark;
                if ( otherEntityDataForMark == null )
                    return DelReturn.Continue;

                debugStage = 200;
                int radius = DelegateHelper_AreaBoostPlanning_radius;

                debugStage = 1000;
                int distance = areaBooster.GetDistanceTo_ExpensiveAccurate( otherEntity, RadiusCheck.SubtractRadiiFromDistance, true );
                int threshold = otherEntityDataForMark.Radius + radius;
                debugStage = 2000;
                if ( distance >= threshold )
                    return DelReturn.Continue;
                
                debugStage = 3000;
                if ( areaBooster.TypeData.ProjectsAreaBoost_ShotEvaluated )
                {
                    if ( otherEntity.AreaBoosters_ShotEvaluated.GetConstructionList().Count < GameEntity_Squad.MaxAttractorsToTrack)
                        otherEntity.AreaBoosters_ShotEvaluated.AddToConstructionList( areaBooster );
                    
                    // jcf: unused, but could be
                    //else
                        //otherEntity.AreaBoosters_NotShotEvaluated.AddToConstructionList( areaBooster );

                    areaBooster.AreaBoosting_CurrentCount.Construction++;
                }
                else
                {
                    LOG.Msg("Error in {0}() for areaBoost {1} which somehow is ProjectsAreaBoost_ShotEvaluated=false?", this.TypeNameAndMethod(), areaBooster);
                }

                debugStage = 3700;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "DelegateHelper_AreaBoostPlanning_TryToBoost: Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
            return DelReturn.Continue;
        }
    }
}
