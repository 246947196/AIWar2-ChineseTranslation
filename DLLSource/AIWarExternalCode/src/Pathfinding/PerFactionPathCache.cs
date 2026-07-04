using Arcen.Universal;
using System;
using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is meant to be used by a given thread while it does its calculations for one iteration -- 
    /// whatever that means for that specific thread (could be a single frame, could be a single LRP run, whatever)) --
    /// and then returned to the pool when the thread is done.
    /// </summary>
    public class PerFactionPathCache : ConcurrentPoolable<PerFactionPathCache>, IPerFactionPathCache
    {
        private ProtectedValDictionary<int, PathCache> pathCacheLookupByFactionID = ProtectedValDictionary<int, PathCache>.Create_WillNeverBeGCed( 10, "PerFactionPathCache-pathCacheLookupByFactionID" );

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PerFactionPathCache()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PerFactionPathCaches" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<PerFactionPathCache> Pool = new ConcurrentPool<PerFactionPathCache>( "PerFactionPathCaches", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PerFactionPathCache(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.Clear();
        }

        public void DoBeforeRemoveOrClear()
        {
            this.Clear();
            this.ReturnToPool();
        }
        #endregion

        public static PerFactionPathCache GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public void Clear()
        {
            this.pathCacheLookupByFactionID.Clear();
        }

        public PathCache GetOrAddPathCacheForFactionInThisThread( int FactionIndex )
        {
            PathCache result = null;
            if ( pathCacheLookupByFactionID.TryGetValue( FactionIndex, out result ) )
                return result;
            result = PathCache.GetFromPoolOrCreate( FactionIndex );
            pathCacheLookupByFactionID[FactionIndex] = result;
            return result;
        }
    }
}
