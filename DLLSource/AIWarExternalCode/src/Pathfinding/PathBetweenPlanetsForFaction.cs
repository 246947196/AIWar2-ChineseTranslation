using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class PathBetweenPlanetsForFaction : ConcurrentPoolable<PathBetweenPlanetsForFaction>, IProtectedListable
    {
        public readonly List<Planet> PathToReadOnly = List<Planet>.Create_WillNeverBeGCed( 60, "PathBetweenPlanetsForFaction-PathToReadOnly", 90 );
        public float TimeOfLastPathCalculation;
        public int ForFactionIndex;
        public PathingMode PathingModeUse;

        public readonly bool IfYouAlterThePathToReadOnlyItWillBreakEverythingSoDoNotDoThat = true;
        public float RecalculatesOnThisIntervalOfSeconds = 3f;

        public static Int64 PathRecalculations = 0;
        public static Int64 PathsPulledFromCache = 0;

        internal static PathBetweenPlanetsForFaction GetFromPoolOrCreate( int ForFactionIndex, PathingMode PathingModeUse )
        {
            PathBetweenPlanetsForFaction path = Pool.GetFromPoolOrCreate();
            path.ForFactionIndex = ForFactionIndex;
            path.PathingModeUse = PathingModeUse;
            //ArcenDebugging.ArcenDebugLogSingleLine( "PathBetweenPlanetsForFaction GetFromPoolOrCreate", Verbosity.DoNotShow );
            return path;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PathBetweenPlanetsForFaction()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PathBetweenPlanetsForFactions" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<PathBetweenPlanetsForFaction> Pool = new ConcurrentPool<PathBetweenPlanetsForFaction>( "PathBetweenPlanetsForFactions", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PathBetweenPlanetsForFaction(); } );

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
            this.PathToReadOnly.Clear();
            this.TimeOfLastPathCalculation = 0;
            this.ForFactionIndex = -1;
            this.PathingModeUse = PathingMode.Length;
            this.RecalculatesOnThisIntervalOfSeconds = 3f;
        }
    }
    public enum PathingMode : byte
    {
        Default = 0,
        Safest,
        Shortest,
        SomeRandomRequest,
        CostToPlanet,
        Length
    }
}
