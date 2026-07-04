using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;

using Arcen.Universal.Sprites;
using Arcen.Universal.Sprites.Instanced;
using MeshSprites = Arcen.Universal.Sprites.MeshBased;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyViewSelector : IUpdateCycler
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "GalaxyViewSelectors" );
        public GalaxyViewSelector()
        {
            RefTracker.IncrementObjectCount();
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            this.ClearWhenNotInThisMode();
        }

        public GalaxyMapSelectionManager ManagerCore;
                
        public void ClearWhenNotInThisMode()
        {
        }

        private Planet planetUnderCursor = null;
        private GameEntity_Squad squadUnderCursor = null;
        private PlanetLinkMarker linkUnderCursor = null;

        private readonly List<ArcenMouseEventType> currentMouseEvents = List<ArcenMouseEventType>.Create_WillNeverBeGCed( 40, "GalaxyViewSelector-currentMouseEvents" );
        public void RunUpdate( IUpdateSource Source, bool CanDoRaycasts, bool CanDoClickHandling )
        {
            this.ManagerCore = (GalaxyMapSelectionManager)Source;

            #region Early-outs
            if ( !ArcenMainGameVisuals.GalaxyViewCamera )
                return;
            if ( ArcenInput.GetIsInputBlocked() )
            {
                this.ClearWhenNotInThisMode();
                return;
            }
            if ( !this.ManagerCore )
                return;
            #endregion

            if ( !InputActionTypeDataTable.IsInitialized )
                return;
            InputCaching.UpdateInputCacheIfNeeded();
            if ( World_AIW2.Instance == null )
                return;

            currentMouseEvents.Clear();
            ArcenInputFlags mouseEventFlags = ArcenInputFlags.None;
            bool hadPrimaryDownEvent = false;
            bool ordersToStationaryFlagships = InputCaching.inputHoldToGiveOrdersToStationaryFlagships.CalculateIsKeyDownNow_IgnoreConflicts();

            #region setting mouseEventFlags based on what's being held down
            if ( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Additive );
            if ( InputCaching.inputRemoveFromSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Subtractive );
            if ( ordersToStationaryFlagships )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes );
            #endregion

            #region set mouseEventType, and pick the layer mask for the squadUnderCursor raycast
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
                }
                
                if ( InputCaching.inputSelectUnit.CalculateIsSinglePress() )
                {
                    hadPrimaryDownEvent = true;
                    currentMouseEvents.Add( ArcenMouseEventType.PrimaryDown );
                }
                
                if ( InputCaching.inputGiveOrdersToUnit.CalculateIsDoubleRelease() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.SecondaryDoubleUp );
                }
                
                if ( InputCaching.inputGiveOrdersToUnit.CalculateIsSinglePress() )
                {
                    currentMouseEvents.Add( ArcenMouseEventType.SecondaryDown );
                }
            }
            #endregion
            //Engine_Universal.EndProfilerSample( "simPointUnderCursor" );

            if ( CanDoRaycasts )
            {
                planetUnderCursor = null;
                squadUnderCursor = null;
                linkUnderCursor = null;

                #region squadUnderCursor raycast
                if ( Engine_Universal.IsMouseOverGUI )
                {
                    squadUnderCursor = null;

                    //Engine_Universal.BeginProfilerSample( "DoIsSelectingLogic" );
                    //Engine_Universal.EndProfilerSample( "DoIsSelectingLogic" );

                    switch (GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType)
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
                    //Engine_Universal.BeginProfilerSample( "ManagerCore.RaycastAgainstAll" );
                    int numberHitsFound = this.ManagerCore.RaycastAgainstAll( ManagerCore.hoverLayers );
                    bool fullyFoundHit = false;
                    if ( numberHitsFound > 0 )
                    {
                        //note: the length of this array is much larger than the number of hits, since it is an array that is reused
                        RaycastHit[] rayHitArray = this.ManagerCore.raycastHitArray;
                        for ( int i = 0; i < numberHitsFound; i++ )
                        {
                            GameObject raycastHitGameObject = rayHitArray[i].transform.gameObject;
                            ArcenGalaxyMapIcon mapIcon = raycastHitGameObject.GetComponentInParent<ArcenGalaxyMapIcon>();
                            if ( mapIcon != null )
                            {
                                if ( !mapIcon.enabled )
                                    continue; //skip any that are disabled

                                if ( World_AIW2.Instance.InSetupPhase ) //if in the lobby
                                {
                                    GalaxyMapPlanet visualObject = mapIcon.LastKnownLinkedPlanet;
                                    if ( visualObject != null )
                                    {
                                        planetUnderCursor = visualObject.RelatedPlanet;
                                        fullyFoundHit = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if ( mapIcon.RelatedEntityOrNull != null )
                                    {
                                        squadUnderCursor = mapIcon.RelatedEntityOrNull;
                                        fullyFoundHit = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if ( !fullyFoundHit )
                        {
                            for ( int i = 0; i < numberHitsFound; i++ )
                            {
                                GameObject raycastHitGameObject = rayHitArray[i].transform.gameObject;
                                GalaxyMapPlanet visualObject = raycastHitGameObject.GetComponentInParent<GalaxyMapPlanet>();
                                if ( visualObject != null )
                                {
                                    planetUnderCursor = visualObject.RelatedPlanet;
                                    fullyFoundHit = true;
                                    break;
                                }
                            }
                        }
                    }

                    if ( !fullyFoundHit && !World_AIW2.Instance.InSetupPhase ) //not in the lobby!
                    {
                        numberHitsFound = this.ManagerCore.RaycastAgainstAll( ManagerCore.planetLinkLayerMaskOnly );
                        if ( numberHitsFound > 0 )
                        {
                            //note: the length of this array is much larger than the number of hits, since it is an array that is reused
                            RaycastHit[] rayHitArray = this.ManagerCore.raycastHitArray;
                            for ( int i = 0; i < numberHitsFound; i++ )
                            {
                                GameObject raycastHitGameObject = rayHitArray[i].transform.gameObject;
                                PlanetLinkMarker planetLink = raycastHitGameObject.GetComponentInParent<PlanetLinkMarker>();
                                if ( planetLink != null )
                                {
                                    linkUnderCursor = planetLink;
                                    break;
                                }
                            }
                        }
                    }
                    //Engine_Universal.EndProfilerSample( "ManagerCore.RaycastAgainstAll" );
                }
                #endregion
            }

            if ( CanDoClickHandling )
            {
                if ( !Engine_Universal.IsMouseOverGUI )
                {
                    if ( squadUnderCursor != null )
                    {
                        GameEntity_Base.SetCurrentlyHoveredOver( squadUnderCursor, FromSidebarType.NonSidebar_SingleUnit );
                        Planet.SetCurrentlyHoveredOver( null );
                        GalaxyMapPlanetLink.CurrentlyHoveredOver = null;
                    }
                    else
                    {
                        GameEntity_Base.SetCurrentlyHoveredOver( null, FromSidebarType.NonSidebar_SingleUnit );
                        if ( planetUnderCursor != null )
                        {
                            GalaxyMapPlanetLink.CurrentlyHoveredOver = null;
                            Planet.SetCurrentlyHoveredOver( planetUnderCursor );
                        }
                        else
                        {
                            Planet.SetCurrentlyHoveredOver( null );
                            if ( linkUnderCursor != null )
                                GalaxyMapPlanetLink.CurrentlyHoveredOver = linkUnderCursor.Link;
                            else
                                GalaxyMapPlanetLink.CurrentlyHoveredOver = null;
                        }
                    }
                }

                //Engine_Universal.DebugText = ( squadUnderCursor == null ? "null" : squadUnderCursor.TypeData.InternalName ) + " " + System.ArcenTime.Now;

                bool isBandBoxMode = this.ManagerCore.isSelecting && Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePositionAtStartOfSelection, Input.mousePosition ).size.x > 0;
                foreach ( ArcenMouseEventType mouseEventType in currentMouseEvents )
                    HandleMouseEvent_GalaxyView( mouseEventType, mouseEventFlags, planetUnderCursor, squadUnderCursor, linkUnderCursor, isBandBoxMode );
            }

            //no bandbox in setup mode
            if ( World_AIW2.Instance.InSetupPhase )
            {
                this.ManagerCore.isSelecting = false;
                return;
            }

            if ( !InputCaching.inputSelectUnit.CalculateIsKeyDownNow_IgnoreConflicts() &&
                ManagerCore.isSelecting )
            {
                this.ManagerCore.isSelecting = false;
                //we let go of it!
                this.DoIsSelectingLogic_GalaxyMapBoxDrag( mouseEventFlags );
                return;
            }

            #region bounding-box selection stuff
            if ( !Engine_Universal.IsMouseOverGUI && !this.ManagerCore.isSelecting &&
                InputCaching.inputSelectUnit.CalculateIsKeyDownNow_IgnoreConflicts() )
            {
                this.ManagerCore.isSelecting = true;
                this.ManagerCore.mousePositionAtStartOfSelection = Input.mousePosition;
            }

            if ( this.ManagerCore.isSelecting )
            {
                //Engine_Universal.BeginProfilerSample( "DoIsSelectingLogic" );
                //this.DoIsSelectingLogic_GalaxyMapBoxDrag( mouseEventFlags );
                //Engine_Universal.EndProfilerSample( "DoIsSelectingLogic" );
                if ( !hadPrimaryDownEvent )
                {
                    //bool isBandBoxMode = this.ManagerCore.isSelecting && Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePositionAtStartOfSelection, Input.mousePosition ).size.x > 0;
                    //HandleMouseEvent_GalaxyView( ArcenMouseEventType.None, mouseEventFlags, planetUnderCursor, squadUnderCursor, linkUnderCursor, isBandBoxMode );
                }
            }
            #endregion
        }

        public void DoIsSelectingLogic_GalaxyMapBoxDrag( ArcenInputFlags mouseEventFlags )
        {
            this.ManagerCore.selectionViewportBounds = Utils.GetViewportBounds( ArcenMainGameVisuals.MainViewCamera, this.ManagerCore.mousePositionAtStartOfSelection, Input.mousePosition );

            if ( this.ManagerCore.selectionViewportBounds.size.x <= 0 )
                return;

            //World_AIW2.Instance.QueueChatMessageOrCommand( "galaxy map drag end " + this.ManagerCore.selectionViewportBounds.size, ChatType.ShowLocallyOnly, Engine_AIW2.Instance.MainThreadContext );

            {
                if ( mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                { }
                else
                {
                    if ( !mouseEventFlags.Has( ArcenInputFlags.Additive ) )
                        Engine_AIW2.Instance.ClearSelection( true, true );
                }
                
                //bool militaryOnly = false;
                //if ( !mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                //    militaryOnly = true;
                for ( int outerLoop = 1; outerLoop < 100; outerLoop++ )
                {
                    bool addedAnything = false;
                    bool containedAnything = false;
                    foreach ( KeyValuePair<Int16, GalaxyMapPlanet> kv in GalaxyMapManager.AllDisplayPlanetsByIndex )
                    {                        
                        GalaxyMapPlanet planetVis = kv.Value;

                        for ( int i = 0; i < planetVis.OtherMapIcons.Length; i++ )
                        {
                            ArcenGalaxyMapIcon icon = planetVis.OtherMapIcons[i];
                            if ( icon == null || !icon.BoxColl.enabled )
                                continue;
                            GameEntity_Squad ship = icon.RelatedEntityOrNull;
                            if ( ship == null || !ship.GetMayBeSelected( UnitSelectionType.DragSelect ) )
                                continue;
                            //if ( militaryOnly && !squad.RelatedEntity.GetMatches( EntityRollupType.MobileCombatants ) )
                            //    continue;
                            if ( this.ManagerCore.IsWithinSelectionBounds( icon.gameObject ) )
                            {
                                containedAnything = true;
                                Fleet fleetOrNull = ship.GetFleetOrNull_Safe();
                                if ( mouseEventFlags.Has( ArcenInputFlags.Subtractive ) )
                                {
                                    //deselect by fleet ALWAYS in galaxy map mode
                                    if ( ship.TypeData.IsFleetLeader )
                                    {
                                        if ( fleetOrNull != null )
                                            fleetOrNull.IsConsideredSelected_NonSim = false;
                                        EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( ship.GetFleetOrNull_Safe(), true );
                                    }
                                    else
                                        ship.Unselect( false, "SubtractiveGalaxyMapDrag" );
                                }
                                else
                                {
                                    if ( !ship.GetIsSelected() )
                                    {
                                        addedAnything = true;
                                        //select by fleet ALWAYS in the galaxy map if it's a fleet leader
                                        if ( ship.TypeData.IsFleetLeader )
                                        {
                                            EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( ship.GetFleetOrNull_Safe(), true );
                                            //select the fleet instead
                                            if ( fleetOrNull != null )
                                                fleetOrNull.MarkAsSelected();
                                        }
                                        else
                                            ship.Select( false, "AdditiveGalaxyMapDrag" );
                                    }
                                }
                                continue;
                            }
                        }
                    }
                    //if ( !militaryOnly )
                    //    break;
                    if ( addedAnything )
                        break;
                    if ( !mouseEventFlags.Has( ArcenInputFlags.Additive ) && containedAnything )
                        break;
                    //militaryOnly = false;
                }
            }
        }

        public static float skipFurtherClicksUntil = 0f;

        private static InputActionTypeData inputMakePlanetClickSelectAndSwitchView = null;

        public static void HandleMouseEvent_GalaxyView( ArcenMouseEventType EventType, ArcenInputFlags InputFlags, Planet PlanetUnderCursor, GameEntity_Base EntityUnderCursor, PlanetLinkMarker LinkUnderCursor, bool InBandBoxMode )
        {
            //World_AIW2.Instance.QueueChatMessageOrCommand( "Galaxy: " + EventType, ChatType.ShowLocallyOnly, Engine_AIW2.Instance.MainThreadContext );

            if ( Engine_AIW2.Instance.IsInPingLocationMode )
            {
                switch ( EventType )
                {
                    case ArcenMouseEventType.PrimaryDoubleUp:
                        return;
                    case ArcenMouseEventType.PrimaryDown:
                        if ( PlanetUnderCursor != null && !Engine_Universal.IsMouseOverGUI )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlanetPing], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedMagnitude = (int)InputCaching.CalculatePlanetPingColor();
                            command.RelatedBool = true;
                            command.RelatedIntegers.Add( PlanetUnderCursor.Index );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        return;
                    case ArcenMouseEventType.SecondaryDoubleUp:
                    case ArcenMouseEventType.SecondaryDown:
                        if ( !Engine_Universal.IsMouseOverGUI )
                            Engine_AIW2.Instance.IsInPingLocationMode = false;
                        return;
                }
            }

            if ( inputMakePlanetClickSelectAndSwitchView == null )
            {
                inputMakePlanetClickSelectAndSwitchView = InputActionTypeDataTable.GetActionByName_FairlySlow( "MakePlanetClickSelectAndSwitchView" );
            }

            switch ( EventType )
            {
                case ArcenMouseEventType.PrimaryDown:
                case ArcenMouseEventType.PrimaryDoubleUp:
                    {
                        #region Setup Phase
                        if ( World_AIW2.Instance.InSetupPhase )
                        {
                            if ( PlanetUnderCursor == null )
                                return;

                            List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;

                            ConfigurationForFaction selectingPlanetForFaction = Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby;
                            if ( selectingPlanetForFaction == null || selectingPlanetForFaction.SpecialFactionData.Type != FactionType.Player )
                            {
                                selectingPlanetForFaction = null;
                                for ( int i = 0; i < factionConfigs.Count; i++ )
                                {
                                    ConfigurationForFaction factionConfig = factionConfigs[i];
                                    if ( factionConfig.SpecialFactionData.Type == FactionType.Player && factionConfig.IsFactionControlledByLocalPlayer )
                                    {
                                        selectingPlanetForFaction = factionConfig;
                                        break;
                                    }
                                }
                            }

                            if ( selectingPlanetForFaction == null || selectingPlanetForFaction.FactionIndex <= 0 )
                            {
                                Engine_Universal.WriteToLocalMomentaryDisplayLog( "你目前是旁观者，且未在阵营标签页中选中任何阵营，因此点击星球无效。", null );
                                return;
                            }

                            Planet planet = PlanetUnderCursor;
                            if ( planet == null )
                                return; //simply be silent on this one

                            string invalidReason = planet.GetPlanetInvalidReason_StartingWorldForFaction( selectingPlanetForFaction );
                            if ( invalidReason.Length > 0 )
                            {
                                Engine_Universal.WriteToLocalMomentaryDisplayLog( "无法分配起始星球，原因：" + invalidReason, null );
                                return;
                            }

                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "StartingIndexChanges";
                            command.RelatedIntegers.Add( selectingPlanetForFaction.FactionIndex ); //FactionIndex
                            command.RelatedIntegers2.Add( planet.Index ); //StartingIndex
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                            return;
                        }
                        #endregion

                        #region Clicking A Planet When NO Entity Hovered
                        if ( PlanetUnderCursor != null && EntityUnderCursor == null && !Engine_Universal.IsMouseOverGUI )
                        {
                            World_AIW2.Instance.SwitchViewToPlanet( PlanetUnderCursor );
                            if ( inputMakePlanetClickSelectAndSwitchView.CalculateIsKeyDownNow_IgnoreConflicts() || EventType == ArcenMouseEventType.PrimaryDoubleUp )
                            {
                                Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.MainGameView );
                                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnPoint( PlanetUnderCursor, Engine_AIW2.Instance.CombatCenter, true );
                                Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( PlanetUnderCursor );
                                skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;
                            }
                            return;
                        }
                        #endregion

                        bool isDoubleClick = EventType == ArcenMouseEventType.PrimaryDoubleUp;
                        //if ( EntityUnderCursor == null ) //from Chris: this is pretty much a hack, but it reconciles some strange difference
                        //    EntityUnderCursor = GameEntity_Base.CurrentlyHoveredOver;

                        if ( skipFurtherClicksUntil > ArcenTime.TimeSinceStartF )
                            break;

                        if ( !InBandBoxMode )
                        {
                            if ( Engine_Universal.IsMouseOverGUI )
                                return;

                            if ( isDoubleClick )
                                skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;

                            GameEntity_Squad squadUnderCursor = EntityUnderCursor as GameEntity_Squad;

                            Fleet fleetOrNull = squadUnderCursor?.GetFleetOrNull_Safe();

                            #region instead of normal click behavior, show details
                            if (InputCaching.CalculateHoldAndClickToViewDetailsOfContents())
                            {
                                if (squadUnderCursor != null &&
                                    EntityText.GetHasContentsToView(squadUnderCursor) != null)
                                {
                                    EntityText.ShowContents(squadUnderCursor);
                                    return;
                                }

                                if (LinkUnderCursor != null)
                                {
                                    Planet firstPlanet = LinkUnderCursor.Link.PlanetOne?.RelatedPlanet;
                                    Planet secondPlanet = LinkUnderCursor.Link.PlanetTwo?.RelatedPlanet;

                                    List<PlannedWave> waves = PlannedWave.GetTemporaryPlannedWaveList( "GalaxyViewSelector-waves", 10f);
                                    if ( waves == null ) //blocked for teardown/shutdown; bail
                                        return;

                                    foreach ( PlannedWave wave in WaveUtils.KnownWavesAgainstHumanWorlds )
                                    {
                                        Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                                        Planet sourcePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                                        if ( sourcePlanet == firstPlanet && targetPlanet == secondPlanet
                                             || sourcePlanet == secondPlanet && targetPlanet == firstPlanet)
                                        {
                                            waves.Add( wave );
                                        }
                                    }

                                    if ( waves.Count > 0 )
                                    {
                                        float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                                        Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.25f, 2f, "攻击波次中的舰队详情", "关闭",
                                            buffer =>
                                            {
                                                bool result = true;
                                                PlannedWave wave;
                                                for ( int i = 0; i < waves.Count; i++ )
                                                {
                                                    wave = waves[i];
                                                    result &= Window_NotificationsDisplay.btnNotification.WriteDetailsOfAWaveContents( buffer, wave, centerPopupScale );
                                                }

                                                return result;
                                            } );

                                        PlannedWave.ReleaseTemporaryPlannedWaveList( waves );
                                        return; 
                                    }
                                    PlannedWave.ReleaseTemporaryPlannedWaveList( waves );
                                }
                            }
                            #endregion

                            //if double-clicking any unit on the galaxy map, switch to that planet and focus on it
                            if ( isDoubleClick && squadUnderCursor != null )
                            {
                                World_AIW2.Instance.SwitchViewToPlanet( squadUnderCursor.Planet );
                                Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.MainGameView );
                                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnPoint( squadUnderCursor.Planet, squadUnderCursor.WorldLocation, true );
                                Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( squadUnderCursor.Planet );
                                skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;
                                return;
                            }

                            //ArcenDebugging.ArcenDebugLog( EntityUnderCursor == null ? "null" : ( EntityUnderCursor.TypeData.Name + " " + EntityUnderCursor.GetMayBeSelected() ) , Verbosity.DoNotShow );
                            //ArcenDebugging.ArcenDebugLog( InputFlags.Has( ArcenInputFlags.Subtractive ) + "  " + InputFlags.Has( ArcenInputFlags.Additive ) , Verbosity.DoNotShow );

                            if ( InputFlags.Has( ArcenInputFlags.Subtractive ) )
                            {
                                if ( EntityUnderCursor != null && EntityUnderCursor.GetMayBeSelected( UnitSelectionType.DirectClick ) && !isDoubleClick )
                                {
                                    //don't care if this is a double-click or not
                                    //deselect by fleet ALWAYS in galaxy map mode
                                    if ( squadUnderCursor != null && ( squadUnderCursor.TypeData.IsFleetLeader && squadUnderCursor.GetFactionTypeSafe() == FactionType.Player ) )
                                    {
                                        if ( fleetOrNull != null )
                                            fleetOrNull.IsConsideredSelected_NonSim = false;
                                        EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( squadUnderCursor.GetFleetOrNull_Safe(), true );
                                    }
                                    else
                                        EntityUnderCursor.Unselect( false, "SubtractiveSingleClick" );
                                    return;
                                }
                            }
                            else
                            {
                                if ( !InputFlags.Has( ArcenInputFlags.Additive ) )
                                    Engine_AIW2.Instance.ClearSelection( true, true );
                                if ( EntityUnderCursor != null && EntityUnderCursor.GetMayBeSelected( UnitSelectionType.DirectClick ) && !isDoubleClick )
                                {
                                    //don't care if this is a double-click or not
                                    //select by fleet ALWAYS in the galaxy map if it's a fleet leader
                                    if ( squadUnderCursor != null && (squadUnderCursor.TypeData.IsFleetLeader && squadUnderCursor.GetFactionTypeSafe() == FactionType.Player) )
                                    {
                                        //deselect the individual ships
                                        EndpointFunctions.DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( squadUnderCursor.GetFleetOrNull_Safe(), true );
                                        //select the fleet instead
                                        if ( fleetOrNull != null )
                                            fleetOrNull.MarkAsSelected();
                                    }
                                    else
                                        EntityUnderCursor.Select( false, "AdditiveSingleClick" );
                                    return;
                                }
                            }
                        }
                        else //any other case!
                        {
                            if ( isDoubleClick )
                                skipFurtherClicksUntil = ArcenTime.TimeSinceStartF + 0.3f;
                        }
                    }
                    break;
                case ArcenMouseEventType.SecondaryDown:
                case ArcenMouseEventType.SecondaryDoubleUp:
                    {
                        if ( World_AIW2.Instance.InSetupPhase )
                            return;
                        if ( Engine_Universal.IsMouseOverGUI )
                            return;

                        GameEntity_Squad squadUnderCursor = EntityUnderCursor as GameEntity_Squad;
                        if ( squadUnderCursor != null )
                        {
                            SecondaryClickResult result = EndpointFunctions.SecondaryClickSquad( squadUnderCursor );
                            switch ( result )
                            {
                                case SecondaryClickResult.ClickedOnWhatMayAsWellBeEmptySpace:
                                    break; //just keep going, then
                                case SecondaryClickResult.NothingWasThere:
                                    break; //just keep going, then
                                case SecondaryClickResult.DidAnyNeededActions:
                                    return; //don't keep looking, in this case
                            }
                        }

                        if ( PlanetUnderCursor != null )
                        {
                            if ( PlanetUnderCursor.IntelLevel == PlanetIntelLevel.Unexplored )
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand( "无法向星球" + PlanetUnderCursor.Name + "派遣舰队，因为它尚未通过侦察探索。", ChatType.ShowLocallyOnly, null );
                                return;
                            }
                            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            if ( localFaction == null ) //for spectator mode, no clicks here
                                return;

                            //Since a selection can be split across multiple planets, we need to
                            //generate different paths (and a SetWormholePath move command) for the ships on each
                            //different planet

                            DictionaryOfLists<Planet, SafeSquadWrapper> entitiesOnDifferentPlanets = Planet.GetTemporaryPlanetDictOfSquadLists( "GalaxyViewSelector-RightClick-entitiesOnDifferentPlanets", 10f );
                            if ( entitiesOnDifferentPlanets == null ) //blocked for teardown/shutdown; bail
                                return;

                            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                            {
                                Planet destinationOrCurrentPlanet = selected.Planet;
                                if ( InputFlags.Has( ArcenInputFlags.Additive ) )
                                    destinationOrCurrentPlanet = selected.GetDestinationPlanet();
                                entitiesOnDifferentPlanets[destinationOrCurrentPlanet].Add( selected );
                            }
                            int pairCount = entitiesOnDifferentPlanets.GetCountOfLists();
                            if ( pairCount == 0 )
                            {
                                Planet.ReleaseTemporaryPlanetDictOfSquadLists( entitiesOnDifferentPlanets );
                                break;
                            }
                            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
                            try
                            {
                                // fill out an order, in batches per their current planet
                                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in entitiesOnDifferentPlanets )
                                {
                                    Planet destinationOrCurrentPlanet = pair.Key;

                                    GameCommand command = null;
                                    PathingMode mode = PathingHelper.GetPathingModeForLocalPlayer();
                                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( localFaction, "GalaxyViewSelectorHandleMouseEvent_GalaxyView",
                                        destinationOrCurrentPlanet, PlanetUnderCursor, mode, Engine_AIW2.Instance.MainThreadContext_ClientOrHost, pathingCacheData );

                                    //This typically means the GameEntity is already on the planet
                                    if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
                                    {
                                        command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                        command.RelatedString = "GalView_PlayerDirect";
                                        for ( int j = 0; j < pair.Value.Count; j++ )
                                            command.RelatedEntityIDs.Add( pair.Value[j].GetPrimaryKeyID() );
                                        if ( command.RelatedEntityIDs.Count > 0 )
                                        {
                                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                            //ArcenDebugging.ArcenDebugLogSingleLine( "ooh, we had " + command.RelatedEntityIDs.Count + 
                                            //    " ships already at this planet that were headed elsewhere and now have no orders.", Verbosity.ShowAsError );
                                        }
                                        else
                                            command.ReturnToPool();

                                        continue;
                                    }

                                    //Normal case where we have planets to traverse to reach the destination

                                    command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                    command.RelatedString = "GalView_PlayerDirect";
                                    command.ToBeQueued = InputFlags.Has( ArcenInputFlags.Additive );
                                    command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                                    for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                                        command.RelatedIntegers.Add( pathCache.PathToReadOnly[j].Index );
                                    
                                    PlanetViewSelector.workingSquadsThatNeedOtherKindsOfOrders.Clear();
                                    foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                                    {
                                        if ( selected.DataForMark.Speed <= 0 )
                                            continue;

                                        //spire relics, etc
                                        //some ships have custom code handling move orders
                                        //collect them to be handled later, below
                                        if ( selected.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                                        {
                                            PlanetViewSelector.workingSquadsThatNeedOtherKindsOfOrders.Add( FourTuple<SafeSquadWrapper, Planet, ArcenPoint, bool>.Create( SafeSquadWrapper.Create( selected ), PlanetUnderCursor,
                                                Engine_AIW2.Instance.CombatCenter, true ) );
                                            continue;
                                        }

                                        if ( selected.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                                            continue;

                                        //if this ship is already going to the planet (additive version, so track destination planets, not current planets)
                                        if ( InputFlags.Has( ArcenInputFlags.Additive ) && selected.GetDestinationPlanet() != destinationOrCurrentPlanet )
                                            continue;

                                        //if this is not additive, check the ship's current planet
                                        // jcf: we are iterating entities collected FROM our selection already though??
                                        if ( !InputFlags.Has( ArcenInputFlags.Additive ) && selected.Planet != pair.Key )
                                            continue;

                                        command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                                    }

                                    if ( command.RelatedEntityIDs.Count > 0 )
                                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                    else
                                        command.ReturnToPool();

                                    // handle entities collected
                                    // that have custom code for move orders
                                    PlanetViewSelector.HandleAnySquadsThatNeedOtherKindsOfOrders();
                                }
                            }
                            catch ( Exception e )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( "Exeption in HandleMouseEvent_GalaxyView right-click code: " + e, Verbosity.ShowAsError );
                            }
                            finally
                            {
                                pathingCacheData.ReturnToPool();
                                Planet.ReleaseTemporaryPlanetDictOfSquadLists( entitiesOnDifferentPlanets );
                            }
                        }
                    }
                    break;
            }
        }

    }
}
