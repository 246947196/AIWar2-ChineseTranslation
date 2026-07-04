using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WaveDisplay : AbstractWaveBase, IProtectedListable
    {
        #region Pooling
        private static ReferenceTracker RefTracker;
        private WaveDisplay()
        {            
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "WaveDisplay" );
            RefTracker.IncrementObjectCount();
        }

        private static readonly ConcurrentPool<AbstractWaveBase> Pool = new ConcurrentPool<AbstractWaveBase>( "WaveDisplay", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new WaveDisplay(); } );

        public static WaveDisplay GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate() as WaveDisplay;
        }

        public WaveDisplay CreateNewForPool()
        {
            return new WaveDisplay();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            //we don't care.  It will be updated next time we have CopyFromPlannedWave
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        public void CopyFromPlannedWave( PlannedWave Wave )
        {
            this.disableWaveWarnings = Wave.disableWaveWarnings;
            this.planetWithWarpGateIdx = Wave.planetWithWarpGateIdx;
            this.targetPlanetIdx = Wave.targetPlanetIdx;
            this.overrideEntityToSpawnAt = Wave.overrideEntityToSpawnAt;
            this.FinalComposition.ClearAndCopyFrom( Wave.FinalComposition );
            this.gameTimeInSecondsForLaunchWave = Wave.gameTimeInSecondsForLaunchWave;
            this.spawnWaveDirectlyOnTarget = Wave.spawnWaveDirectlyOnTarget;
            this.isReconquestWave = Wave.isReconquestWave;
            this.isActuallyACrossPlanetAttack = Wave.isActuallyACrossPlanetAttack;
            this.secondsAdvanceWarningToGive = Wave.secondsAdvanceWarningToGive;
            this.playerBeingAlerted = Wave.playerBeingAlerted;
            this.aiCostBudgetForWave = Wave.aiCostBudgetForWave;
            this.StrengthOfWave = Wave.StrengthOfWave;
            this.cancelRefundRatioForNextWave = Wave.cancelRefundRatioForNextWave;
            this.cancelRefundRatioForNextWormholeInvasion = Wave.cancelRefundRatioForNextWormholeInvasion;
            this.deQueueWave = Wave.deQueueWave;
            this.isExogalacticWormholeWave = Wave.isExogalacticWormholeWave;
            this.sendWaveThisSimStep = Wave.sendWaveThisSimStep;
            this.NonSimIsAgainstAHumanHomeworld = Wave.NonSimIsAgainstAHumanHomeworld;
            this.NonSimPlanetName = Wave.NonSimPlanetName;
            this.SendingFactionIndex = Wave.SendingFactionIndex;
            this.TargetFactionIndex = Wave.TargetFactionIndex;
            this.IsAstroTrainWave = Wave.IsAstroTrainWave;
            this.DebugString = Wave.DebugString;
        }
    }
}
