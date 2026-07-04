using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;


namespace Arcen.AIW2.ExternalVisualization
{
    public class SquadRenderManagerGroup : InstancedRendererBase
    {
        public readonly SquadDisplayType Type = SquadDisplayType.Unknown;
        private readonly ConcurrentPool<SquadVisualizer> SquadVisualizerPoolForRenderer;

        public static ReferenceTracker RefTracker;
        private SquadRenderManagerGroup( SquadDisplayType Type )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "RenderManagerGroup-Squad" );
            RefTracker.IncrementObjectCount();

            this.Type = Type;
            this.SquadVisualizerPoolForRenderer = new ConcurrentPool<SquadVisualizer>( "RenderManagerGroup-Squad-Pool-" + Type, 20000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SquadVisualizer( Type ); } )
                .ForcePrepopulateCount_UseThisWithGreatCare( Type == SquadDisplayType.Basic ? 4000 : 200 );            
        }

        #region static GetSquadInstanceRendererBySquadDisplayType
        private class SquadInstancedRendererPoolOfPools : CountedPoolBase
        {
            public SquadInstancedRendererPoolOfPools() : base( "SquadPoolOfPools" )
            {
                DoFullInit(); //there's nothing we can delay
            }

            protected override void DoFullInit()
            {
                InitIfNeeded();
            }

            protected override Int64 GetTotalNumberOfItemsInsideForTracing()
            {
                return FullListOfRenderers.Count;
            }
             
            protected override Int64 GetTotalCurrentCapacityForItemsForTracing()
            {
                return FullListOfRenderers.Capacity;
            }

            public override void WipeDuringXmlReload()
            {
                //nothing to do, it's okay!
            }
        }

        private static SquadInstancedRendererPoolOfPools squadPoolOfPools = new SquadInstancedRendererPoolOfPools();
        private static Dictionary<SquadDisplayType, SquadRenderManagerGroup> instanceRenderersByName = Dictionary<SquadDisplayType, SquadRenderManagerGroup>.Create_WillNeverBeGCed( 3000, "SquadRenderManagerGroup-instanceRenderersByName" );
        private static List<SquadRenderManagerGroup> FullListOfRenderers = List<SquadRenderManagerGroup>.Create_WillNeverBeGCed( 10000, "SquadRenderManagerGroup-FullListOfRenderers" );
        public static SquadRenderManagerGroup GetSquadInstanceRendererBySquadDisplayType( SquadDisplayType Type )
        {
            if ( instanceRenderersByName.ContainsKey( Type ) )
                return instanceRenderersByName[Type];

            SquadRenderManagerGroup rend = new SquadRenderManagerGroup( Type );
            instanceRenderersByName[Type] = rend;
            FullListOfRenderers.Add( rend );
            squadPoolOfPools.ItemsCreated++;
            return rend;
        }
        #endregion
                
        #region Get Out / Put Back for Pool
        public override IInstancedRenderer GetInstancedRendererFromPersonalPool( GameEntity_Base Entity )
        {
            if ( SquadVisualizerPoolForRenderer == null )
                return null;
            SquadVisualizer vis = SquadVisualizerPoolForRenderer.GetFromPoolOrCreate();
            if ( vis == null )
                return null;
            vis.Pool = this;
            //if ( vis.IsConsideredActive )
            //    SquadVisualizer.takeOutOfPoolActiveCount++;
            return vis;
        }

        public void PutBackInPoolRightAway( SquadVisualizer vis )
        {
            //if ( vis.IsConsideredActive )
            //    SquadVisualizer.putBackInPoolActiveCount++;
            SquadVisualizerPoolForRenderer.ReturnToPool( vis );
        }
        #endregion
    }
}
