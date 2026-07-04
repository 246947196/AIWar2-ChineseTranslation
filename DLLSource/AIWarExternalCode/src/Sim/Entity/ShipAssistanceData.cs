using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class ShipAssistanceData : ConcurrentPoolable<ShipAssistanceData>, IProtectedListable
    {
        public GameEntity_Squad Target;
        /// <summary>
        /// This is a copy of an existing flow, not a reference!
        /// </summary>
        public PlannedMetalFlow FlowReference;
        public int Distance;
        public bool IsInRange;

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ShipAssistanceDatas" );
        private ShipAssistanceData()
        {
            if ( RefTracker != null ) //it will be null for the two above in the static definitions
                RefTracker.IncrementObjectCount();
        }
        public ShipAssistanceData CreateNewForPool()
        {
            return new ShipAssistanceData();
        }

        private static readonly ConcurrentPool<ShipAssistanceData> Pool = new ConcurrentPool<ShipAssistanceData>( "ShipAssistanceData", 50000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ShipAssistanceData(); } );

        public static ShipAssistanceData GetFromPoolOrCreate( GameEntity_Squad Target, PlannedMetalFlow FlowReference, int Distance, bool IsInRange )
        {
            ShipAssistanceData assist = Pool.GetFromPoolOrCreate();
            assist.Target = Target;
            assist.FlowReference = FlowReference;
            assist.Distance = Distance;
            assist.IsInRange = IsInRange;
            return assist;
        }

        public void PutBackInPool()
        {
            Pool.ReturnToPool( this );
        }

        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.Target = null;
            this.FlowReference.Clear();
            this.Distance = 0;
            this.IsInRange = false;
        }
    }
}
