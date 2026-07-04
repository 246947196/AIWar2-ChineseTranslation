using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class PlanetPathfinderBasic : PlanetPathfinder, IRapidAntiLeakPoolable<PlanetPathfinderBasic>
    {
        #region Pooling
        private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "PlanetPathfinderBasic" );

        private PlanetPathfinderBasic()
        {
            if ( RefTracker != null )
                RefTracker.IncrementObjectCount();
            //ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPathfinderBasic const", Verbosity.DoNotShow );
        }

        public static readonly RapidAntiLeakPool<PlanetPathfinderBasic> Pool = 
            RapidAntiLeakPool<PlanetPathfinderBasic>.Create_WillNeverBeGCed( "PlanetPathfinderBasic", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlanetPathfinderBasic(); } );

        protected override void SubWipeForReuseAsNewObject()
        {
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        #region IRapidAntiLeakPoolable
        private bool IsInRapidPool = false;

        public bool GetInRapidAntiLeakPoolStatus()
        {
            return this.IsInRapidPool;
        }

        public void SetInRapidAntiLeakPoolStatus( bool InPool )
        {
            this.IsInRapidPool = InPool;
            this.RapidPoolExpirationTime = 0;
        }

        private string RapidPoolName = string.Empty;
        private float RapidPoolExpirationTime = 0;

        public void SetNameAndTimeAfterWhichToDeclareLeak( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            this.RapidPoolName = Name;
            this.RapidPoolExpirationTime = ArcenTime.TimeSinceStartF + SecondsAfterWhichToDeclareLeak;
        }

        public void RaiseErrorIfExpiredTimeForLeak()
        {
            if ( this.IsInRapidPool || this.RapidPoolExpirationTime <= 0 )
                return; //nothing to worry about here

            if ( this.RapidPoolExpirationTime > ArcenTime.TimeSinceStartF )
                return; //has not expired yet

            this.RapidPoolExpirationTime = 0; //prevent repeat error spam from one leak

            ArcenDebugging.ArcenDebugLogSingleLine( "Memory leak detected at RapidAntiLeakPoolable '" + this.RapidPoolName + "' of type " +
                this.GetType(), Verbosity.ShowAsError );
        }

        public void DoCleanupWhenComingOutOfRapidAntiLeakPool()
        {
            this.WipeForReuseAsNewObject();
        }
        #endregion
    }

    public sealed class PlanetPathfinderConservative : PlanetConservativePathfinderBase, IRapidAntiLeakPoolable<PlanetPathfinderConservative>
    {
        protected override StrengthData_PlanetFaction_Stance GetStanceData( Planet node, FactionStance stance )
        {
            return node.GetPlanetFactionForFaction( this.Faction ).DataByStance[stance];
        }

        #region Pooling
        private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "PlanetPathfinderConservative" );

        private PlanetPathfinderConservative()
        {
            if ( RefTracker != null )
                RefTracker.IncrementObjectCount();
            //ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPathfinderConservative const", Verbosity.DoNotShow );
        }

        public static readonly RapidAntiLeakPool<PlanetPathfinderConservative> Pool = 
            RapidAntiLeakPool<PlanetPathfinderConservative>.Create_WillNeverBeGCed( "PlanetPathfinderConservative", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart,
                PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlanetPathfinderConservative(); } );

        protected override void SubWipeForReuseAsNewObject()
        {
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        #region IRapidAntiLeakPoolable
        private bool IsInRapidPool = false;

        public bool GetInRapidAntiLeakPoolStatus()
        {
            return this.IsInRapidPool;
        }

        public void SetInRapidAntiLeakPoolStatus( bool InPool )
        {
            this.IsInRapidPool = InPool;
            this.RapidPoolExpirationTime = 0;
        }

        private string RapidPoolName = string.Empty;
        private float RapidPoolExpirationTime = 0;

        public void SetNameAndTimeAfterWhichToDeclareLeak( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            this.RapidPoolName = Name;
            this.RapidPoolExpirationTime = ArcenTime.TimeSinceStartF + SecondsAfterWhichToDeclareLeak;
        }

        public void RaiseErrorIfExpiredTimeForLeak()
        {
            if ( this.IsInRapidPool || this.RapidPoolExpirationTime <= 0 )
                return; //nothing to worry about here

            if ( this.RapidPoolExpirationTime > ArcenTime.TimeSinceStartF )
                return; //has not expired yet

            this.RapidPoolExpirationTime = 0; //prevent repeat error spam from one leak

            ArcenDebugging.ArcenDebugLogSingleLine( "Memory leak detected at RapidAntiLeakPoolable '" + this.RapidPoolName + "' of type " +
                this.GetType(), Verbosity.ShowAsError );
        }

        public void DoCleanupWhenComingOutOfRapidAntiLeakPool()
        {
            this.WipeForReuseAsNewObject();
        }
        #endregion
    }

    public sealed class PlanetPathfinderWarden : PlanetConservativePathfinderBase, IRapidAntiLeakPoolable<PlanetPathfinderWarden>
    {
        protected override StrengthData_PlanetFaction_Stance GetStanceData( Planet node, FactionStance stance )
        {
            if ( node == null )
                return StrengthData_PlanetFaction_Stance.DummyData;
            return node.GetDataByStanceForFaction( this.Faction, stance );
        }

        protected override NodePassability CalculateIsNodePassable_Slow( Planet node )
        {
            NodePassability result = base.CalculateIsNodePassable_Slow( node );
            if ( result > NodePassability.ONLY_PASSABLE_FOR_ORIGIN && node.GetIsEitherControllerOrInfluencerHostileTo( this.Faction ) )
                result = NodePassability.ONLY_PASSABLE_FOR_ORIGIN;
            return result;
        }

        protected override int HeuristicCostEstimate( Planet Origin, Planet Target )
        {
            StrengthData_PlanetFaction_Stance selfData = GetStanceData( Origin, FactionStance.Self );
            StrengthData_PlanetFaction_Stance friendlyData = GetStanceData( Origin, FactionStance.Friendly );
            StrengthData_PlanetFaction_Stance hostileData = GetStanceData( Origin, FactionStance.Hostile );
            int friendlyStrength = selfData.TotalStrength + friendlyData.TotalStrength;
            int hostileStrength = hostileData.TotalStrength;

            int uncounteredHostileStrength = Math.Max( 0, hostileStrength - friendlyStrength );

            return Origin.GetHopsTo( Target ) + uncounteredHostileStrength / 10;
        }

        #region Pooling
        private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "PlanetPathfinderWarden" );

        private PlanetPathfinderWarden()
        {
            if ( RefTracker != null )
                RefTracker.IncrementObjectCount();
            //ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPathfinderWarden const", Verbosity.DoNotShow );
        }

        public static readonly RapidAntiLeakPool<PlanetPathfinderWarden> Pool =
            RapidAntiLeakPool<PlanetPathfinderWarden>.Create_WillNeverBeGCed( "PlanetPathfinderWarden", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart,
                PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlanetPathfinderWarden(); } );

        protected override void SubWipeForReuseAsNewObject()
        {
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        #region IRapidAntiLeakPoolable
        private bool IsInRapidPool = false;

        public bool GetInRapidAntiLeakPoolStatus()
        {
            return this.IsInRapidPool;
        }

        public void SetInRapidAntiLeakPoolStatus( bool InPool )
        {
            this.IsInRapidPool = InPool;
            this.RapidPoolExpirationTime = 0;
        }

        private string RapidPoolName = string.Empty;
        private float RapidPoolExpirationTime = 0;

        public void SetNameAndTimeAfterWhichToDeclareLeak( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            this.RapidPoolName = Name;
            this.RapidPoolExpirationTime = ArcenTime.TimeSinceStartF + SecondsAfterWhichToDeclareLeak;
        }

        public void RaiseErrorIfExpiredTimeForLeak()
        {
            if ( this.IsInRapidPool || this.RapidPoolExpirationTime <= 0 )
                return; //nothing to worry about here

            if ( this.RapidPoolExpirationTime > ArcenTime.TimeSinceStartF )
                return; //has not expired yet

            this.RapidPoolExpirationTime = 0; //prevent repeat error spam from one leak

            ArcenDebugging.ArcenDebugLogSingleLine( "Memory leak detected at RapidAntiLeakPoolable '" + this.RapidPoolName + "' of type " +
                this.GetType(), Verbosity.ShowAsError );
        }

        public void DoCleanupWhenComingOutOfRapidAntiLeakPool()
        {
            this.WipeForReuseAsNewObject();
        }
        #endregion
    }
}
