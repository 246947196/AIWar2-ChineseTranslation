using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Not only is this unique per faction, but also per thread (via pooled lists).  That way threads don't get mixed up between the pathfinding across multiple factions.
    /// </summary>
    public class PathCache : ConcurrentPoolable<PathCache>, IProtectedListable
    {
        private CacheForPathingMode[] pathsByCachingMode = null;
        public int FactionIndex;

        public static int TotalPathCachesEver = 1;

        internal static PathCache GetFromPoolOrCreate( int FactionIndex )
        {
            PathCache cache = Pool.GetFromPoolOrCreate();
            cache.FactionIndex = FactionIndex;
            //ArcenDebugging.ArcenDebugLogSingleLine( "PathCache GetFromPoolOrCreate", Verbosity.DoNotShow );
            return cache;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PathCache()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PathCaches" );
            RefTracker.IncrementObjectCount();

            System.Threading.Interlocked.Add( ref TotalPathCachesEver, 1 );
        }

        private static ConcurrentPool<PathCache> Pool = new ConcurrentPool<PathCache>( "PathCaches", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PathCache(); } );

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

        public void Clear()
        {
            if ( this.pathsByCachingMode != null )
            {
                for ( int i = 0; i < this.pathsByCachingMode.Length; i++ )
                {
                    CacheForPathingMode cache = this.pathsByCachingMode[i];
                    if ( cache != null )
                    {
                        cache.ReturnToPool();
                        this.pathsByCachingMode[i] = null;
                    }
                }
            }
        }

        #region GetOrAddCacheForPathingMode
        public CacheForPathingMode GetOrAddCacheForPathingMode( PathingMode PathingMode )
        {
            CacheForPathingMode cacheToReturn = null;
            if ( pathsByCachingMode == null )
                pathsByCachingMode = new CacheForPathingMode[(int)PathingMode.Length];
            else
                cacheToReturn = pathsByCachingMode[(int)PathingMode];

            if ( cacheToReturn == null )
            {
                cacheToReturn = CacheForPathingMode.GetFromPoolOrCreate( this.FactionIndex, PathingMode );
                pathsByCachingMode[(int)PathingMode] = cacheToReturn;
            }
            else
            {
                cacheToReturn.FactionIndex = this.FactionIndex;
                cacheToReturn.PathingMode = PathingMode;
            }

            return cacheToReturn;
        }
        #endregion

        #region class CacheForPathingMode
        public class CacheForPathingMode : ConcurrentPoolable<CacheForPathingMode>
        {
            public int FactionIndex;
            public PathingMode PathingMode;
            private DictionaryOfProtectedValDictionaries<int, int, PathBetweenPlanetsForFaction> toFromPlanetCache = null;

            internal static CacheForPathingMode GetFromPoolOrCreate( int FactionIndex, PathingMode PathingMode )
            {
                CacheForPathingMode cache = Pool.GetFromPoolOrCreate();
                cache.FactionIndex = FactionIndex;
                cache.PathingMode = PathingMode;
                //ArcenDebugging.ArcenDebugLogSingleLine( "CacheForPathingMode GetFromPoolOrCreate", Verbosity.DoNotShow );
                return cache;
            }

            #region Pooling
            private static ReferenceTracker RefTracker;
            private CacheForPathingMode()
            {
                if ( RefTracker == null )
                    RefTracker = new ReferenceTracker( "CacheForPathingModes" );
                RefTracker.IncrementObjectCount();
            }

            private static ConcurrentPool<CacheForPathingMode> Pool = new ConcurrentPool<CacheForPathingMode>( "CacheForPathingModes", 30000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new CacheForPathingMode(); } );

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

            public void Clear()
            {
                this.FactionIndex = -1;
                this.PathingMode = PathingMode.Length;

                if ( this.toFromPlanetCache != null )
                    this.toFromPlanetCache.Clear();
            }

            #region GetOrAddToFromPlanetData
            public PathBetweenPlanetsForFaction GetOrAddToFromPlanetDataOrNull( Planet ToPlanet, Planet FromPlanet )
            {
                if ( ToPlanet == null || FromPlanet == null )
                    return null;
                return this.GetOrAddToFromPlanetDataOrNull( ToPlanet.Index, FromPlanet.Index );
            }

            public PathBetweenPlanetsForFaction GetOrAddToFromPlanetDataOrNull( int ToPlanetIndex, int FromPlanetIndex )
            {
                //if this is not going to result in a path, return null.  Don't waste time.
                if ( ToPlanetIndex < 0 || FromPlanetIndex < 0 )
                    return null;
                if ( ToPlanetIndex == FromPlanetIndex )
                    return null;

                //first instantiate the outer dictionary as a whole if need be
                if ( toFromPlanetCache == null )
                    toFromPlanetCache = DictionaryOfProtectedValDictionaries<int, int, PathBetweenPlanetsForFaction>.Create_WillNeverBeGCed( 300, 300, "Pathing-toFromPlanetCache" );

                //the inner dictionary will be created if it's not there, or it will return what is there.  Either is fine.
                ProtectedValDictionary<int, PathBetweenPlanetsForFaction> fromDictionary = toFromPlanetCache[ToPlanetIndex];

                //now try to find the actual path between to and from (yes direction matters)
                PathBetweenPlanetsForFaction pathCacheToReturn = null;
                if ( fromDictionary.TryGetValue( FromPlanetIndex, out pathCacheToReturn ) )
                    return pathCacheToReturn;
                else
                {
                    //if we can't, then create the path
                    pathCacheToReturn = PathBetweenPlanetsForFaction.GetFromPoolOrCreate( this.FactionIndex, this.PathingMode );
                    fromDictionary[FromPlanetIndex] = pathCacheToReturn;
                }

                return pathCacheToReturn;
            }
            #endregion
        }
        #endregion
    }
}
