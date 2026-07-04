using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;


namespace Arcen.AIW2.ExternalVisualization
{
    public class ShotRenderManagerGroup : InstancedRendererBase
    {
        public ArcenAssetBundlePath PathOfPrototype;
        private ArcenShot Prototype;
        private readonly ConcurrentPool<ShotVisualizer> shotVisualizerPoolForRenderer;
        private readonly CyclicalArrayPool<Matrix4x4> matrices = CyclicalArrayPool<Matrix4x4>.Create_WillNeverBeGCed( 1023, "ShotRenderManagerGroup-matrices" );
        private readonly CyclicalArrayPool<Matrix4x4> trailMatrices = CyclicalArrayPool<Matrix4x4>.Create_WillNeverBeGCed( 1023, "ShotRenderManagerGroup-trailMatrices" );

        private Mesh topmostMesh;
        private Material[] topmostMaterials;
        private int topmostMaterialCount;

        private Mesh trailMesh;
        private Material[] trailMaterials;
        private int trailMaterialCount;
        public readonly float PrototypeScale;

        public readonly bool HasTop;
        public readonly bool HasTrail;
        public readonly bool IsInvalid;

        public static ReferenceTracker RefTracker;
        private ShotRenderManagerGroup( string UniqueName, ArcenAssetBundlePath PathOfPrototype, IArcenGameObjectResourcePoolable Prototype )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "RenderManagerGroup-Shot" );
            RefTracker.IncrementObjectCount();

