using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlannedWaveOptions : ConcurrentPoolable<PlannedWaveOptions>, IProtectedListable
    {
        public GameEntity_Squad overrideEntityToSpawnAt;
        public GameEntityTypeData overrideSpawnUnits;
        public bool isReconquestWave;
        public FInt requiredWaveToDefenseRatio; //if this is (say) 1.0 then the wave must be as strong as the defenses to launch. 0.5 means the wave must be half as strong as the defenses to launch
        public Faction targetFaction;
        public int overrideLaunchTime;
        public bool forceUseAdjacentPlanet;
        public bool forceUseRandomPlanetThatIsNotMark7;
        public Int16 ForcePlanetWithWarpGateIndex; //for tutorial
        public Int16 ForceTargetPlanetIndex; //for tutorial
        public readonly Dictionary<GameEntityTypeData, int> ForceCompositionIfFilled = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 30, "PlannedWaveOptions-ForceCompositionIfFilled" ); //for tutorial
        public bool allowGuardians;
        public bool allowDireGuardians;
        public bool spawnBonusShipFromAIType;

        /// <summary>
        /// Use this if you don't want any of the options to be set up in any particular special way
        /// </summary>
        public static PlannedWaveOptions CreateWithDefaults()
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            return options;
        }

        /// <summary>
        /// If all you want to set is the entity to spawn at...
        /// </summary>
        public static PlannedWaveOptions CreateWithDefaultsAtEntity( GameEntity_Squad overrideEntityToSpawnAt, bool forceUseAdjacentPlanet, bool forceUseRandomPlanetThatIsNotMark7 )
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            return options;
        }

        /// <summary>
        /// If all you want to set is the overriding launch time...
        /// </summary>
        public static PlannedWaveOptions CreateWithDefaultsAtSpecificLaunchTime( int overrideLaunchTime )
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            options.overrideEntityToSpawnAt = null;
            options.overrideSpawnUnits = null;
            options.isReconquestWave = false;
            options.requiredWaveToDefenseRatio = FInt.Zero;
            options.targetFaction = null;
            options.overrideLaunchTime = overrideLaunchTime;
            options.forceUseAdjacentPlanet = false;
            options.forceUseRandomPlanetThatIsNotMark7 = false;
            options.ForcePlanetWithWarpGateIndex = -1;
            options.ForceTargetPlanetIndex = -1;
            options.allowGuardians = true;
            options.allowDireGuardians = false;
            options.spawnBonusShipFromAIType = false;
            return options;
        }

        /// <summary>
        /// Specific setup for reconquest waves
        /// </summary>
        public static PlannedWaveOptions CreateReconquestWave()
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            options.overrideEntityToSpawnAt = null;
            options.overrideSpawnUnits = null;
            options.isReconquestWave = true; //always YES here
            options.requiredWaveToDefenseRatio = FInt.FromParts(1, 200); //The AI intends to win this battle
            options.targetFaction = null;
            options.overrideLaunchTime = 0;
            options.forceUseAdjacentPlanet = false;
            options.forceUseRandomPlanetThatIsNotMark7 = false;
            options.ForcePlanetWithWarpGateIndex = -1;
            options.ForceTargetPlanetIndex = -1;
            options.allowGuardians = true;
            options.allowDireGuardians = false;
            options.spawnBonusShipFromAIType = false;
            return options;
        }

        /// <summary>
        /// Use this if you want to manually set all the options for some reason
        /// </summary>
        public static PlannedWaveOptions CreateWithAllManuallySet( GameEntity_Squad overrideEntityToSpawnAt, GameEntityTypeData overrideSpawnUnits,
                                                                   bool isReconquestWave, FInt ratio, Faction targetFaction, int overrideLaunchTime, bool forceUseAdjacentPlanet, bool forceUseRandomPlanetThatIsNotMark7,
                                                                   bool allowGuardians, bool allowDireGuardians)
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            options.overrideEntityToSpawnAt = overrideEntityToSpawnAt;
            options.overrideSpawnUnits = overrideSpawnUnits;
            options.isReconquestWave = isReconquestWave;
            options.requiredWaveToDefenseRatio = ratio;
            options.targetFaction = targetFaction;
            options.overrideLaunchTime = overrideLaunchTime;
            options.forceUseAdjacentPlanet = forceUseAdjacentPlanet;
            options.forceUseRandomPlanetThatIsNotMark7 = forceUseRandomPlanetThatIsNotMark7;
            options.ForcePlanetWithWarpGateIndex = -1;
            options.ForceTargetPlanetIndex = -1;
            options.allowGuardians = allowGuardians;
            options.allowDireGuardians = allowDireGuardians;
            options.spawnBonusShipFromAIType = false;
            return options;
        }

        /// <summary>
        /// Use this if you want a basic set of options
        /// </summary>
        public static PlannedWaveOptions CreateWithBasics( GameEntity_Squad overrideEntityToSpawnAt, GameEntityTypeData overrideSpawnUnits,
                                                           Faction targetFaction, bool allowGuardians, bool allowDireGuardians )
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            options.overrideEntityToSpawnAt = overrideEntityToSpawnAt;
            options.overrideSpawnUnits = overrideSpawnUnits;
            options.isReconquestWave = false;
            options.requiredWaveToDefenseRatio = FInt.Zero;
            options.targetFaction = targetFaction;
            options.overrideLaunchTime = 0;
            options.forceUseAdjacentPlanet = false;
            options.forceUseRandomPlanetThatIsNotMark7 = false;
            options.ForcePlanetWithWarpGateIndex = -1;
            options.ForceTargetPlanetIndex = -1;
            options.allowGuardians = allowGuardians;
            options.allowDireGuardians = allowDireGuardians;
            return options;
        }

        /// <summary>
        /// We hate some specific minor faction in particular, go get those guys!
        /// </summary>
        public static PlannedWaveOptions CreateAntiMinorFactionWave( bool isReconquestWave, FInt ratio, Faction targetFaction )
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            options.overrideEntityToSpawnAt = null;
            options.overrideSpawnUnits = null;
            options.isReconquestWave = isReconquestWave;
            options.requiredWaveToDefenseRatio = ratio;
            options.targetFaction = targetFaction;
            options.overrideLaunchTime = 0;
            options.forceUseAdjacentPlanet = false;
            options.forceUseRandomPlanetThatIsNotMark7 = true; //let's keep this really different every time!
            options.ForcePlanetWithWarpGateIndex = -1;
            options.ForceTargetPlanetIndex = -1;
            options.allowGuardians = true;
            options.allowDireGuardians = false;
            return options;
        }
        /// <summary>
        /// For the tutorial, allow everything to be specified
        /// </summary>
        public static PlannedWaveOptions CreateTutorialWave( Int16 ForceWarpGateIndex, Int16 ForceTargetIndex,  Dictionary<GameEntityTypeData, int> ForceComposition )
        {
            PlannedWaveOptions options = Pool.GetFromPoolOrCreate();
            //Note that specifying the ForceStrength and ForceComposition together are incompatibly
            options.overrideEntityToSpawnAt = null;
            options.overrideSpawnUnits = null;
            options.isReconquestWave = false;
            options.requiredWaveToDefenseRatio = FInt.Zero;
            options.targetFaction = null;
            options.overrideLaunchTime = 0;
            options.forceUseAdjacentPlanet = false;
            options.forceUseRandomPlanetThatIsNotMark7 = false;
            options.ForcePlanetWithWarpGateIndex = ForceWarpGateIndex;
            options.ForceTargetPlanetIndex = ForceTargetIndex;
            options.ForceCompositionIfFilled.ClearAndCopyFrom( ForceComposition );
            options.allowGuardians = false;
            options.allowDireGuardians = false;
            return options;
        }

        public void SetToDefaults()
        {
            this.overrideEntityToSpawnAt = null;
            this.overrideSpawnUnits = null;
            this.isReconquestWave = false;
            this.requiredWaveToDefenseRatio = FInt.Zero;
            this.targetFaction = null;
            this.overrideLaunchTime = 0;
            this.forceUseAdjacentPlanet = false;
            this.forceUseRandomPlanetThatIsNotMark7 = false;
            this.ForcePlanetWithWarpGateIndex = -1;
            this.ForceTargetPlanetIndex = -1;
            this.ForceCompositionIfFilled.Clear();
            this.allowGuardians = true;
            this.allowDireGuardians = false;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PlannedWaveOptions()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "OptionWrappers" );
            RefTracker.IncrementObjectCount();

            this.SetToDefaults();
        }

        private static ConcurrentPool<PlannedWaveOptions> Pool = new ConcurrentPool<PlannedWaveOptions>( "OptionWrappers", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlannedWaveOptions(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        public void DoBeforeRemoveOrClear()
        {
            this.SetToDefaults();
            this.ReturnToPool();
        }
        #endregion
    }
}
