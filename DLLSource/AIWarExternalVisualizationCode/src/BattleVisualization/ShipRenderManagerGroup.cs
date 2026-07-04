using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;


namespace Arcen.AIW2.ExternalVisualization
{
    public class ShipRenderManagerGroup : InstancedRendererBase
    {
        public ArcenAssetBundlePath PathOfPrototype;
        private ArcenVisualSolomeshShip Prototype;

        public static readonly ConcurrentPool<ShipVisualizer> shipVisualizerPool = new ConcurrentPool<ShipVisualizer>( "RenderManagerGroup-Ship-Pool", 90000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ShipVisualizer(); } ).ForcePrepopulateCount_UseThisWithGreatCare( 8000 );

        public readonly float PrototypeScale;
        public readonly float[] LODDistances;
        private Mesh[] lodMeshes;
        private Material[] soloMaterials;
        private int soloMaterialCount;
        private GameEntityTypeData FirstRelatedEntityType;
        private RenderStatusRenderer[] statusRenderers = new RenderStatusRenderer[(int)ShipRenderStatus.Length];

        public readonly bool IsInvalid;

        public static ReferenceTracker RefTracker;
        private ShipRenderManagerGroup( GameEntityTypeData FirstRelatedEntityType, ArcenAssetBundlePath PathOfPrototype, IArcenGameObjectResourcePoolable Prototype )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "RenderManagerGroup-Ship" );
            RefTracker.IncrementObjectCount();

            this.FirstRelatedEntityType = FirstRelatedEntityType;
            this.PathOfPrototype = PathOfPrototype;
            this.Prototype = (ArcenVisualSolomeshShip)Prototype;
            this.PrototypeScale = this.Prototype.transform.localScale.x;
            
            this.lodMeshes = this.Prototype.MeshesPerLOD;
            this.LODDistances = this.Prototype.LODDistances;

            for(int i = 0; i < lodMeshes.Length; i++)
            {
                var mesh = lodMeshes[i];
                if (mesh == null)
                {
                    LOG.Msg("WARNING: {0} lodMeshes[{1}] = null", FirstRelatedEntityType.InternalName, i);
                }
                else
                {
                    //LOG.Msg("{0} lodMeshes[{1}] = {2}", FirstRelatedEntityType.InternalName, i, mesh.name);
                }
            }
            
            this.soloMaterials = this.Prototype.GetComponent<MeshRenderer>().sharedMaterials;
            this.soloMaterialCount = this.soloMaterials.Length;

            if (FirstRelatedEntityType != null)
            {
                if (FirstRelatedEntityType.OverrideMaterial != null )
                {
                    for ( int i = 0; i < soloMaterialCount; i++)
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Assigning override material {0} to slot {1} in place of {2}", FirstRelatedEntityType.OverrideMaterial.name, i, soloMaterials[i].name), Verbosity.DoNotShow);
                        soloMaterials[i] = FirstRelatedEntityType.OverrideMaterial;
                    }
                }
            }

            #region Validate Mats
            for ( int j = 0; j < soloMaterialCount; j++ )
            {
                Material mat = soloMaterials[j];
                if ( !mat )
                    ArcenDebugging.ArcenDebugLog( "Missing material on renderer for ship: " + this.Prototype.name, Verbosity.ShowAsError );
                else
                {
                    if ( !mat.enableInstancing )
                        ArcenDebugging.ArcenDebugLog( "Instancing not enabled on material " + mat.name + " for ship: " + this.Prototype.name, Verbosity.ShowAsError );
                }
            }
            #endregion

            for ( ShipRenderStatus rend = ShipRenderStatus.Normal; rend < ShipRenderStatus.Length; rend++ )
                statusRenderers[(int)rend] = new RenderStatusRenderer( this.LODDistances.Length, rend, this );
        }

        #region Render
        public void Render()
        {
            if ( this.IsInvalid )
                return;

            for ( int i = 0; i < statusRenderers.Length; i++ )
                statusRenderers[i].Render();

            this.HasBeenMarkedAsToRenderForThisFrame = false;
        }
        #endregion

        #region static GetShipInstanceRendererByName
        private class ShipInstancedRendererPoolOfPools : CountedPoolBase
        {
            public ShipInstancedRendererPoolOfPools() : base( "ShipPoolOfPools" )
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

        private static ShipInstancedRendererPoolOfPools shipPoolOfPools = new ShipInstancedRendererPoolOfPools();

        private static Dictionary<string, ShipRenderManagerGroup> instanceRenderersByName = Dictionary<string, ShipRenderManagerGroup>.Create_WillNeverBeGCed( 3000, "ShipRenderManagerGroup-instanceRenderersByName" );
        private static List<ShipRenderManagerGroup> FullListOfRenderers = List<ShipRenderManagerGroup>.Create_WillNeverBeGCed( 10000, "ShipRenderManagerGroup-FullListOfRenderers" );
        public static ShipRenderManagerGroup GetShipInstanceRendererByName( GameEntityTypeData CurrentRelatedEntityType, ArcenAssetBundlePath PathOfPrototype, IArcenGameObjectResourcePoolable Prototype )
        {
            string name = PathOfPrototype.CombinedPath;
            
            var overrideMatPath = CurrentRelatedEntityType.OverrideMaterialPath.PathInBundle;
            if (!string.IsNullOrEmpty(overrideMatPath))
            {
                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("GetShipInstanceRendererByName had an overridepath {0}", overrideMatPath), Verbosity.DoNotShow);
                name += overrideMatPath;
            }

            lock ( instanceRenderersByName )
            {
                if ( instanceRenderersByName.ContainsKey( name ) )
                {
                    ShipRenderManagerGroup rendExisting = instanceRenderersByName[name];
                    CurrentRelatedEntityType.LODDistancesFromVis = rendExisting.LODDistances;
                    CurrentRelatedEntityType.LODMeshesFromVis = rendExisting.lodMeshes;
                    return rendExisting;
                }

                ShipRenderManagerGroup rend = new ShipRenderManagerGroup( CurrentRelatedEntityType, PathOfPrototype, Prototype );
                CurrentRelatedEntityType.LODDistancesFromVis = rend.LODDistances;
                CurrentRelatedEntityType.LODMeshesFromVis = rend.lodMeshes;
                instanceRenderersByName[name] = rend;
                FullListOfRenderers.Add( rend );
                shipPoolOfPools.ItemsCreated++;
                return rend;
            }
        }
        #endregion
        
        #region static RenderAll
        private static List<ShipRenderManagerGroup> ToRenderThisFrame = List<ShipRenderManagerGroup>.Create_WillNeverBeGCed( 4000, "ShipRenderManagerGroup-ToRenderThisFrame" );

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
            ShipVisualizer vis = shipVisualizerPool.GetFromPoolOrCreate();
            if ( vis == null )
                return null;
            vis.RenderGroup = this;
            return vis;
        }

        public static void PutBackInPoolRightAway( ShipVisualizer vis )
        {
            shipVisualizerPool.ReturnToPool( vis );
        }
        #endregion

        #region WriteToDrawBufferForOneFrame
        private bool HasBeenMarkedAsToRenderForThisFrame = false;
        public void WriteToDrawBufferForOneFrame( Vector3 Position, UnityEngine.Quaternion Rotation, float Scale, int LOD, ShipRenderStatus RenderStatus, 
            GameEntity_Base ThisEntityIsBeingHovered, float RelatedInstancedFloat )
        {
            if ( this.IsInvalid )
            {
                ArcenDebugging.ArcenDebugLog( "Tried to write to invalid ship! " + this.Prototype.name, Verbosity.ShowAsError );
                return;
            }

            //if ( ThisEntityIsBeingHovered != null )
            //    ThisEntityIsBeingHovered.DebugText = "LOD:" + LOD + " Scale: " + Scale;

            Matrix4x4 matrix = Matrix4x4.TRS( Position, Rotation, new Vector3( Scale, Scale, Scale ) );
            statusRenderers[(int)RenderStatus].WriteToDrawBufferForOneFrame( matrix, LOD, ThisEntityIsBeingHovered, RelatedInstancedFloat );

            if ( !this.HasBeenMarkedAsToRenderForThisFrame )
            {
                this.HasBeenMarkedAsToRenderForThisFrame = true;
                ToRenderThisFrame.Add( this );
            }
        }
        #endregion
        
        private class RenderStatusRenderer
        {
            private CyclicalArrayPool<Matrix4x4>[] matricesByLOD;
            private CyclicalArrayPool<float>[] relatedFloatsByLOD;
            private ShipRenderStatus RenderStatus;
            private ShipRenderManagerGroup Rend;

            private Material[] soloMaterials;
            private int soloMaterialCount;

            #region RenderStatusRenderer Constructor
            private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "RenderStatusRenderers" );
            public RenderStatusRenderer( int LODCount, ShipRenderStatus RenderStatus, ShipRenderManagerGroup Rend )
            {
                if ( RefTracker != null ) //it will be null for the two above in the static definitions
                    RefTracker.IncrementObjectCount();

                this.RenderStatus = RenderStatus;
                this.Rend = Rend;

                int maxBatchSize = 1023;
                switch ( this.RenderStatus )
                {
                    case ShipRenderStatus.UnderConstruction:
                    case ShipRenderStatus.UnderConstructionStalled:
                    case ShipRenderStatus.ShipRemains:
                    case ShipRenderStatus.Phase_Multi_Still:
                    case ShipRenderStatus.Phase_Multi_VertexAnim:
                    case ShipRenderStatus.Phase_Solo_Still:
                    case ShipRenderStatus.Phase_Solo_VertexAnim:
                        maxBatchSize = 1;
                        break;
                }
                switch ( this.RenderStatus )
                {
                    case ShipRenderStatus.BurningAndDyingLastDeath:
                    case ShipRenderStatus.BurningAndDyingNonLastDeath:
                        relatedFloatsByLOD = new CyclicalArrayPool<float>[LODCount];
                        for ( int i = 0; i < LODCount; i++ )
                            relatedFloatsByLOD[i] = CyclicalArrayPool<float>.Create_WillNeverBeGCed( maxBatchSize, "ShipRenderManagerGroup-relatedFloatsByLOD" );
                        break;
                }

                matricesByLOD = new CyclicalArrayPool<Matrix4x4>[LODCount];
                for ( int i = 0; i < LODCount; i++ )
                    matricesByLOD[i] = CyclicalArrayPool<Matrix4x4>.Create_WillNeverBeGCed( maxBatchSize, "ShipRenderManagerGroup-matricesByLOD" );

                soloMaterialCount = Rend.soloMaterialCount;
            }
            #endregion

            #region InitMaterialsIfNeeded
            private bool hasInit = false;
            private void InitMaterialsIfNeeded()
            {
                if ( hasInit )
                    return;
                hasInit = true;

                switch ( this.RenderStatus )
                {
                    case ShipRenderStatus.Normal:
                        soloMaterials = Rend.soloMaterials;
                        break;
                    case ShipRenderStatus.AntiSpawn:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipSpawnMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipSpawnMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipSpawnMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.Spawn:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipSpawnMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipSpawnMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipSpawnMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.UnderConstruction:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipUnderConstructionMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipUnderConstructionMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipUnderConstructionMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.UnderConstructionStalled:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipUnderConstructionStalledMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipUnderConstructionStalledMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipUnderConstructionStalledMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.BurningAndDyingLastDeath:
                        this.WriteSingleMaterialIntoAllMaterialSlots( this.Rend.FirstRelatedEntityType.ShipLastDeathMaterial );
                        if ( !this.Rend.FirstRelatedEntityType.ShipLastDeathMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipLastDeathMaterial on " + this.Rend.FirstRelatedEntityType.InternalName + "!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.BurningAndDyingNonLastDeath:
                        this.WriteSingleMaterialIntoAllMaterialSlots( this.Rend.FirstRelatedEntityType.ShipNonLastDeathMaterial );
                        if ( !this.Rend.FirstRelatedEntityType.ShipNonLastDeathMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipNonLastDeathMaterial on " + this.Rend.FirstRelatedEntityType.InternalName + "!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.ShipRemains:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipRemainsMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipRemainsMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipRemainsMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.WarpingIn:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipSpawnMaterial );
                        if ( !ArcenVisualOrganizer.Instance.ShipSpawnMaterial )
                            ArcenDebugging.ArcenDebugLog( "Null ShipSpawnMaterial!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.Phase_Solo_Still:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipSoloPhasingMaterial_Still );
                        if ( !ArcenVisualOrganizer.Instance.ShipSoloPhasingMaterial_Still )
                            ArcenDebugging.ArcenDebugLog( "Null ShipSoloPhasingMaterial_Still!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.Phase_Solo_VertexAnim:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipSoloPhasingMaterial_VertexAnimated );
                        if ( !ArcenVisualOrganizer.Instance.ShipSoloPhasingMaterial_VertexAnimated )
                            ArcenDebugging.ArcenDebugLog( "Null ShipSoloPhasingMaterial_VertexAnimated!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.Phase_Multi_Still:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipMultiPhasingMaterial_Still );
                        if ( !ArcenVisualOrganizer.Instance.ShipMultiPhasingMaterial_Still )
                            ArcenDebugging.ArcenDebugLog( "Null ShipMultiPhasingMaterial_Still!", Verbosity.ShowAsError );
                        break;
                    case ShipRenderStatus.Phase_Multi_VertexAnim:
                        this.WriteSingleMaterialIntoAllMaterialSlots( ArcenVisualOrganizer.Instance.ShipMultiPhasingMaterial_VertexAnimated );
                        if ( !ArcenVisualOrganizer.Instance.ShipMultiPhasingMaterial_VertexAnimated )
                            ArcenDebugging.ArcenDebugLog( "Null ShipMultiPhasingMaterial_VertexAnimated!", Verbosity.ShowAsError );
                        break;
                    default:
                        ArcenDebugging.ArcenDebugLog( "Nothing set up for ShipRenderStatus" + this.RenderStatus + "!", Verbosity.ShowAsError );
                        break;
                }
            }

            private void WriteSingleMaterialIntoAllMaterialSlots( Material mat )
            {
                soloMaterials = new Material[this.soloMaterialCount];
                for ( int i = 0; i < this.soloMaterialCount; i++ )
                    this.soloMaterials[i] = mat;
            }
            #endregion

            public GameEntity_Base EntityToWriteDebugInfoFor;

            private bool HasBeenMarkedAsToRenderForThisFrame = false;
            public void WriteToDrawBufferForOneFrame( Matrix4x4 matrix, int LOD, GameEntity_Base ThisEntityIsBeingHovered, float RelatedInstancedFloat )
            {
                if ( ThisEntityIsBeingHovered != null )
                    EntityToWriteDebugInfoFor = ThisEntityIsBeingHovered;
                //if ( ThisEntityIsBeingHovered != null )
                //    ThisEntityIsBeingHovered.DebugText += " try";

                if ( LOD >= matricesByLOD.Length )
                    return; //intentional culling -- that's what these high LODs mean!

                this.HasBeenMarkedAsToRenderForThisFrame = true;
                matricesByLOD[LOD].Add( matrix );

                if ( relatedFloatsByLOD != null )
                    relatedFloatsByLOD[LOD].Add( RelatedInstancedFloat );
                
                //if ( ThisEntityIsBeingHovered != null )
                //    ThisEntityIsBeingHovered.DebugText += " added!";
            }

            private static Dictionary<string, bool> loggedErrorsByMaterialName = Dictionary<string, bool>.Create_WillNeverBeGCed( 5000, "ShipRenderManagerGroup-loggedErrorsByMaterialName" );

            #region Render
            private static MaterialPropertyBlock emptyBlock = new MaterialPropertyBlock();
            private static MaterialPropertyBlock reusableBlock = new MaterialPropertyBlock();
            public void Render()
            {
                //if ( EntityToWriteDebugInfoFor != null )
                //    EntityToWriteDebugInfoFor.DebugText = " tryrend ";

                if ( !this.HasBeenMarkedAsToRenderForThisFrame )
                    return;

                InitMaterialsIfNeeded();

                //if ( EntityToWriteDebugInfoFor != null )
                //    EntityToWriteDebugInfoFor.DebugText += " rend";

                Matrix4x4[] matrixArray;
                int matrixArrayLength;
                float[] floatArray;
                int floatArrayLength;
                CyclicalArrayPool<Matrix4x4> matrices;
                MaterialPropertyBlock blockToUse;
                for ( int lod = 0; lod < this.matricesByLOD.Length; lod++ )
                {
                    matrices = this.matricesByLOD[lod];
                    //Debug.Log( "attempt " + this.Prototype.name );
                    
                    for ( int unused = 0; unused < 100; unused++ )
                    {
                        matrixArray = matrices.ReadNextArray( out matrixArrayLength );

                        //if ( EntityToWriteDebugInfoFor != null )
                        //    EntityToWriteDebugInfoFor.DebugText += " lod" + lod + "/" + arrayLength;
                        if ( matrixArrayLength <= 0 || matrixArray == null )
                            break;

                        if ( matrixArrayLength > 1023 )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "matrixArrayLength of " + matrixArrayLength + ", which is more than 1023, so not allowed...", Verbosity.ShowAsError );
                            continue;
                        }

                        if ( this.relatedFloatsByLOD == null )
                            blockToUse = emptyBlock;
                        else
                        {
                            reusableBlock.Clear();
                            floatArray = this.relatedFloatsByLOD[lod].ReadNextArray( out floatArrayLength );
                            if ( floatArrayLength > 0 && floatArray != null )
                            {
                                switch ( this.RenderStatus )
                                {
                                    case ShipRenderStatus.BurningAndDyingLastDeath:
                                    case ShipRenderStatus.BurningAndDyingNonLastDeath:
                                        reusableBlock.SetFloatArray( ArcenVisualOrganizer.Instance.ShipDeathProgressShaderPropertyID, floatArray );
                                        break;
                                }
                            }
                            blockToUse = reusableBlock;
                        }

                        var mesh = this.Rend.lodMeshes[lod];
                        if (mesh == null)
                            continue;
                        
                        for ( int i = 0; i < this.soloMaterialCount; i++ )
                        {
                            try
                            {
                                Graphics.DrawMeshInstanced( mesh, i, this.soloMaterials[i], matrixArray, matrixArrayLength,
                                    blockToUse, UnityEngine.Rendering.ShadowCastingMode.Off, false, 17, //SelectableShip layer
                                    ArcenMainGameVisuals.MainViewCamera, UnityEngine.Rendering.LightProbeUsage.Off );
                            }
                            catch ( Exception e )
                            {
                                string name = this.soloMaterials[i].name;
                                if ( !loggedErrorsByMaterialName.ContainsKey( name ) )
                                {
                                    loggedErrorsByMaterialName[name] = true;
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception for material '" + name + "': " + e, Verbosity.ShowAsError );
                                }

                            }
                            //Debug.Log( this.Prototype.name + " " + unused + " " + i + " " + array.Length + " " + arrayLength );
                        }
                    }
                    
                    matrices.Reset();
                    
                    if ( this.relatedFloatsByLOD != null )
                        this.relatedFloatsByLOD[lod].Reset();
                }

                this.HasBeenMarkedAsToRenderForThisFrame = false;
            }
            #endregion
        }
    }
}
