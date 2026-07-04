using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;

using Arcen.Universal.Sprites;
using MeshSprites = Arcen.Universal.Sprites.MeshBased;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GenericObjectVisualizer : IArcenGenericObjectVisualizer, IRelatedEntity
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            this.DeactivateAndReturnToPool();
        }

        public ArcenVisualGenericObject UnityObject;
        public ArcenGameObjectResourcePool Pool;
        public GameEntity_Other RelatedEntity;
        public GameEntityTypeData RelatedTypeData;
        private ArcenPoint PriorLoc = ArcenPoint.OutOfRange;
        private Vector2 LastPos = Vector2.zero;
        private ArcenVisualOrganizer VisualDict;
        public float TimeUntilIDieNoMatterWhat = -1f;
        public bool ToBeRemoved;
        public Transform selfT;

        public static Int64 NumberCreated = 0;
        public Int64 MyID;

        private static ReferenceTracker RefTracker;
        public GenericObjectVisualizer()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "GenericObjectVisualizers" );
            RefTracker.IncrementObjectCount();
            MyID = System.Threading.Interlocked.Add( ref NumberCreated, 1 );
                    }

        private bool didInit;
        public void Init( MonoBehaviour unityObject )
        {
            if ( !this.UnityObject && (ArcenVisualGenericObject)unityObject )
            {
                this.UnityObject = (ArcenVisualGenericObject)unityObject;
                //ArcenDebugging.ArcenDebugLog( "Create wormhole: " + this.MyID + " active: " + this.UnityObject.gameObject.activeSelf, Verbosity.DoNotShow );
            }

            if ( this.didInit )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( MyID + " Skip GenericObjectVisualizer init!  Already did it? selfT: " + (selfT != null ), Verbosity.DoNotShow );
                return;
            }

            if ( !this.VisualDict )
                this.VisualDict = ArcenVisualOrganizer.Instance;
            if ( !this.VisualDict )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( MyID + " Error during GenericObjectVisualizer init!  ArcenVisualOrganizer.Instance is null.", Verbosity.ShowAsError );
                return;
            }
            this.selfT = this.UnityObject.transform;
            this.selfT.parent = this.VisualDict.MainGameObjectParent;

            this.didInit = true;
            this.RelatedTypeData = null;
            this.RelatedEntity = null;
        }

        public bool IsConsideredActive;
        public void Activate( ArcenGameObjectResourcePool fromPool, GameEntity_Base RelatedTo, GameEntityTypeData RelatedType )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( this.IsConsideredActive )
                return;
            this.IsConsideredActive = true;
            int debugStage = 0;
            try
            {
                debugStage = 100;
                this.UnityObject.gameObject.SetActive( true );
                //ArcenDebugging.ArcenDebugLog( "Activate wormhole: " + this.MyID + " active: " + this.UnityObject.gameObject.activeSelf, Verbosity.DoNotShow );
                debugStage = 200;
                this.PriorLoc = ArcenPoint.OutOfRange;
                debugStage = 300;
                this.RelatedEntity = (GameEntity_Other)RelatedTo;
                debugStage = 400;
                this.RelatedTypeData = RelatedType;
                debugStage = 500;
                if ( this.RelatedTypeData != null )
                {
                    debugStage = 600;
                    this.UnityObject.name = this.RelatedTypeData.InternalName;
                }

                debugStage = 700;
                //this.lastScale_Main = -1f;
                this.lastScale_Gimbal = -1f;
                debugStage = 800;
                this.Pool = fromPool;

                debugStage = 900;
                GameEntity_Other relatedEnt = this.RelatedEntity;
                if ( relatedEnt != null )
                {
                    Vector3 newPos = relatedEnt.WorldLocation.ToVisualMainGameCoordinates_Unity( relatedEnt.Planet );
                    debugStage = 1000;
                    this.selfT.localPosition = newPos;
                }

                debugStage = 1100;
                BattlefieldVisualSingleton.Instance.LooseOtherObjects.Add( this );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( MyID + " Error Activating GenericObjectVisualizer debugStage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }

        public void DeactivateAndReturnToPool()
        {
            //ArcenDebugging.ArcenDebugLog( "DeactivateAndReturnToPool wormhole: " + this.MyID + " active: " + this.UnityObject.gameObject.activeSelf, Verbosity.DoNotShow );

            ArcenDebugging.ErrorIfNotMainThread();

            if ( !this.IsConsideredActive )
                return;
            this.IsConsideredActive = false;

            if ( this.RelatedEntity != null )
                this.RelatedEntity.ClearVisualObjIfExists( true, false, InstancedRendererDeactivationReason.Unknown );

            this.UnityObject.gameObject.SetActive( false );
            this.RelatedEntity = null;
            this.RelatedTypeData = null;
            this.ToBeRemoved = false;
            this.LastPos = Vector2.zero;
            //this.lastScale_Main = -1f;
            this.lastScale_Gimbal = -1f;

            this.Pool.PutBackInPoolRightAway( this.UnityObject );
            this.Pool = null;

            BattlefieldVisualSingleton.Instance.LooseOtherObjects.Add( this );
        }

        public GameObject GetGameObject()
        {
            return this.UnityObject.gameObject;
        }

        public bool GetIsToUseSharedPool()
        {
            return false;
        }

        public void DoFancySpawnAnimation()
        {
            this.DoFancySpawnAnimation();
        }
        public void DoFancyDespawnAnimation()
        {
            this.DoFancySpawnAnimation();
        }

        public void DetachVisObjectFromSim()
        {
            this.FlagForRemoval(true);
        }

        public void FlagForRemoval( bool ForceRemoval )
        {
            if ( this.ToBeRemoved )
                return;
            this.ToBeRemoved = true;
            BattlefieldVisualSingleton.Instance.VisualObjectRemovalRequests.Enqueue( this.UnityObject );
        }

        #region SetLocationToMatchSim
        private void SetLocationToMatchSim()
        {
            GameEntity_Other relatedEnt = this.RelatedEntity;
            if ( relatedEnt == null || relatedEnt.HasBeenRemovedFromSim )
                return;

            ArcenPoint simLoc = relatedEnt.WorldLocation;
            if ( simLoc == ArcenPoint.OutOfRange || simLoc == ArcenPoint.ZeroZeroPoint || simLoc == this.PriorLoc )
                return;

            this.PriorLoc = simLoc;
            this.selfT.localPosition = simLoc.ToVisualMainGameCoordinates_Unity( relatedEnt.Planet );
        }
        #endregion

        //public Vector3 GetPosition()
        //{
        //    return this.selfT.position;
        //}

        public GameEntity_Base GetEntityRelatedTo()
        {
            return this.RelatedEntity;
        }

        public void UpdateObject( int CurrentSimFrameVisualOnly, BattlefieldVisualSingleton MainVis, float DeltaTime, float WormholeGimbalScaleMultiplier )
        {
            if ( this.ToBeRemoved )
                return;

            if ( this.TimeUntilIDieNoMatterWhat > 0 )
            {
                this.TimeUntilIDieNoMatterWhat -= DeltaTime;
                if ( this.TimeUntilIDieNoMatterWhat <= 0 )
                {
                    this.FlagForRemoval( true );
                    return;
                }
            }

            GameEntity_Other relatedEnt = this.RelatedEntity;

            if ( relatedEnt == null || !relatedEnt.IsConsideredActive )
            {
                this.FlagForRemoval( false );
                return;
            }

            int lastFrame = relatedEnt.LastSimFrameToldToHaveVisualObject;
            if ( lastFrame > CurrentSimFrameVisualOnly || CurrentSimFrameVisualOnly > lastFrame + 3 ) //|| relatedEnt.HasBeenRemovedFromSim
            {
                this.FlagForRemoval( false );
                if ( this.ToBeRemoved )
                    return;
            }

            this.SetLocationToMatchSim();

            switch ( this.UnityObject.ObjectType )
            {
                case VisualObjectType.Wormhole:
                    this.UpdateWormhole( MainVis, WormholeGimbalScaleMultiplier );
                    break;
            }

            if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.Debug_DrawAllRadiiAtAllTimes ) )
            {
                if ( !ArcenUI.Instance.InHideGUIMode )
                    FrontEndLink.Instance.IMDraw_WireDisc3D( this.selfT.position, Mat.Quater_Ident,
                        relatedEnt.TypeData.BaseMark.CalculateRadiusForDisplay( relatedEnt.Planet ), IMDrawAxis.Y, Color.yellow );
            }
        }

        //private float lastScale_Main = -1f;

        #region ShowOrHideRenderers
        private Renderer[] allRenderers;
        private bool wereRenderersLastShown = true;
        public void ShowOrHideRenderers( bool ShouldBeShown )
        {
            if ( this.wereRenderersLastShown == ShouldBeShown )
                return;
            this.wereRenderersLastShown = ShouldBeShown;
            if ( this.allRenderers == null )
                this.allRenderers = this.UnityObject.GetComponentsInChildren<Renderer>();
            if ( this.allRenderers == null )
                return;
            for ( int i = 0; i < this.allRenderers.Length; i++ )
                this.allRenderers[i].enabled = ShouldBeShown;
        }
        #endregion

        #region UpdateWormhole
        private static InputActionTypeData inputShowPlanetNamesOnSinglePlanet = null;
        private int lastKnownWormholeLayer = -1;
        private void UpdateWormhole( BattlefieldVisualSingleton MainVis, float GimbalScaleMultiplier )
        {
            this.UpdateMeshes();
            this.DoScaleCheck( MainVis, GimbalScaleMultiplier );
            this.DoLookAtCheck( MainVis );

            if ( inputShowPlanetNamesOnSinglePlanet == null )
                inputShowPlanetNamesOnSinglePlanet = InputActionTypeDataTable.GetActionByName_FairlySlow( "ShowPlanetNamesOnSinglePlanet" );

            int desiredWormholeLayer = 22;//invisible
            if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.ShowPlanetNamesAboveWormholesByDefault ) )
                desiredWormholeLayer = 23; //sprites 17; //selectable ship
            if ( inputShowPlanetNamesOnSinglePlanet.CalculateIsKeyDownNow_IgnoreConflicts() )
                desiredWormholeLayer = 24; //overlay item
            
            var pos = RelatedEntity.WorldLocation.ToVisualMainGameCoordinates_Unity(RelatedEntity.Planet);

            if ( ArcenMainGameVisuals.AllowYOffsetsForGimbals )
            {
                var dir = ArcenMainGameVisuals.MainViewCamera.transform.position - pos;
                dir.Normalize();

                var dist = RelatedTypeData.YOffsetOfIcon + ExternalVisualConstants.Instance.extra_y_offset_to_all_icons;

                pos += dir * dist;
            }

            if ( RelatedTypeData.GimbalNameExtraTextOffsetY != 0 )
            {
                pos.y += this.RelatedTypeData.GimbalNameExtraTextOffsetY;
            }

            this.UnityObject.TextMeshOrNull.transform.position = pos;
            
            if ( desiredWormholeLayer != lastKnownWormholeLayer )
            {
                lastKnownWormholeLayer = desiredWormholeLayer;
                if ( this.UnityObject.TextMeshOrNull )
                {
                    this.UnityObject.TextMeshOrNull.gameObject.layer = desiredWormholeLayer;
                    this.UnityObject.TextMeshGimbalParentOrNull.gameObject.layer = 24; //wormhole
                }
            }
        }
        #endregion

        private WormholeStatus lastWormholeStatus = (WormholeStatus)(-1);
        private bool lastWormholeWasHovered = false;

        private bool hasDoneInitOfGimbals;
        private bool isGimbalTextOn;
        private ArcenDoubleCharacterBuffer displayName = new ArcenDoubleCharacterBuffer("GenericObjectVisualizer.displayName");
        
        private void UpdateMeshes()
        {
            if ( this.RelatedTypeData == null )
                return;

            if ( !this.hasDoneInitOfGimbals )
            {
                if ( this.UnityObject.TextMeshOrNull )
                {
                    this.hasDoneInitOfGimbals = true;
                    this.UnityObject.TextMeshGimbalParentOrNull.gameObject.layer = PlanetViewSelectionManager.Instance.selectedLayerInt;
                    this.UnityObject.TextMeshOrNull.gameObject.layer = PlanetViewSelectionManager.Instance.selectedLayerInt;
                    this.UnityObject.TextMeshOrNull.SetText( this.RelatedTypeData.GimbalName );
                    this.UnityObject.TextMeshGimbalParentOrNull.transform.localScale *= this.RelatedTypeData.NameTextSize;
                    this.UnityObject.TextMeshOrNull.color = this.RelatedTypeData.NameTextColor;
                    //if ( this.RelatedTypeData.GimbalNameExtraTextOffsetY != 0 )
                    //{
                    //    Vector3 position = this.UnityObject.TextMeshGimbalParentOrNull.transform.localPosition;
                    //    position.y += this.RelatedTypeData.GimbalNameExtraTextOffsetY;
                    //    this.UnityObject.TextMeshGimbalParentOrNull.transform.localPosition = position;
                    //}
                }
            }

            #region Name Text and Wormhole Color
            if ( this.UnityObject.ObjectType == VisualObjectType.Wormhole )
            {
                if ( this.UnityObject.AlternateMaterials != null )
                {
                    WormholeStatus status = this.GetWormholeStatus( this.RelatedEntity );
                    bool isHovered = ( GameEntity_Base.CurrentlyHoveredOver == this.RelatedEntity );
                    if ( status != this.lastWormholeStatus || isHovered != this.lastWormholeWasHovered )
                    {
                        this.lastWormholeWasHovered = isHovered;
                        this.lastWormholeStatus = status;

                        for ( int i = 0; i < this.UnityObject.RelatedRenderers.Length; i++ )
                            this.UnityObject.RelatedRenderers[i].sharedMaterial = this.UnityObject.AlternateMaterials[(int)status + ( isHovered ? 1 : 0 )];
                    }
                }

                if ( !this.isGimbalTextOn )
                {
                    this.isGimbalTextOn = true;
                    if ( this.UnityObject.TextMeshGimbalParentOrNull )
                        this.UnityObject.TextMeshGimbalParentOrNull.gameObject.SetActive( true );
                }
                
                if ( this.UnityObject.TextMeshOrNull &&
                     !Engine_AIW2.Instance.InHideGimbalMode && 
                     !ArcenUI.Instance.InHideGUIMode )
                {
                    var impl = PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayModeImplementationSafe();
                    if ( impl != null )
                    {
                        impl.WriteEntityPlanetViewText( this.RelatedEntity, displayName );

                        bool changed;
                        var str = displayName.GetStringAndResetForNextUpdate(out changed);
                        
                        if (changed)
                            this.UnityObject.TextMeshOrNull.SetText( str );
                    }
                }
            }
            #endregion
        }

        public void DoLookAtCheck( BattlefieldVisualSingleton MainVis )
        {
            if ( !this.UnityObject.TextMeshGimbalParentOrNull )
                return;

            this.UnityObject.TextMeshGimbalParentOrNull.LookAt( MainVis.MainCameraPos, MainVis.MainUpVector );
        }

        #region DoScaleCheck
        private float lastScale_Gimbal = -1;
        public void DoScaleCheck( BattlefieldVisualSingleton MainVis, float GimbalScaleMultiplier )
        {
            if ( !this.UnityObject.TextMeshGimbalParentOrNull )
                return;

            Vector3 globalPos = this.UnityObject.TextMeshGimbalParentOrNull.position;
            float distance = Mat.MagnitudeFastestMainThread( MainVis.MainCameraPos - globalPos );
            if ( distance > ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance )
            {
                float sizeScale = 1f + ( ( distance - ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance ) * ExternalVisualConstants.Instance.gimbal_scaling_up_multiplier );
                sizeScale *= GimbalScaleMultiplier;
                if ( Mat.Abs( this.lastScale_Gimbal - sizeScale ) < 0.001f )
                    return;
                this.lastScale_Gimbal = sizeScale;
                this.UnityObject.TextMeshGimbalParentOrNull.localScale = new Vector3( sizeScale, sizeScale, sizeScale );
            }
            else
            {
                float percentageIn = distance / ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance;
                float sizeScale = percentageIn * ExternalVisualConstants.Instance.gimbal_scaling_down_multiplier;
                if ( sizeScale < ExternalVisualConstants.Instance.gimbal_scaling_absolute_min )
                    sizeScale = ExternalVisualConstants.Instance.gimbal_scaling_absolute_min;
                sizeScale *= GimbalScaleMultiplier;
                if ( Mat.Abs( this.lastScale_Gimbal - sizeScale ) < 0.001f )
                    return;
                this.lastScale_Gimbal = sizeScale;
                this.UnityObject.TextMeshGimbalParentOrNull.localScale = new Vector3( sizeScale, sizeScale, sizeScale );
            }
        }
        #endregion

        private enum WormholeStatus
        {
            Basic = 0,
            WaveIncoming = 2,
            MyPlanet = 4,
            Allied = 6,
            Unowned = 8,
            NeutralFaction = 10
        }

        #region GetWormholeStatus
        private WormholeStatus GetWormholeStatus( GameEntity_Other entity )
        {
            int debugLine = 0;
            try
            {
                debugLine = 1;
                bool debug = false;
                if ( entity == null )
                    return WormholeStatus.Unowned;
                debugLine = 2;
                if ( entity.TypeData.OtherSpecialType != OtherSpecialEntityType.Wormhole )
                    return WormholeStatus.Unowned;
                debugLine = 3;
                Planet currentPlanet = entity.Planet;
                debugLine = 4;
                Planet wormholeDestPlanet = entity.GetLinkedPlanet();
                debugLine = 5;
                if ( wormholeDestPlanet == null )
                    return WormholeStatus.Unowned;
                debugLine = 6;
                Faction destFaction = wormholeDestPlanet.GetControllingFaction();
                debugLine = 7;
                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "currentPlanet  " + currentPlanet.Name + " wormhole to " + wormholeDestPlanet.Name + " owned by " + destFaction.GetDisplayName() + " idx " + destFaction.FactionIndex + ", figuring out colour", Verbosity.DoNotShow );
                debugLine = 8;
                if ( destFaction != null && destFaction.Type != FactionType.NaturalObject )
                {
                    debugLine = 9;
                    debugLine = 10;
                    Faction controllingFaction = currentPlanet.GetControllingFaction();
                    debugLine = 1001;
                    if ( controllingFaction != null && controllingFaction.Type == FactionType.Player && destFaction.Type == FactionType.AI )
                    {
                        debugLine = 11;
                        //check whether this wormhole has a wave going to come through it,
                        //and if so then show that
                        AISentinelsFactionBaseInfo sentinelsInfo = destFaction.GetAISentinelsCoreData();
                        debugLine = 12;
                        ProtectedList<PlannedWave> localList = sentinelsInfo.WaveList;
                        debugLine = 13;
                        for ( int i = 0; i < localList.Count; i++ )
                        {
                            debugLine = 14;
                            PlannedWave wave = localList[i];
                            debugLine = 15;
                            if ( wave != null && wave.playerBeingAlerted )
                            {
                                debugLine = 16;
                                Planet warpGatePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                                debugLine = 17;
                                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                                debugLine = 18;
                                if ( targetPlanet == currentPlanet && wormholeDestPlanet == warpGatePlanet )
                                {
                                    debugLine = 19;
                                    return WormholeStatus.WaveIncoming;
                                }
                            }
                        }
                    }
                    debugLine = 22;

                    //other side belongs to me or a friend
                    if ( destFaction.Type == FactionType.Player )
                    {
                        //if it's me
                        if ( destFaction.GetIsLocalFaction() )
                            return WormholeStatus.MyPlanet;
                        else
                            return WormholeStatus.Allied;
                    }
                    else
                    {
                        //typical AI wormhole
                        if ( destFaction.GetIsHostileToLocalFaction() )
                            return WormholeStatus.Basic;
                        else //allied or neutral factions, technically
                            return WormholeStatus.NeutralFaction;
                    }
                }
                //not owned by anyone, legitimately this time
                return WormholeStatus.Unowned;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Warning: GetWormholeStatus encountered exception on debugLine==" + debugLine + "\n" + e.ToString(), Verbosity.Chat );
            }
            return WormholeStatus.Unowned;
        }
        #endregion
    }
}