            this.PathOfPrototype = PathOfPrototype;
            this.Prototype = (ArcenShot)Prototype;
            this.PrototypeScale = this.Prototype.transform.localScale.x;
            this.shotVisualizerPoolForRenderer = new ConcurrentPool<ShotVisualizer>( "RenderManagerGroup-Shot-Pool_" + UniqueName, 20000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ShotVisualizer(); } ).ForcePrepopulateCount_UseThisWithGreatCare( 100 );

            MeshFilter topFilter = this.Prototype.GetComponent<MeshFilter>();
            this.HasTop = ( topFilter != null );

            if ( this.HasTop )
            {
                this.topmostMesh = topFilter.sharedMesh;
                this.topmostMaterials = this.Prototype.GetComponent<MeshRenderer>().sharedMaterials;
                this.topmostMaterialCount = this.topmostMaterials.Length;

                #region Validate Mats
                for ( int j = 0; j < topmostMaterialCount; j++ )
                {
                    Material mat = topmostMaterials[j];
                    if ( !mat )
                        ArcenDebugging.ArcenDebugLog( "Missing material on renderer for shot: " + this.Prototype.name, Verbosity.ShowAsError );
                    else
                    {
                        if ( !mat.enableInstancing )
                            ArcenDebugging.ArcenDebugLog( "Instancing not enabled on material " + mat.name + " for shot: " + this.Prototype.name, Verbosity.ShowAsError );
                    }
                }
                #endregion
            }

            this.HasTrail = ( this.Prototype.TrailObject != null );

            if ( this.HasTrail )
            {
                this.trailMesh = this.Prototype.TrailObject.GetComponent<MeshFilter>().sharedMesh;
                this.trailMaterials = this.Prototype.TrailObject.GetComponent<MeshRenderer>().sharedMaterials;
                this.trailMaterialCount = this.trailMaterials.Length;

                #region Validate Mats
                for ( int j = 0; j < trailMaterialCount; j++ )
                {
                    Material mat = trailMaterials[j];
                    if ( !mat )
                        ArcenDebugging.ArcenDebugLog( "Missing material on renderer for shot: " + this.Prototype.name, Verbosity.ShowAsError );
                    else
                    {
                        if ( !mat.enableInstancing )
                            ArcenDebugging.ArcenDebugLog( "Instancing not enabled on material " + mat.name + " for shot: " + this.Prototype.name, Verbosity.ShowAsError );
                    }
                }
                #endregion
            }

            if ( !this.HasTop && !this.HasTrail )
            {
                this.IsInvalid = true;
                return;
            }
        }

        #region Render
        private static MaterialPropertyBlock emptyBlock = new MaterialPropertyBlock();
        public void Render()
        {
            if ( this.IsInvalid )
                return;
            Matrix4x4[] array;
            int arrayLength;

            if ( this.HasTop )
            {
                //Debug.Log( "attempt " + this.Prototype.name );
                for ( int unused = 0; unused < 100; unused++ )
                {
                    array = matrices.ReadNextArray( out arrayLength );
                    if ( arrayLength <= 0 || array == null )
                        break;

                    for ( int i = 0; i < this.topmostMaterialCount; i++ )
                    {
                        Graphics.DrawMeshInstanced( this.topmostMesh, i, this.topmostMaterials[i], array, arrayLength,
                            emptyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, 13, //shots layer
                            ArcenMainGameVisuals.MainViewCamera, UnityEngine.Rendering.LightProbeUsage.Off );
                        //Debug.Log( this.Prototype.name + " " + unused + " " + i + " " + array.Length + " " + arrayLength );
                    }
                }
            }

            if ( this.HasTrail )
            {
                for ( int unused = 0; unused < 100; unused++ )
                {
                    array = trailMatrices.ReadNextArray( out arrayLength );
                    if ( arrayLength <= 0 || array == null )
                        break;

                    for ( int i = 0; i < this.trailMaterialCount; i++ )
                        Graphics.DrawMeshInstanced( this.trailMesh, i, this.trailMaterials[i], array, arrayLength, 
                            emptyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, 13, //shots layer
                            ArcenMainGameVisuals.MainViewCamera, UnityEngine.Rendering.LightProbeUsage.Off ); 
                }
            }
            
            matrices.Reset();
            trailMatrices.Reset();

            this.HasBeenMarkedAsToRenderForThisFrame = false;
        }
        #endregion

        #region static GetShotInstanceRendererByName
        private class ShotInstancedRendererPoolOfPools : CountedPoolBase
        {
            public ShotInstancedRendererPoolOfPools() : base( "ShotPoolOfPools" )
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

        private static ShotInstancedRendererPoolOfPools shotPoolOfPools = new ShotInstancedRendererPoolOfPools();
        private static readonly Dictionary<string, ShotRenderManagerGroup> instanceRenderersByName = Dictionary<string, ShotRenderManagerGroup>.Create_WillNeverBeGCed( 7000, "ShotRenderManagerGroup-instanceRenderersByName" );
        private static readonly List<ShotRenderManagerGroup> FullListOfRenderers = List<ShotRenderManagerGroup>.Create_WillNeverBeGCed( 400, "ShotRenderManagerGroup-FullListOfRenderers" );
        public static ShotRenderManagerGroup GetShotInstanceRendererByName( string UniqueName, ArcenAssetBundlePath PathOfPrototype, IArcenGameObjectResourcePoolable Prototype )
        {
            string name = PathOfPrototype.CombinedPath;
            lock ( instanceRenderersByName )
            {
                if ( instanceRenderersByName.ContainsKey( name ) )
                    return instanceRenderersByName[name];

                ShotRenderManagerGroup rend = new ShotRenderManagerGroup( UniqueName, PathOfPrototype, Prototype );
                instanceRenderersByName[name] = rend;
                FullListOfRenderers.Add( rend );
                shotPoolOfPools.ItemsCreated++;
                return rend;
            }
        }
        #endregion

        #region static RenderAll
        private static readonly List<ShotRenderManagerGroup> ToRenderThisFrame = List<ShotRenderManagerGroup>.Create_WillNeverBeGCed( 400, "ShotRenderManagerGroup-ToRenderThisFrame" );
        public static void RenderAll()
        {
            for ( int i = 0; i < ToRenderThisFrame.Count; i++ )
                ToRenderThisFrame[i].Render();

            ToRenderThisFrame.Clear();
        }
        #endregion
        
        #region Get Out / Put Back for Pool
        public override IInstancedRenderer GetInstancedRendererFromPersonalPool( GameEntity_Base Entity )
        {
            if ( shotVisualizerPoolForRenderer == null )
                return null;
            ShotVisualizer vis = shotVisualizerPoolForRenderer.GetFromPoolOrCreate();
            if ( vis == null )
                return null;
            vis.Pool = this;
            return vis;
        }

        public void PutBackInPoolRightAway( ShotVisualizer vis )
        {
            shotVisualizerPoolForRenderer.ReturnToPool( vis );
        }
        #endregion

        #region WriteToDrawBufferForOneFrame
        private bool HasBeenMarkedAsToRenderForThisFrame = false;
        public void WriteToDrawBufferForOneFrame( System.Numerics.Vector3 Position, UnityEngine.Quaternion Rotation, float TopmostScale, float TrailZScale )
        {
            if ( this.IsInvalid )
            {
                //ArcenDebugging.ArcenDebugLog( "Tried to write to invalid shot! " + this.Prototype.name, Verbosity.ShowAsError );
                return;
            }
            Vector3 pos = Position.ToUnityVector3();

            if ( this.HasTrail )
            {
                Matrix4x4 matrix = Matrix4x4.TRS( pos, Rotation, new Vector3( TopmostScale, TopmostScale, TopmostScale * TrailZScale ) );
                trailMatrices.Add( matrix );
            }
            if ( this.HasTop )
            {
                Matrix4x4 matrix = Matrix4x4.TRS( pos, Rotation, new Vector3( TopmostScale, TopmostScale, TopmostScale ) );
                matrices.Add( matrix );
            }

            if ( !this.HasBeenMarkedAsToRenderForThisFrame )
            {
                this.HasBeenMarkedAsToRenderForThisFrame = true;
                ToRenderThisFrame.Add( this );
            }
        }
        #endregion
    }
}
