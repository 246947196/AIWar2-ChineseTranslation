using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlanetWaveComposition : ConcurrentPoolable<PlanetWaveComposition>, IProtectedListable
    {
        public Planet FromPlanet;
        public readonly Dictionary<GameEntityTypeData, int> Composition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 40, "PlanetWaveComposition-Composition" );
        public int spentBudget;

        public void Initialize( Planet From, int spent )
        {
            this.FromPlanet = From;
            this.spentBudget = spent;
        }

        public void SetToDefaults()
        {
            this.FromPlanet = null;
            this.Composition.Clear();
            this.spentBudget = 0;
        }

        public static PlanetWaveComposition GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PlanetWaveComposition()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PlanetWaveCompositions" );
            RefTracker.IncrementObjectCount();

            this.SetToDefaults();
        }

        private static ConcurrentPool<PlanetWaveComposition> Pool = new ConcurrentPool<PlanetWaveComposition>( "PlanetWaveCompositions", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlanetWaveComposition(); } );

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
