using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;

using Arcen.Universal.Sprites;
using Arcen.Universal.Sprites.Instanced;
using MeshSprites = Arcen.Universal.Sprites.MeshBased;
using Arcen.AIW2.External;
using System.Numerics;

namespace Arcen.AIW2.ExternalVisualization
{
    public class PlanetViewSelector : IUpdateCycler, IArcenShapeOwner
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "PlanetViewSelectors" );
        public PlanetViewSelector()
        {
            RefTracker.IncrementObjectCount();

            Random = new MersenneTwister(Engine_Universal.PermanentQualityRandom.Next());
        }

        private RandomGenerator Random;

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            this.ClearWhenNotInThisMode();
        }

        public PlanetViewSelectionManager ManagerCore;

        public void ClearWhenNotInThisMode()
        {
            if ( this.placementGimbal )
                this.placementGimbal.SetActiveStateIfNeeded( false );

            if ( cursorInner != null)
                cursorInner.SetEnabled( false );
            if ( cursorOuter != null )
                cursorOuter.SetEnabled( false );

            if ( placementModeInner != null )
                placementModeInner.SetEnabled( false );
            if ( placementModeOuter != null )
                placementModeOuter.SetEnabled( false );

            this.rangeRingCurrentIndex = 0;
            this.HideExtraRangeRings();
        }

        private GameEntity_Base entityUnderCursor = null;

        [ThreadStatic] //ok - this is only really needed on one thread, but having it available on multiple is a good idea
        private static System.Diagnostics.Stopwatch swplanetViewMouseHandling = null;

        private static void WriteToPlanetViewMouseHandlingLog( string Text )
        {
            try
            {
                Engine_Universal.AppendTextToFile( Engine_Universal.CurrentPlayerDataDirectory + "PlanetViewMouseHandlingLog.txt", DateTime.Now.ToShortDateString() + " " +
                    DateTime.Now.ToShortTimeString() + ": " + Text + Environment.NewLine, 1024 * 1024 );
            }
            catch ( Exception ) { } //ignore any of these, probably sharing violation IOExceptions
        }

        private readonly List<ArcenMouseEventType> currentMouseEvents = List<ArcenMouseEventType>.Create_WillNeverBeGCed( 80, "PlanetViewSelector-currentMouseEvents" );
        public void RunUpdate( IUpdateSource Source, bool CanDoRaycasts, bool CanDoClickHandling )
        {
            Log log = null;
            //log = Log.Yes;
            try
            {
            this.ManagerCore = (PlanetViewSelectionManager)Source;

            #region Early-outs
            if ( !ArcenMainGameVisuals.MainViewCamera )
                return;
            if ( ArcenInput.GetIsInputBlocked() )
            {
                this.ClearWhenNotInThisMode();
                return;
            }
            if ( !this.ManagerCore )
                return;
            if ( !this.ManagerCore.unitsContainer )
                return;
            #endregion

            if ( !InputActionTypeDataTable.IsInitialized )
                return;
            InputCaching.UpdateInputCacheIfNeeded();

            Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            if ( currentPlanet == null )
                return;

            currentMouseEvents.Clear();
            bool hadPrimaryDownEvent = false;
            ArcenInputFlags mouseEventFlags = ArcenInputFlags.None;

            #region setting mouseEventFlags based on what's being held down
            bool throughWormhole = InputCaching.inputSendThroughWormhole.CalculateIsKeyDownNow_IgnoreConflicts();
            bool ordersToStationaryFlagships = InputCaching.inputHoldToGiveOrdersToStationaryFlagships.CalculateIsKeyDownNow_IgnoreConflicts();

            if ( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Additive );
            if ( InputCaching.inputRemoveFromSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Subtractive );
            if ( throughWormhole )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.WormholeInteraction );
            if ( ordersToStationaryFlagships )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes );
            if ( InputCaching.inputSelectOnlyTurrets.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.OnlyTurrets );
            #endregion

            #region set mouseEventType, and pick the layer mask for the entityUnderCursor raycast
            LayerMask layerMask = this.ManagerCore.selectedLayerOneOnly;
            if ( PresentationLayer_AIW2.Instance.IgnoreNextMouseUp )
            {
                this.ManagerCore.isSelecting = false;
                PresentationLayer_AIW2.Instance.IgnoreNextMouseUp = false;
            }
            else
            {
                if ( InputCaching.inputSelectUnit.CalculateIsDoubleRelease() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.PrimaryDoubleUp );
                    layerMask = this.ManagerCore.targetLayers;
                }
                
                if ( InputCaching.inputSelectUnit.CalculateIsSinglePress() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.PrimaryDown );
                    layerMask = this.ManagerCore.targetLayers;
                    hadPrimaryDownEvent = true;
                }
                
                if ( InputCaching.inputGiveOrdersToUnit.CalculateIsDoubleRelease() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.SecondaryDoubleUp );
                    layerMask = this.ManagerCore.targetLayers;
                }
                
                if ( InputCaching.inputGiveOrdersToUnit.CalculateIsSinglePress() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.SecondaryDown );
                    layerMask = this.ManagerCore.targetLayers;
                }
            }
            #endregion

            //Engine_Universal.BeginProfilerSample( "simPointUnderCursor" );
            #region simPointUnderCursor raycast
            if ( !Engine_Universal.IsMouseOverGUI ) //let this raycast slide no matter what, for purposes of higher accuracy
            {
                if ( this.ManagerCore.SimpleRaycast( this.ManagerCore.planeLayerMaskOnly ) )
                {
                    this.ManagerCore.worldPointUnderCursor = this.ManagerCore.raycastHit.point;
                    this.ManagerCore.worldUnitsPointUnderCursor = this.ManagerCore.unitsContainer.transform.worldToLocalMatrix.MultiplyPoint( this.ManagerCore.worldPointUnderCursor );
                    this.ManagerCore.simPointUnderCursor = ArcenPoint.Create(
                        Mathf.RoundToInt( (this.ManagerCore.worldUnitsPointUnderCursor.x * currentPlanet.GravWellSize.CombatVisualScaleDivisor) + Engine_AIW2.Instance.CombatCenter.X ),
                        Mathf.RoundToInt( (this.ManagerCore.worldUnitsPointUnderCursor.z * currentPlanet.GravWellSize.CombatVisualScaleDivisor) + Engine_AIW2.Instance.CombatCenter.Y )
                        );
                    this.ManagerCore.simPointUnderCursor = currentPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( this.ManagerCore.simPointUnderCursor );
                }
            }
            #endregion
            //Engine_Universal.EndProfilerSample( "simPointUnderCursor" );

            if ( CanDoRaycasts )
            {
                entityUnderCursor = null;

                #region entityUnderCursor raycast
                if ( Engine_Universal.IsMouseOverGUI )
                {
                    entityUnderCursor = null;
                    this.ManagerCore.simPointUnderCursor = ArcenPoint.OutOfRange;

                    //Engine_Universal.BeginProfilerSample( "DoIsSelectingLogic" );
                    //Engine_Universal.EndProfilerSample( "DoIsSelectingLogic" );

                    switch ( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType )
                    {
                        case FromSidebarType.NonSidebar_MultipleUnits:
                        case FromSidebarType.NonSidebar_SingleUnit:
                            GameEntity_Base.SetCurrentlyHoveredOver( null, FromSidebarType.SelectionWindow_SingleUnit );
                            break;
                    }
                    Planet.SetCurrentlyHoveredOver( null );
                    GalaxyMapPlanetLink.CurrentlyHoveredOver = null;
                    CanDoClickHandling = false;
                }
                else
                {
                    #region Unity Raycast
                    
                    #region profile
                    Engine_Universal.BeginProfilerSample( "ManagerCore.SimpleRaycast" );
                    bool planetViewMouseHandlingDebugLog = GameSettings.Current.GetBoolBySetting( "PlanetViewMouseHandlingDebugLog" );
                    if ( planetViewMouseHandlingDebugLog )
                    {
                        if ( swplanetViewMouseHandling == null )
                            swplanetViewMouseHandling = new System.Diagnostics.Stopwatch();
                        swplanetViewMouseHandling.Start();
                    }
                    #endregion

                    bool foundHit = false;
                    if ( throughWormhole ) //first search for wormholes ONLY if we are holding the wormhole button
                    {
                        foundHit = this.ManagerCore.SimpleRaycast( this.ManagerCore.wormholeLayerOneOnly );
                        log?.Msg("{0}: Check vs Collider (wormholes)", foundHit?"HIT":"MISS");
                    }
                    
                    if (!foundHit) //then search for any ships or structures
                    {
                        foundHit = this.ManagerCore.SimpleRaycast( layerMask );
                        log?.Msg("{0}: Check vs Collider (entities)", foundHit?"HIT":"MISS");
                    }

                    if ( foundHit )
                    {
                        GameObject raycastHitGameObject = this.ManagerCore.raycastHit.transform.gameObject;
                        IVisualObject visualObject = raycastHitGameObject.GetComponentInParent<IVisualObject>();
                        if ( visualObject != null )
                        {
                            entityUnderCursor = visualObject.GetEntityRelatedTo();
                            //Engine_Universal.DebugText = "IVisualObject method:" + (entityUnderCursor == null ? "null" : entityUnderCursor.TypeData.InternalName) + " " + raycastHitGameObject.name + " " + System.DateTime.Now;
                        }
                        else
                        {
                            ArcenGimbal gimbbalObject = raycastHitGameObject.GetComponentInParent<ArcenGimbal>();
                            if ( gimbbalObject && gimbbalObject.Visualizer != null )
                            {
                                entityUnderCursor = gimbbalObject.Visualizer.GetEntityRelatedTo();
                            }
                        }
                    }

                    #region profile
                    if ( planetViewMouseHandlingDebugLog )
                    {
                        swplanetViewMouseHandling.Stop();
                        LOG.Msg( "[perf] FindHoveredSquadByRaycast() took {0} ms", swplanetViewMouseHandling.ElapsedMilliseconds);
                        swplanetViewMouseHandling.Reset();
                    }
                    Engine_Universal.EndProfilerSample( "ManagerCore.SimpleRaycast" );
                    #endregion

                    #endregion

                    bool easymode = GameSettings.Current.GetBoolBySetting("EasyModeShipClicking");
                    if (easymode)
                    {
                        // this section checks against for mouse-over using our own collision sphere
                        // which is more leniant than the unity raycast collision shape
                        // also we we test wormholes and everything else at the same time
                        // not in separate passes, since we can pick the closest, in a 3D test
                        #region Dismiss's
                        if ( entityUnderCursor == null )
                        {
                            //LOG.Msg("Check vs IVisualObject(s)");
                            
                            #region Profile
                            if ( planetViewMouseHandlingDebugLog )
                            {
                                if ( swplanetViewMouseHandling == null )
                                    swplanetViewMouseHandling = new System.Diagnostics.Stopwatch();
                                swplanetViewMouseHandling.Start();
                            }
                            Engine_Universal.BeginProfilerSample( "FindHoveredSquadByRadius" );
                            #endregion

                            //System.Numerics.Vector3 cursorLocation = this.ManagerCore.worldPointUnderCursor.ToNumericsVector3();
                            
                            float min_entity_world_radius = ExternalConstants.Instance.OriginalXmlData.GetFloat("min_entity_world_radius", 0.0f, false);
                            float min_entity_screen_radius = ExternalConstants.Instance.OriginalXmlData.GetFloat("min_entity_screen_radius", 0.0f, false);
                            
                            // if wormhole button held, check them first
                            var other = BattlefieldVisualSingleton.Instance.LooseOtherObjects;
                            var squads = BattlefieldVisualSingleton.Instance.ActiveSquads;
                            var ray = ArcenMainGameVisuals.MainViewCamera.ScreenPointToRay( Input.mousePosition );
                            
                            float min_d = float.MaxValue;
                            GameEntity_Base min_e = null;
                            int counter = 0;
                            
                            bool CheckHit(System.Collections.IEnumerable list)
                            {
                                foreach (var itr in list)
                                {
                                    var vis = itr as IRelatedEntity;
                                    if (vis == null)
                                    {
                                        LOG.Msg("itr is not an IRelatedEntity?? its {0}", itr.GetType().Name);
                                        continue;
                                    }
                                    
                                    var e = vis.GetEntityRelatedTo();
                                    if (e == null)
                                        continue;
                                    
                                    counter++;
                                    
                                    var type = e.TypeData;
                                    var divisor = e?.Planet.GravWellSize.CombatVisualScaleDivisor ?? 20.0f;
                                   
                                    var w_pos = e.RenderPosition;
                                    var w_radius = e.GetRadius() / divisor;
                                    
                                    var wc_radius = w_radius;
                                    if (wc_radius < min_entity_world_radius)
                                        wc_radius = min_entity_world_radius;

                                    //var spos = ArcenMainGameVisuals.MainViewCamera.WorldToScreenPoint(pos);
                                    var s_radius = ArcenMainGameVisuals.MainViewCamera.WorldToScreenRadius(wc_radius, w_pos);

                                    var sc_radius = s_radius;
                                    if (sc_radius < min_entity_screen_radius)
                                        sc_radius = min_entity_screen_radius;

                                    var f_radius = ArcenMainGameVisuals.MainViewCamera.ScreenToWorldRadius(sc_radius, w_pos);
                                    
                                    //var offset = e.TypeData.YOffsetOfIcon / divisor * -ray.direction;
                                    //pos += offset;
                                    
                                    float d = 0;
                                    UnityEngine.Vector3 h = UnityEngine.Vector3.zero;
                                    bool yes = Mat.RayIntersectSphere(ray.origin, ray.direction, w_pos, f_radius, ref d, ref h);
                                    
                                    //LOG.Msg("{0}; check p={1} r={2} sp={3} sr={4} | rp={5} rd={6}", 
                                      //  yes?"HIT":"MISS", pos, radius, spos, sradius, ray.origin, ray.direction);
                                    
                                    //InSceneTextRenderer.Instance.DrawPos(42, counter, spos, sradius, Color.white, 1.0f);
                                    
                                    if (!yes)
                                        continue;
                                    
                                    //LOG.Msg("ray hit '{0}' d={1} h={2}", e, d, h);
                                    
                                    if (d < min_d)
                                    {
                                        min_d = d;
                                        min_e = e;
                                    }
                                }
                                
                                return min_e != null;
                            }
                            
                            counter = 0;
                            bool hit = CheckHit( other );
                            //LOG.Msg("{0}: Check vs radius ({1}x wormholes)", hit?"HIT":"MISS", counter);
                            
                            if (!(hit && throughWormhole))
                            {
                                counter = 0;
                                hit = CheckHit( squads );
                                //LOG.Msg("{0}: Check vs radius ({1}x entity)", hit?"HIT":"MISS", counter);
                            }
                            
                            entityUnderCursor = min_e;

                            #region Profile
                            Engine_Universal.LodgeCountMessageInProfilerData( counter );
                            Engine_Universal.EndProfilerSample( "FindHoveredSquadByRadius" );
                            
                            if ( planetViewMouseHandlingDebugLog )
                            {
                                swplanetViewMouseHandling.Stop();
                                LOG.Msg( "[perf] FindHoveredSquadByRadius() took {0} ms", swplanetViewMouseHandling.ElapsedMilliseconds);
                                swplanetViewMouseHandling.Reset();
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        // this is the original algorithm which does a 2D circle against circle test
                        // and then aunity raycast against wormholes..
                        #region Original
                        
                        if ( entityUnderCursor == null )
                        {
                            //Engine_Universal.BeginProfilerSample( "FindHoveredSquadByRadius" );
                            System.Numerics.Vector3 cursorLocation = this.ManagerCore.worldPointUnderCursor.ToNumericsVector3();

                            List<SquadVisualizer> squads = BattlefieldVisualSingleton.Instance.ActiveSquads;
                            SquadVisualizer vis;
                            int checkedCount = 0;
                            for ( int i = 0; i < squads.Count; i++ )
                            {
                                vis = squads[i];
			                    GameEntity_Squad squad = vis.RelatedEntity.GetSquad();
                                if ( squad == null )
                                    continue;
                                checkedCount++;
                                if ( (vis.CurrentPosition - cursorLocation).Length() < squad.DataForMark.CalculateRadiusForDisplay( currentPlanet ) )
                                {
                                    entityUnderCursor = squad;
                                    //Engine_Universal.DebugText = "BattlefieldVisualSingleton.Instance.ActiveSquads method:" + ( entityUnderCursor == null ? "null" : entityUnderCursor.TypeData.InternalName ) + " " + System.DateTime.Now;
                                    break;
                                }
                            }

                            Engine_Universal.LodgeCountMessageInProfilerData( checkedCount );
                            //Engine_Universal.EndProfilerSample( "FindHoveredSquadByRadius" );
                        }

                        //if we still have nothing, see about those wormholes!
                        if ( entityUnderCursor == null )
                        {
                            if ( !foundHit && !throughWormhole ) //if we did not search for wormholes before, and we do not have anything else, then search for wormholes
                                foundHit = this.ManagerCore.SimpleRaycast( this.ManagerCore.wormholeLayerOneOnly );

                            if ( foundHit )
                            {
                                GameObject raycastHitGameObject = this.ManagerCore.raycastHit.transform.gameObject;
                                IVisualObject visualObject = raycastHitGameObject.GetComponentInParent<IVisualObject>();
                                if ( visualObject != null )
                                {
                                    entityUnderCursor = visualObject.GetEntityRelatedTo();
                                }
                                else
                                {
                                    {
                                        ArcenGimbal gimbbalObject = raycastHitGameObject.GetComponentInParent<ArcenGimbal>();
                                        if ( gimbbalObject && gimbbalObject.Visualizer != null )
                                        {
                                            entityUnderCursor = gimbbalObject.Visualizer.GetEntityRelatedTo();
                                        }
                                    }
                                }
                            }
                        }

                        #endregion
                    }
                }
                #endregion
            }

            //do this regardless of whether or not this is a raycast frame
            if ( Engine_Universal.IsMouseOverGUI )
            {
                this.ClearWhenNotInThisMode();
            }
            else
            {
                GameEntity_Base.SetCurrentlyHoveredOver( entityUnderCursor, FromSidebarType.NonSidebar_SingleUnit );
                Planet.SetCurrentlyHoveredOver( null );
                GalaxyMapPlanetLink.CurrentlyHoveredOver = null;
            }

            if ( CanDoClickHandling )
            {
                //Engine_Universal.DebugText = ( entityUnderCursor == null ? "null" : entityUnderCursor.TypeData.InternalName ) + " " + System.ArcenTime.Now;
                #region Profile
                bool planetViewMouseHandlingDebugLog = GameSettings.Current.GetBoolBySetting( "PlanetViewMouseHandlingDebugLog" );
                if ( planetViewMouseHandlingDebugLog )
                {
                    if ( swplanetViewMouseHandling == null )
                        swplanetViewMouseHandling = new System.Diagnostics.Stopwatch();
                    swplanetViewMouseHandling.Start();
                }
                #endregion
                
                HandleMouseCursor( this.ManagerCore.simPointUnderCursor );
                
                bool isBandBoxMode = this.ManagerCore.isSelecting && Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePosition, Input.mousePosition ).size.x > 0;
                
                foreach ( ArcenMouseEventType mouseEventType in currentMouseEvents )
                    HandleMouseEvent_PlanetView( mouseEventType, mouseEventFlags, this.ManagerCore.simPointUnderCursor, entityUnderCursor, isBandBoxMode, planetViewMouseHandlingDebugLog );

                #region Profile
                if ( planetViewMouseHandlingDebugLog )
                {
                    swplanetViewMouseHandling.Stop();
                    LOG.Msg( "[perf] ClickHandling() took {0} ms", swplanetViewMouseHandling.ElapsedMilliseconds);
                    if ( swplanetViewMouseHandling.ElapsedMilliseconds > 10 )
                        WriteToPlanetViewMouseHandlingLog( "WARNING TOTAL PASS took ms: " + swplanetViewMouseHandling.ElapsedMilliseconds );
                    else
                        WriteToPlanetViewMouseHandlingLog( "TOTAL PASS took only ms: " + swplanetViewMouseHandling.ElapsedMilliseconds );
                    swplanetViewMouseHandling.Reset();
                }
                #endregion
            }

            //needs to be reset
            this.rangeRingCurrentIndex = 0;

            this.DrawCursorStateForPlanetView( entityUnderCursor != null, currentPlanet );

            this.HideExtraRangeRings();

            if ( //!InputCaching.inputSelectUnit.CalculateIsKeyDownNow_IgnoreConflicts() &&
                ManagerCore.isSelecting )
            {
                this.DoIsSelectingLogic_DragSelectPlanetView( mouseEventFlags );
                
                if (!InputCaching.inputSelectUnit.CalculateIsKeyDownNow_IgnoreConflicts())
                    this.ManagerCore.isSelecting = false;

                return;
            }
            
            

            #region bounding-box selection stuff
            
            if ( Engine_AIW2.Instance.PendingTargetedAction != null )
            {
                this.ManagerCore.isSelecting = false;
            }
            // todo: make these all ITargetedInputAction(s)
            else 
            if ( Engine_AIW2.Instance.IsInPingLocationMode )
            {
                this.ManagerCore.isSelecting = false;
            }
            else  
            if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() )
            {
                this.ManagerCore.isSelecting = false;
            }
            else  
            if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null )
            {
                this.ManagerCore.isSelecting = false;
            }
            else 
            if ( !Engine_Universal.IsMouseOverGUI && 
                 !this.ManagerCore.isSelecting &&
                 InputCaching.inputSelectUnit.CalculateIsKeyDownNow_IgnoreConflicts() )
            {
                this.ManagerCore.isSelecting = true;
                this.ManagerCore.mousePosition = Input.mousePosition;
            }

            if ( this.ManagerCore.isSelecting )
            {
                //Engine_Universal.BeginProfilerSample( "DoIsSelectingLogic" );
                //this.DoIsSelectingLogic_DragSelectPlanetView( mouseEventFlags );
                if ( !hadPrimaryDownEvent )
                {
                    //bool isBandBoxMode = this.ManagerCore.isSelecting && Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePosition, Input.mousePosition ).size.x > 0;
                    //HandleMouseEvent_PlanetView( ArcenMouseEventType.None, mouseEventFlags, this.ManagerCore.simPointUnderCursor, entityUnderCursor, isBandBoxMode, false );
                }
                //Engine_Universal.EndProfilerSample( "DoIsSelectingLogic" );
            }
                #endregion
            }
            catch (Exception e)
            {
                LOG.Msg("exception in PlanetViewSelector.RunUpdate()\n{0}", e);
            }
        }

        #region CalculateCursorScale
        private float CalculateCursorScale()
        {
            float desiredScale = ArcenVisualOrganizer.Instance.MainViewCameraWrapper.GetCameraZoomOutAs01PercentForCursorScale();
            if ( desiredScale > ExternalVisualConstants.Instance.MainGameCursorScalesUpAbove )
                desiredScale *= (1 + (desiredScale - ExternalVisualConstants.Instance.MainGameCursorScalesUpAbove) * ExternalVisualConstants.Instance.MainGameCursorScalesUpMultiplier);
            else if ( desiredScale > ExternalVisualConstants.Instance.MainGameCursorScalesDownBelow )
                desiredScale = 1f;
            else
            {
                desiredScale /= ExternalVisualConstants.Instance.MainGameCursorScalesDownBelow;
                if ( desiredScale < ExternalVisualConstants.Instance.MainGameCursorMinScaleMultiplier )
                    desiredScale = ExternalVisualConstants.Instance.MainGameCursorMinScaleMultiplier;
            }
            return desiredScale;
        }
        #endregion

        private ArcenShape_DiscSizedBase cursorInner = null;
        private ArcenShape_ThicknessRingBase cursorOuter = null;

        private ArcenShape_DiscSizedTransparentBase placementModeInner = null;
        private ArcenShape_ThicknessRingBase placementModeOuter = null;

        #region DrawCursorStateForPlanetView
        private ArcenPlacementGimbal placementGimbal = null;
        private Transform placementGimbalT = null;

        //private bool hasShownCursorYet = false;
        //public Vector3 cursorBaseLocation = Mat.V3_Zero;
        public void DrawCursorStateForPlanetView( bool IsHoveringOverEntity, Planet currentPlanet )
        {
            if ( !ArcenVisualOrganizer.Instance )
                return;
            if ( FrontEndLink.Instance == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI )
            {
                this.ClearWhenNotInThisMode();
                return;
            }

            float desiredScaleBase = this.CalculateCursorScale();
            float desiredScale = desiredScaleBase * ExternalVisualConstants.Instance.MainGameCursorRadius;

            #region Also Draw Any Pings In General
            if ( currentPlanet != null && currentPlanet.Network_Pings.Count > 0 )
            {
                foreach ( PlanetPing ping in currentPlanet.Network_Pings )
                {
                    if ( ping.IsOnGalaxyMapOnly )
                        continue;
                    this.DrawPingAtPlanetAtSpot( ping.Color, ping.Point.ToVisualMainGameCoordinates_Unity( currentPlanet  ), desiredScale * ping.ScaleToDraw_OnPlanet() );
                }
            }
            #endregion

            //Engine_Universal.DebugText = ( Engine_AIW2.Instance.PlacingEntityType == null ? "null type" : Engine_AIW2.Instance.PlacingEntityType.TypeData.ToString() ) + "   " + DateTime.Now;

            if ( cursorInner == null || !cursorInner.GetIsStillValidForUseAtOwner( this ) )
                cursorInner = (ArcenShape_DiscSizedBase)ArcenVisualOrganizer.Instance.Shape_DiscSized_Pool.GetFromPool( this );
            if ( cursorOuter == null || !cursorOuter.GetIsStillValidForUseAtOwner( this ) )
                cursorOuter = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( this );

            if ( placementModeInner == null || !placementModeInner.GetIsStillValidForUseAtOwner( this ) )
                placementModeInner = (ArcenShape_DiscSizedTransparentBase)ArcenVisualOrganizer.Instance.Shape_DiscSizedTransparent_Pool.GetFromPool( this );
            if ( placementModeOuter == null || !placementModeOuter.GetIsStillValidForUseAtOwner( this ) )
                placementModeOuter = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( this );

            bool didDrawPlacementModeRing = false;
            bool didDrawCursor = false;

            //if we are outside the gravity well, this clamps in.
            //we don't want to highlight units like this, but for placement and cursor purposes we do want to
            //the parts that don't want to highlight this should still just use PlanetViewSelectionManager.Instance.worldPointUnderCursor
            UnityEngine.Vector3 worldBasedSimPointOfCursor = PlanetViewSelectionManager.Instance.simPointUnderCursor.ToVisualMainGameCoordinates_Unity( currentPlanet );
            
            if ( Engine_AIW2.Instance.PendingTargetedAction != null && ArcenUI.Instance.InHideGUIMode == false )
            { 
                Engine_AIW2.Instance.PendingTargetedAction.UpdateCursor(PlanetViewSelectionManager.Instance.simPointUnderCursor);
            }
            // todo: make these all ITargetedInputAction(s)
            else 
            if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() && !ArcenUI.Instance.InHideGUIMode )
            {
                DirectBuildable buildable = Engine_AIW2.Instance.PlacingDirectBuildable;
                #region Placement Mode
                //if ( IsHoveringOverEntity )
                //{
                //    if ( this.placementGimbal )
                //        this.placementGimbal.SetActiveStateIfNeeded( false );
                //}
                //else
                {
                    bool isBlocked = false;
                    if ( !currentPlanet.GetIsPlacementPointSafe( buildable.TypeData, LastSimPointForPlacementPurposes, true ) )
                        isBlocked = true;

                    UnityEngine.Vector3 placementPoint = SetPlacementGimbal( buildable.TypeData,
                        isBlocked ? TeamColorDefinitionTable.Instance.Black : TeamColorDefinitionTable.Instance.White, 1, currentPlanet );
                    //Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFaction();
                    GameEntityTypeData.MarkLevelStats markStats = buildable.ForMark;

                    didDrawPlacementModeRing = true;
                    float size = markStats.CalculateRadiusForDisplay( currentPlanet );

                    placementModeInner.SetValues( placementPoint, isBlocked ? ColorMath.RedHalfA : ExternalVisualConstants.Instance.color_placement_center, size );
                    placementModeOuter.SetValues( placementPoint, isBlocked ? ColorMath.DarkRed : ExternalVisualConstants.Instance.color_placement_border, size, 0.8f );

                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    var planetFaction = currentPlanet.GetPlanetFactionForFaction(localFaction);

                    for ( int i = 0; i < buildable.TypeData.SystemTypes.Count; i++ )
                    {
                        EntitySystemTypeData systemData = buildable.TypeData.SystemTypes[i];
                        EntitySystemTypeData.MarkLevelStats statsForMark = systemData.MarkStatsFor( buildable.EffectiveMark );

                        SquadVisualizer.DrawRangeCircleForSystem( placementPoint, systemData, statsForMark, planetFaction, false, this.rangeRings, ref this.rangeRingCurrentIndex, this, 0 );
                    }
                    SquadVisualizer.DrawRangeCircleForForcefield( placementPoint, buildable.TypeData, buildable.ForMark, null, currentPlanet,
                        this.rangeRings, ref this.rangeRingCurrentIndex, this );
                }
                #endregion
            }
            else 
            if ( Engine_AIW2.Instance.IsInPingLocationMode && !ArcenUI.Instance.InHideGUIMode )
            {
                this.DrawPingAtPlanetAtSpot( InputCaching.CalculatePlanetPingColor(), worldBasedSimPointOfCursor, desiredScale );
            }
            else 
            if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null && !ArcenUI.Instance.InHideGUIMode )
            {
                var outguard = Engine_AIW2.Instance.PlacingOutguardDeployable;

                // Using the OutguardFlagship entity type if we need a dummy to stand in.
                // Since the outguard data doesn't necessarily have an entity type it could have tags, etc.
                var entityType = GameEntityTypeDataTable.Instance.GetRowByName( "OutguardFlagship" );
                if (outguard.UnitBag != null && outguard.UnitBag.EntityList != null && outguard.UnitBag.EntityList.Count > 0)
                    entityType = outguard.UnitBag.EntityList[0];
                
                UnityEngine.Vector3 placementPoint = worldBasedSimPointOfCursor;
                var isBlocked = false;

                if ( !currentPlanet.GetIsPlacementPointSafe( entityType, LastSimPointForPlacementPurposes, true ) )
                    isBlocked = true;

                placementPoint = SetPlacementGimbal( entityType, isBlocked ? TeamColorDefinitionTable.Instance.Black : TeamColorDefinitionTable.Instance.White, 1, currentPlanet );
                
                didDrawPlacementModeRing = true;

                float size = 10.0f;
                placementModeInner.SetValues( placementPoint, isBlocked ? ColorMath.RedHalfA : ExternalVisualConstants.Instance.color_placement_center, size );
                placementModeOuter.SetValues( placementPoint, isBlocked ? ColorMath.DarkRed : ExternalVisualConstants.Instance.color_placement_border, size, 0.8f );
            }
            else
            {
                #region Not Placement Mode
                if ( this.placementGimbal )
                    this.placementGimbal.SetActiveStateIfNeeded( false );

                if ( ArcenUI.Instance.InHideGUIMode )
                {}
                else if ( IsHoveringOverEntity && Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() )
                {}
                else
                {
                    if ( PlanetViewSelectionManager.Instance )
                    {
                        UnityEngine.Vector3 point = worldBasedSimPointOfCursor;

                        didDrawCursor = true;
                        cursorInner.SetValues( point, Color.red, desiredScale * 0.1f );
                        cursorOuter.SetValues( point, Color.red, desiredScale, 0.2f * (desiredScaleBase < 1f ? desiredScaleBase : 1f ) );
                    }
                }
                #endregion
            }

            if ( !didDrawPlacementModeRing )
            {
                placementModeInner.SetEnabled( false );
                placementModeOuter.SetEnabled( false );
            }
            if ( !didDrawCursor )
            {
                cursorInner.SetEnabled( false );
                cursorOuter.SetEnabled( false );
            }
        }
        #endregion

        private readonly List<ArcenShape_ThicknessRingBase> rangeRings = List<ArcenShape_ThicknessRingBase>.Create_WillNeverBeGCed( 9000, "PlanetViewSelector-rangeRings" );
        private int rangeRingCurrentIndex = 0;

        #region HideExtraRangeRings
        private void HideExtraRangeRings()
        {
            if ( this.rangeRingCurrentIndex >= this.rangeRings.Count )
                return; //nothing to hide, no extras
            for ( int i = this.rangeRingCurrentIndex; i < this.rangeRings.Count; i++ )
            {
                ArcenShape_ThicknessRingBase ring = this.rangeRings[i];
                if ( ring != null )
                    ring.SetEnabled( false );
            }
        }
        #endregion

        private void DrawPingAtPlanetAtSpot( PlanetPingColor PingColor, UnityEngine.Vector3 Position, float desiredScale )
        {
            PlanetPing.FillColor( PingColor, out Color color1, out Color color2, out Color color3 );

            FrontEndLink.Instance.IMDraw_SolidDisc3D( Position, GalaxyMapPlanet.ring1.Quat, (9 + (3 * GalaxyMapPlanet.scaleMultiplier)) * desiredScale * 0.5f, IMDrawAxis.Y, color1 );
            FrontEndLink.Instance.IMDraw_SolidDisc3D( Position, GalaxyMapPlanet.ring2.Quat, (9 + (3 * GalaxyMapPlanet.scaleMultiplier2)) * desiredScale * 0.5f, IMDrawAxis.Y, color1 );
            FrontEndLink.Instance.IMDraw_SolidDisc3D( Position, GalaxyMapPlanet.ring3.Quat, (11 + (2 * GalaxyMapPlanet.scaleMultiplier2)) * desiredScale * 0.5f, IMDrawAxis.Y, color2 );
            FrontEndLink.Instance.IMDraw_SolidDisc3D( Position, GalaxyMapPlanet.ring4.Quat, (11 + (2 * GalaxyMapPlanet.scaleMultiplier3)) * desiredScale * 0.5f, IMDrawAxis.Y, color2 );
            FrontEndLink.Instance.IMDraw_SolidDisc3D( Position, GalaxyMapPlanet.ring5.Quat, (12 + (1 * GalaxyMapPlanet.scaleMultiplier3)) * desiredScale * 0.5f, IMDrawAxis.Y, color3 );
        }

        #region SetPlacementGimbal
        private UnityEngine.Vector3 SetPlacementGimbal( GameEntityTypeData TypeDataToDraw, TeamColorDefinition Color, float VerticalOffset, Planet currentPlanet )
        {
            if ( this.placementGimbal == null )
            {
                GameObject obj = new GameObject();
                obj.name = "PlacementGimbal";
                this.placementGimbalT = obj.transform;
                this.placementGimbalT.parent = ArcenVisualOrganizer.Instance.MainGameObjectParent;
                this.placementGimbalT.SetDefaultTRS();
                this.placementGimbalT.localRotation = UnityEngine.Quaternion.Euler( 0, 180, 0 );
                this.placementGimbal = obj.AddComponent<ArcenPlacementGimbal>();
            }

            UnityEngine.Vector3 placementPoint = LastSimPointForPlacementPurposes.ToVisualMainGameCoordinates_Unity( currentPlanet );
            //Debug.Log( placementPoint + "   " + ArcenInput.)

            this.placementGimbal.SetActiveStateIfNeeded( true );
            this.placementGimbal.UpdateValues( Color, TypeDataToDraw );
            UnityEngine.Vector3 placementGimbalPoint = ( placementPoint + new UnityEngine.Vector3( 0, VerticalOffset, 0 ) );
            this.placementGimbalT.position = placementGimbalPoint;
            this.placementGimbal.DoScaleCheck( placementGimbalPoint, TypeDataToDraw );
            return placementPoint;
        }
        #endregion
        
        private enum SelectedShipMenuStatus
        {
            None,
            Fleets,
            DirectBuild,
            Hacking,
            Science
        }

        public void DoIsSelectingLogic_DragSelectPlanetView( ArcenInputFlags mouseEventFlags )
        {
            this.ManagerCore.selectionViewportBounds = Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePosition, Input.mousePosition );

            if ( this.ManagerCore.selectionViewportBounds.size.x <= 0 )
                return;

            //World_AIW2.Instance.QueueChatMessageOrCommand( "planet map drag end " + this.ManagerCore.selectionViewportBounds.size, ChatType.ShowLocallyOnly, Engine_AIW2.Instance.MainThreadContext );

            //if we stopped holding it
            {
                if ( mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                { }
                else
                {
                    if ( !mouseEventFlags.Has( ArcenInputFlags.Additive ) )
                        Engine_AIW2.Instance.ClearSelection( true, true );
                }

                var activeSquads = BattlefieldVisualSingleton.Instance.ActiveSquads;
                
                bool militaryOnly = false;
                if ( !mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                    militaryOnly = true;
                
                bool turretsOnly = mouseEventFlags.Has( ArcenInputFlags.OnlyTurrets );
                
                for ( int outerLoop = 1; outerLoop < 100; outerLoop++ )
                {
                    bool addedAnything = false;
                    bool containedAnything = false;
                    
                    for ( int i = 0; i < activeSquads.Count; i++ )
                    {
                        var vis = activeSquads[i];
                        
			            var squad = vis.RelatedEntity.GetSquad();
                        if ( squad == null || !squad.GetMayBeSelected( UnitSelectionType.DragSelect ) )
                            continue;
                        
                        if ( turretsOnly && !squad.TypeData.IsTurret )
                            continue;

                        if ( militaryOnly && !squad.TypeData.IsMobileCombatant )
                            continue;

                        if ( vis.Gimbal != null && 
                             this.ManagerCore.IsWithinSelectionBounds( vis.Gimbal.UnityObject.gameObject ) )
                        {
                            containedAnything = true;
                            
                            if ( mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                            {
                                squad.Unselect( false, "SubtractiveMouseDrag" );
                            }
                            else
                            {
                                if ( !squad.GetIsSelected() ) 
                                    addedAnything = true;
                                
                                squad.Select( false, "AdditiveMouseDrag" );
                            }
                            
                            continue;
                        }
                        
                        bool didApplySelectLogic = false;
                        if ( this.ManagerCore.IsWithinSelectionBounds( vis.CurrentPosition ) )
                        {
                            containedAnything = true;
                            if ( mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                                squad.Unselect( false, "SubtractiveMouseDrag" );
                            else
                            {
                                if ( !squad.GetIsSelected() ) addedAnything = true;
                                squad.Select( false, "AdditiveMouseDrag" );
                            }
                            didApplySelectLogic = true;
                        }
                        
                        if ( didApplySelectLogic )
                            continue;
                    }
                    
                    if ( !militaryOnly )
                        break;
                    
                    if ( addedAnything )
                        break;
                    
                    if ( !mouseEventFlags.Has( ArcenInputFlags.Additive ) && containedAnything )
                        break;
                    
                    militaryOnly = false;
                }
            }
        }

        public static ArcenPoint LastSimPointForPlacementPurposes;

        public void HandleMouseCursor( ArcenPoint SimPointUnderCursor )
        {
            if ( SimPointUnderCursor == ArcenPoint.ZeroZeroPoint )
                return;
            LastSimPointForPlacementPurposes = SimPointUnderCursor;

            if ( Engine_Universal.IsMouseOverGUI )
                this.ClearWhenNotInThisMode();

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            
            // todo: make these all ITargetedInputAction(s)
            // wait... why is this needed?
            if ( Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() && localFactionOrNull != null )
            {
                Engine_AIW2.Instance.IsInPingLocationMode = false;
            }
            
            /*
            if ( Engine_AIW2.Instance.PendingTargetedAction != null )
            {
                Engine_AIW2.Instance.PendingTargetedAction.UpdateCursor(SimPointUnderCursor);
            }
            */
        }

        public void HandleMouseEvent_PlanetView( 
            ArcenMouseEventType EventType, ArcenInputFlags InputFlags, ArcenPoint SimPointUnderCursor, 
            GameEntity_Base EntityUnderCursor, bool InBandBoxMode, bool planetViewMouseHandlingDebugLog )
        {
            if ( GalaxyViewSelector.skipFurtherClicksUntil > ArcenTime.TimeSinceStartF )
                return;
            if ( SimPointUnderCursor == ArcenPoint.ZeroZeroPoint )
                return;

            LastSimPointForPlacementPurposes = ArcenPoint.ZeroZeroPoint;

            Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( currentPlanet == null )
                return;

            //World_AIW2.Instance.QueueChatMessageOrCommand( "Planet: " + EventType, ChatType.ShowLocallyOnly, Engine_AIW2.Instance.MainThreadContext );

            if ( Engine_AIW2.Instance.IsInPingLocationMode )
            {
                if ( Engine_Universal.IsMouseOverGUI )
                    return;

                switch (EventType)
                {
                    case ArcenMouseEventType.PrimaryDoubleUp:
                        return;
                    case ArcenMouseEventType.PrimaryDown:
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlanetPing], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedBool = false;
                        command.RelatedMagnitude = (int)InputCaching.CalculatePlanetPingColor();
                        command.RelatedIntegers.Add( currentPlanet.Index );
                        command.RelatedPoints.Add( SimPointUnderCursor );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        return;
                    case ArcenMouseEventType.SecondaryDoubleUp:
                    case ArcenMouseEventType.SecondaryDown:
                        Engine_AIW2.Instance.IsInPingLocationMode = false;
                        return;
                }                
            }

            GameEntity_Squad builder = null;
            {
                if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() && localFactionOrNull != null )
                {
                    DirectBuildable buildable = Engine_AIW2.Instance.PlacingDirectBuildable;
                    Engine_AIW2.Instance.IsInPingLocationMode = false;

                    if ( Engine_Universal.IsMouseOverGUI )
                        return;

                    PlanetFaction localFaction = currentPlanet.GetPlanetFactionForFaction( localFactionOrNull );
                    bool foundAny = false;
                    
                    builder = buildable.Builder;
                    if ( builder != null && builder.GetFactionTypeSafe() != FactionType.Player )
                        builder = null;
                    if ( builder != null && builder.Planet == currentPlanet )
                        foundAny = true;

                    if ( !foundAny )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "No builders available here, or all of the builders are crippled or nonfunctional for some other reason!", ChatType.ShowLocallyOnly, null );

                    // TODO: Add a method to DirectBuildable to check if we should cancel placement mode
                    // In particular, DirectBuild_CommandStation should use it to cancel placement mode
                    // once we've placed a command station.

                    if ( !foundAny )
                        Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                }
            }
            
            //World_AIW2.Instance.QueueChatMessageOrCommand( EventType + ": " + (EntityUnderCursor == null ? "null" : EntityUnderCursor.TypeData.InternalName) + " " +
            //    (EntityUnderCursor == null ? "nothing" : EntityUnderCursor.TypeData.OtherSpecialType.ToString()) + " " +
            //    InputFlags.Has( ArcenInputFlags.WormholeInteraction ), ChatType.ShowLocallyOnly, Engine_AIW2.Instance.MainThreadContext );
            switch ( EventType )
            {
                case ArcenMouseEventType.PrimaryDown:
                case ArcenMouseEventType.PrimaryDoubleUp:
                    {
                        bool isDoubleClick = EventType == ArcenMouseEventType.PrimaryDoubleUp;
                        if ( EntityUnderCursor == null ) //from Chris: this is pretty much a hack, but it reconciles some strange difference
                            EntityUnderCursor = GameEntity_Base.CurrentlyHoveredOver;

                        if ( GalaxyViewSelector.skipFurtherClicksUntil > ArcenTime.TimeSinceStartF )
                            break;

                        if ( Engine_AIW2.Instance.PendingTargetedAction != null )
                        {
                            bool done = Engine_AIW2.Instance.PendingTargetedAction.Click(EventType, SimPointUnderCursor, EntityUnderCursor);
                            if (done)
                                Engine_AIW2.Instance.PendingTargetedAction = null;
                        }
                        // todo: make these all ITargetedInputAction(s)
                        else 
                        if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() && builder != null && localFactionOrNull != null )
                        {
                            DirectBuildable buildable = Engine_AIW2.Instance.PlacingDirectBuildable;
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            if ( !isDoubleClick )
                            {
                                DirectBuildMode buildMode = DirectBuildMode.OneUnit;
                                if ( !buildable.TypeData.IsCommandStation )
                                {
                                    if ( InputCaching.inputBuildHalfUnits.CalculateIsKeyDownNow_IgnoreConflicts() )
                                    {
                                        buildMode = DirectBuildMode.HalfUnits;
                                    }
                                    else if ( InputCaching.inputBuildThirdUnits.CalculateIsKeyDownNow_IgnoreConflicts() )
                                    {
                                        buildMode = DirectBuildMode.ThirdUnits;
                                    }
                                    else if ( InputCaching.inputBuild5xUnits.CalculateIsKeyDownNow_IgnoreConflicts() )
                                    {
                                        if ( InputCaching.inputBuild10xUnits.CalculateIsKeyDownNow_IgnoreConflicts() )
                                            buildMode = DirectBuildMode.x50Units;
                                        else
                                            buildMode = DirectBuildMode.x5Units;
                                    }
                                    else if ( InputCaching.inputBuild10xUnits.CalculateIsKeyDownNow_IgnoreConflicts() )
                                        buildMode = DirectBuildMode.x10Units;
                                }
                                
                                ArcenRejectionReason foundAnyDisabled = builder.ComputeDisabledReason(ArcenRejectionReason.EntityIsInHoldFireMode );
                                if ( foundAnyDisabled != ArcenRejectionReason.Unknown )
                                {
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Builders present here cannot do the work because: " + foundAnyDisabled, ChatType.ShowLocallyOnly, null );
                                }
                                else
                                {
                                    GameCommand command = buildable.CreateBuildCommand( GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, SimPointUnderCursor, buildMode );
                                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                    return;
                                }
                            }
                        }
                        else 
                        if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null && 
                             localFactionOrNull != null)
                        {
                            bool debug = false;
                            bool outguardPendingDeployment = true;
                            if ( outguardPendingDeployment )
                            {
                                if ( Engine_Universal.IsMouseOverGUI )
                                    return;

                                OutguardGroupData group = Engine_AIW2.Instance.PlacingOutguardDeployable;

                                if ( !isDoubleClick )
                                {
                                    var playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                                    if ( playerFaction == null )
                                    {
                                        if ( debug )
                                        {
                                            ArcenDebugging.ArcenDebugLogSingleLine(
                                                "can't deploy outguard: no local player faction found",
                                                Verbosity.DoNotShow );
                                        }

                                        return;
                                    }

                                    if ( currentPlanet == null )
                                    {
                                        if ( debug )
                                        {
                                            ArcenDebugging.ArcenDebugLogSingleLine(
                                                "can't deploy outguard: no currentPlanet", Verbosity.DoNotShow );
                                        }

                                        return;
                                    }

                                    var spawnPoint = SimPointUnderCursor;
                                    if ( debug )
                                    {
                                        ArcenDebugging.ArcenDebugLogSingleLine( string.Format(
                                                "deploying outguard {0} at {1} on planet {2}",
                                                group.DisplayName, spawnPoint.ToString(), currentPlanet.Name ),
                                            Verbosity.DoNotShow );
                                    }

                                    GameCommand_QueueOutguard.QueueCommand( group, currentPlanet, SimPointUnderCursor,
                                        playerFaction );
                                    Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                                }
                            }
                        }
                        else 
                        if ( EntityUnderCursor != null && 
                             EntityUnderCursor.TypeData.OtherSpecialType == OtherSpecialEntityType.Wormhole && 
                             InputFlags.Has( ArcenInputFlags.WormholeInteraction ) )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            
                            GameEntity_Other wormhole = (GameEntity_Other)EntityUnderCursor;
                            Planet targetPlanet = wormhole.GetLinkedPlanet();
                            if ( targetPlanet != null )
                            {
                                if ( AIWar2GalaxySettingQuickAccess.HiddenGalaxy && targetPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                                {
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot move camera to " + targetPlanet.Name + ", because it has not been explored via scouting and" +
                                        " the \"Hide Unexplored Planets\" setting was enabled.", ChatType.ShowLocallyOnly, null );
                                    return;
                                }
                                //if ( targetPlanet.IntelLevel == PlanetIntelLevel.Unexplored )
                                //    World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot cross over to planet " + targetPlanet.Name + ", because it has not been explored via scouting.", Engine_AIW2.Instance.MainThreadContext );
                                //else
                                {
                                    Planet myPlanet = EntityUnderCursor.Planet;
                                    GameEntity_Other wormholeOnOtherEnd = targetPlanet.GetWormholeTo( myPlanet );
                                    World_AIW2.Instance.SwitchViewToPlanet( targetPlanet );
                                    Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( myPlanet );
                                    Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( wormholeOnOtherEnd, true );
                                    Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( targetPlanet );
                                    return;
                                }
                            }
                        }
                        else if ( !InBandBoxMode )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                            {
                                this.ClearWhenNotInThisMode();
                                return;
                            }
                            if ( isDoubleClick )
                                GalaxyViewSelector.skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;

                            GameEntity_Squad squadUnderCursor = null;
                            if ( EntityUnderCursor != null && EntityUnderCursor is GameEntity_Squad )
                                squadUnderCursor = (GameEntity_Squad)EntityUnderCursor;

                            Fleet fleetOrNull = squadUnderCursor == null ? null : squadUnderCursor.GetFleetOrNull_Safe();

                            #region instead of normal click behavior, show details
                            if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() && squadUnderCursor != null &&
                                EntityText.GetHasContentsToView( squadUnderCursor ) != null )
                            {
                                EntityText.ShowContents( squadUnderCursor );
                                return;
                            }
                            #endregion

                            //ArcenDebugging.ArcenDebugLog( EntityUnderCursor == null ? "null" : ( EntityUnderCursor.TypeData.Name + " " + EntityUnderCursor.GetMayBeSelected() ) , Verbosity.DoNotShow );
                            //ArcenDebugging.ArcenDebugLog( InputFlags.Has( ArcenInputFlags.Subtractive ) + "  " + InputFlags.Has( ArcenInputFlags.Additive ) , Verbosity.DoNotShow );

                            if ( InputFlags.Has( ArcenInputFlags.Subtractive ) )
                            {
                                if ( EntityUnderCursor != null && EntityUnderCursor.GetMayBeSelected( UnitSelectionType.DirectClick ) )
                                {
                                    if ( !isDoubleClick )
                                        EntityUnderCursor.Unselect( false, "SubtractiveSingleClick" );
                                    else
                                    {
                                        //deselect by fleet
                                        if ( squadUnderCursor != null && squadUnderCursor.TypeData.IsFleetLeader )
                                        {
                                            if ( fleetOrNull != null )
                                                fleetOrNull.IsConsideredSelected_NonSim = false;
                                            EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( squadUnderCursor.GetFleetOrNull_Safe(), true );
                                        }
                                        else //deselect by type
                                        {
                                            //this is ok to do at just the local planet
                                            PlanetFaction pFaction = EntityUnderCursor.PlanetFaction;
                                            if ( pFaction != null )
                                            {
                                                foreach ( GameEntity_Squad entity in pFaction.Entities.Squads() )
                                                {
                                                    if ( EntityUnderCursor.TypeData != entity.TypeData )
                                                        continue;
                                                    entity.Unselect( false, "SubtractiveDoubleClick" );
                                                }
                                            }
                                        }
                                    }
                                    return;
                                }
                            }
                            else
                            {
                                if ( !InputFlags.Has( ArcenInputFlags.Additive ) )
                                    Engine_AIW2.Instance.ClearSelection( true, true );
                                if ( EntityUnderCursor != null && EntityUnderCursor.GetMayBeSelected( UnitSelectionType.DirectClick ) )
                                {
                                    if ( !isDoubleClick )
                                        EntityUnderCursor.Select( false, "AdditiveSingleClick" );
                                    else
                                    {
                                        //select by fleet
                                        if ( squadUnderCursor != null && squadUnderCursor.TypeData.IsFleetLeader )
                                        {
                                            //deselect the individual ships
                                            EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( squadUnderCursor.GetFleetOrNull_Safe(), true );
                                            //select the fleet instead
                                            if ( fleetOrNull != null )
                                                fleetOrNull.MarkAsSelected();
                                        }
                                        else //select by type
                                        {
                                            //this is ok to do at just the local planet
                                            foreach ( GameEntity_Squad entity in EntityUnderCursor.PlanetFaction.Entities.Squads() )
                                            {
                                                if ( EntityUnderCursor.TypeData != entity.TypeData )
                                                    continue;
                                                entity.Select( false, "AdditiveDoubleClick" );
                                            }
                                        }
                                    }
                                    return;
                                }
                            }
                        }
                        else //any other case!
                        {
                            if ( isDoubleClick )
                                GalaxyViewSelector.skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;
                        }
                    }
                    break;
                case ArcenMouseEventType.SecondaryDown:
                //case ArcenMouseEventType.SecondaryDoubleUp:
                    {
                        //right-clicking in the planet view first cancels out of placement mode
                        if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                        }
                        //...or placement of outguards
                        else if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                        }
                        else if ( EntityUnderCursor == null )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            HandleSecondaryClickOnEmptySpacePoint( InputFlags, SimPointUnderCursor, currentPlanet );
                            return;
                        }
                        else
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;
                            bool hasDoneSomethingYet = false;
                            if ( EntityUnderCursor.TypeData.Category == GameEntityCategory.Ship )
                            {
                                SecondaryClickResult result = EndpointFunctions.SecondaryClickSquad( EntityUnderCursor as GameEntity_Squad );
                                switch ( result )
                                {
                                    case SecondaryClickResult.ClickedOnWhatMayAsWellBeEmptySpace:
                                        HandleSecondaryClickOnEmptySpacePoint( InputFlags, SimPointUnderCursor, currentPlanet );
                                        return; //treat the game as if you did not click on a unit at all
                                    case SecondaryClickResult.NothingWasThere:
                                        break; //just keep going, then
                                    case SecondaryClickResult.DidAnyNeededActions:
                                        return; //don't keep looking, in this case
                                }
                            }


                            if ( EntityUnderCursor.TypeData.OtherSpecialType == OtherSpecialEntityType.Wormhole && InputFlags.Has( ArcenInputFlags.WormholeInteraction ) )
                            {
                                GameEntity_Other wormhole = (GameEntity_Other)EntityUnderCursor;
                                Planet targetPlanet = wormhole.GetLinkedPlanet();
                                if ( targetPlanet != null )
                                {
                                    if ( targetPlanet.IntelLevel == PlanetIntelLevel.Unexplored )
                                        World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send ships to planet " + targetPlanet.Name + ", because it has not been explored via scouting.", ChatType.ShowLocallyOnly, null );
                                    else
                                    {
                                        workingSquadsThatNeedOtherKindsOfOrders.Clear();

                                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                        command.RelatedString = "PView_PlayerWClick";
                                        command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                                        command.ToBeQueued = InputFlags.Has( ArcenInputFlags.Additive );
                                        command.RelatedIntegers.Add( targetPlanet.Index );
                                        command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                                        int numberOfStationary = 0;
                                        int numberOfPlanetary = 0;
                                        int numberLoadingIntoFlagships = 0;
                                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                                        {
                                            hasDoneSomethingYet = true;
                                            if ( selected.DataForMark.Speed <= 0 )
                                            {
                                                numberOfStationary++;
                                                continue;
                                            }
                                            if ( selected.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                                            {
                                                numberOfPlanetary++;
                                                continue;
                                            }
                                            if ( selected.GetIsNonFlagshipInLoadingModeMode() )
                                            {
                                                numberLoadingIntoFlagships++;
                                                continue;
                                            }
                                            if ( selected.Planet == targetPlanet )
                                            {
                                                // if they are already ON that planet that is perfectly fine
                                                continue;
                                            }
                                            if ( selected.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                                            {
                                                var tuple = FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create(
                                                                SafeSquadWrapper.Create( selected ),
                                                                targetPlanet,
                                                                Engine_AIW2.Instance.CombatCenter,
                                                                true );

                                                workingSquadsThatNeedOtherKindsOfOrders.Add( tuple );

                                                continue;
                                            }

                                            command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                                        }

                                        bool couldSomePass = command.RelatedEntityIDs.Count > 0;
                                        if ( command.RelatedEntityIDs.Count > 0 )
                                        {
                                            //UnityEngine.Debug.Log( "wormhole:" + command.WriteToStringInefficient() );
                                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                        }
                                        else
                                        {
                                            command.ReturnToPool();
                                        }

                                        string reasons = string.Empty;
                                        if ( numberOfStationary > 0 )
                                        {
                                            if ( reasons.Length > 0 )
                                                reasons += ", ";
                                            reasons += numberOfStationary.ToString( "#,##0" ) + " are immobile";
                                        }
                                        if ( numberOfPlanetary > 0 )
                                        {
                                            if ( reasons.Length > 0 )
                                                reasons += ", ";
                                            reasons += numberOfPlanetary.ToString( "#,##0" ) + " are station-keepers";
                                        }
                                        if ( numberLoadingIntoFlagships > 0 )
                                        {
                                            if ( reasons.Length > 0 )
                                                reasons += ", ";
                                            reasons += numberLoadingIntoFlagships.ToString( "#,##0" ) + " are loading into a transport";
                                        }

                                        if ( reasons.Length > 0 )
                                        {
                                            if ( couldSomePass )
                                            {
                                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send some of your selected units to planet " + targetPlanet.Name +
                                                    ", because: " + reasons + ".", ChatType.ShowLocallyOnly, null );
                                            }
                                            else
                                            {
                                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send any of your selected units to planet " + targetPlanet.Name +
                                                    ", because: " + reasons + ".", ChatType.ShowLocallyOnly, null );
                                            }
                                        }

                                        HandleAnySquadsThatNeedOtherKindsOfOrders();
                                    }
                                }
                            }

                            if ( !hasDoneSomethingYet )
                                HandleSecondaryClickOnEmptySpacePoint( InputFlags, SimPointUnderCursor, currentPlanet );
                        }
                        break;
                    }
            }
        }

        internal static List<FourTuple<SafeSquadWrapper,Planet,ArcenPoint,bool>> workingSquadsThatNeedOtherKindsOfOrders = 
            List<FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>>.Create_WillNeverBeGCed( 3000, "PlanetViewSelector-workingSquadsThatNeedOtherKindsOfOrders" );

        private static void HandleSecondaryClickOnWormhole( ArcenInputFlags InputFlags, ArcenPoint SimPointUnderCursor, Planet planet )
        {
            bool isQueuedOrder = InputFlags.Has( ArcenInputFlags.Additive );

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            EndpointFunctions.BringRemoteShipsToPlanet ( World_AIW2.Instance.GetPlanetByIndex(Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed()), null, isQueuedOrder, InputFlags, pathingCacheData );
            pathingCacheData.ReturnToPool();

            /* Now handle the local move command for the remote ships */
            {
                GameCommand remoteCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_PlayerRemoteShipsToHere], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                remoteCommand.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                remoteCommand.ToBeQueued = true; //this is always queued, since we have some wormhole move commands already
                remoteCommand.RelatedPoints.Add( SimPointUnderCursor );
                remoteCommand.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;

                workingSquadsThatNeedOtherKindsOfOrders.Clear();

                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( e.Planet == planet )
                        continue;

                    if ( e.DataForMark.Speed <= 0 )
                        continue;

                    if ( e.GetIsNonFlagshipInLoadingModeMode() )
                        continue;

                    if ( e.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                    {
                        var tuple = FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create(
                                        SafeSquadWrapper.Create( e ),
                                        Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed(),
                                        SimPointUnderCursor, true );

                        workingSquadsThatNeedOtherKindsOfOrders.Add( tuple );

                        continue;
                    }

                    remoteCommand.RelatedEntityIDs.Add( e.PrimaryKeyID );
                }
                if ( remoteCommand.RelatedEntityIDs.Count > 0 )
                {
                    //UnityEngine.Debug.Log( "click:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), remoteCommand, true );
                }
                else //prevents having a leak!
                    remoteCommand.ReturnToPool();
            }

            /* Now issue the move command for the ships already at the planet */
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

                command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                command.ToBeQueued = isQueuedOrder;
                command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                command.RelatedPoints.Add( SimPointUnderCursor );

                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( e.Planet != planet )
                        continue; //skip anything that is NOT on our current planet
                    if ( e.DataForMark.Speed <= 0 )
                        continue;
                    if ( e.GetIsNonFlagshipInLoadingModeMode() )
                        continue;
                    if ( e.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                    {
                        //these types ignore all move orders

                        var tuple = FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create(
                                        SafeSquadWrapper.Create( e ),
                                        Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed(),
                                        SimPointUnderCursor, true );

                        workingSquadsThatNeedOtherKindsOfOrders.Add( tuple );

                        continue;
                    }

                    command.RelatedEntityIDs.Add( e.PrimaryKeyID );
                }
                if ( command.RelatedEntityIDs.Count > 0 )
                {
                    //UnityEngine.Debug.Log( "click:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
                else //prevents having a leak!
                    command.ReturnToPool();
            }

            HandleAnySquadsThatNeedOtherKindsOfOrders();
        }

        private static void HandleSecondaryClickOnEmptySpacePoint( ArcenInputFlags InputFlags, ArcenPoint SimPointUnderCursor, Planet planet )
        {
            bool isQueuedOrder = InputFlags.Has( ArcenInputFlags.Additive );

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            EndpointFunctions.BringRemoteShipsToPlanet ( World_AIW2.Instance.GetPlanetByIndex(Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed()), null, isQueuedOrder, InputFlags, pathingCacheData );
            pathingCacheData.ReturnToPool();

            /* Now handle the local move command for the remote ships */
            {
                GameCommand remoteCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_PlayerRemoteShipsToHere], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                remoteCommand.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                remoteCommand.ToBeQueued = true; //this is always queued, since we have some wormhole move commands already
                remoteCommand.RelatedPoints.Add( SimPointUnderCursor );
                remoteCommand.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;

                workingSquadsThatNeedOtherKindsOfOrders.Clear();

                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( e.Planet == planet )
                        continue;

                    if ( e.DataForMark.Speed <= 0 )
                        continue;

                    if ( e.GetIsNonFlagshipInLoadingModeMode() )
                        continue;

                    if ( e.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                    {
                        var tuple = FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create(
                                        SafeSquadWrapper.Create( e ),
                                        Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed(),
                                        SimPointUnderCursor, true );

                        workingSquadsThatNeedOtherKindsOfOrders.Add( tuple );

                        continue;
                    }

                    remoteCommand.RelatedEntityIDs.Add( e.PrimaryKeyID );
                }
                if ( remoteCommand.RelatedEntityIDs.Count > 0 )
                {
                    //UnityEngine.Debug.Log( "click:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), remoteCommand, true );
                }
                else //prevents having a leak!
                    remoteCommand.ReturnToPool();
            }

            /* Now issue the move command for the ships already at the planet */
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

                command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                command.ToBeQueued = isQueuedOrder;
                command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                command.RelatedPoints.Add( SimPointUnderCursor );

                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( e.Planet != planet )
                        continue; //skip anything that is NOT on our current planet
                    if ( e.DataForMark.Speed <= 0 )
                        continue;
                    if ( e.GetIsNonFlagshipInLoadingModeMode() )
                        continue;
                    if ( e.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                    {
                        //these types ignore all move orders

                        var tuple = FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create(
                                        SafeSquadWrapper.Create( e ),
                                        Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed(),
                                        SimPointUnderCursor, true );

                        workingSquadsThatNeedOtherKindsOfOrders.Add( tuple );

                        continue;
                    }

                    command.RelatedEntityIDs.Add( e.PrimaryKeyID );
                }
                if ( command.RelatedEntityIDs.Count > 0 )
                {
                    //UnityEngine.Debug.Log( "click:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
                else //prevents having a leak!
                    command.ReturnToPool();
            }

            HandleAnySquadsThatNeedOtherKindsOfOrders();
        }

        internal static void HandleAnySquadsThatNeedOtherKindsOfOrders()
        {
            if ( workingSquadsThatNeedOtherKindsOfOrders.Count <= 0 )
                return;

            for ( int i = 0; i < workingSquadsThatNeedOtherKindsOfOrders.Count; i++ )
            {
                FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool> four = workingSquadsThatNeedOtherKindsOfOrders[i];
                GameEntity_Squad squad = four.FirstItem.GetSquad();
                if ( squad == null )
                    continue;

                if ( squad.DataForMark.Speed <= 0 || squad.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary ||
                    squad.GetIsNonFlagshipInLoadingModeMode() )
                    continue; //these ones don't get free movement!
                
                if ( squad != null && squad.TypeData.AlternativeMoveOrderHandler != null )
                    squad.TypeData.AlternativeMoveOrderHandler.MoveToPlanetOrLocation( squad, four.SecondItem, four.ThirdItem, four.FourthItem );
            }
        }

        /// <summary>
        /// This is unusually simplistic, because there is really only one.
        /// Frankly this is a success if there is a cast to a PlanetViewSelector.
        /// </summary>
        public bool GetIsShapeOwner( IArcenShapeOwner Other )
        {
            if ( Other is PlanetViewSelector otherSelector )
                return otherSelector == this;
            return false;
        }
    }
}
