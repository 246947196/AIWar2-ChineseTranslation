using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using static Arcen.AIW2.External.Window_InGameSidebarShips;

namespace Arcen.AIW2.External
{
    public abstract class BaseInputHandler : IInputActionHandler
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseInputHandlers" );
        public BaseInputHandler()
        {
            RefTracker.IncrementObjectCount();
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            if (Window_ErrorReportMenu.Instance.GetShouldDrawThisFrame_Subclass())
            {
                Window_ErrorReportMenu.Instance.Handle( Int1, InputActionType );
                return;
            }
            
            var windows = ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers;
            if ( windows.Count > 0 )
            {
                for ( int i = windows.Count-1; i >= 0; i-- )
                {
                    var windowAsType = windows[i] as IInputActionHandler;
                    if ( windowAsType == null )
                        continue;
                    
                    windowAsType.Handle( Int1, InputActionType );
                    if (ArcenInput.GetIsInputBlocked())
                        return;
                }
                
                if (InputActionType.Handling == InputHandlingType.Normal)
                    return;
            }
            
            windows = ArcenUI.CurrentlyShownWindows_ThatAllowNormalNormalInputHandlers;
            if ( windows.Count > 0 )
            {
                for ( int i = windows.Count-1; i >= 0; i-- )
                {
                    var windowAsType = windows[i] as IInputActionHandler;
                    if ( windowAsType == null )
                        continue;
                    
                    windowAsType.Handle( Int1, InputActionType );
                    if (ArcenInput.GetIsInputBlocked())
                        return;
                }
            }
            
            this.HandleInner( Int1, InputActionType );
        }

        public abstract void HandleInner(Int32 Int1, InputActionTypeData InputActionType );
    }

    public class Input_MainHandler : BaseInputHandler
    {
        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
            {
                //ArcenDebugging.ArcenDebugLog( InputActionType.InternalName, Verbosity.ShowAsError );
                return;
            }
            bool SidebarKeybindingsAreToggles = GameSettings.Current.GetBoolBySetting("SidebarKeybindingsAreToggles");

            string InputActionInternalName = InputActionType.InternalName;
            switch ( InputActionInternalName )
            {
                #region Development Tools
                case "DebugConnectToLocalServer":
                    //implementation removed, handled through main menu now
                    break;           
                #endregion
                case "ToggleGalaxyMap":
                    EndpointFunctions.ToggleGalaxyMap();
                    break;
                case "ResetCameraRotationAndTilt":
                    Engine_AIW2.Instance.PresentationLayer.ResetMainCameraOrientation();
                    break;
                case "TogglePause":
                case "TogglePauseAlt":
                    EndpointFunctions.TogglePause( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ScoutAll":
                    EndpointFunctions.Debug_ScoutAll( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ScrapUnits":
                    EndpointFunctions.ScrapSelectedUnits( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "TogglePursuitMode":
                    EndpointFunctions.TogglePursuitMode_FromPlayer( GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ToggleAttackMove":
                    EndpointFunctions.ToggleAttackMove_FromPlayer( GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ToggleGroupMove":
                    EndpointFunctions.ToggleSpeedGroupForPlayerShipsONLY( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ToggleStopToShoot":
                    EndpointFunctions.ToggleStopToShootAnySeenTargets( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "UnloadTransports":
                    EndpointFunctions.UnloadTransports( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "LoadTransports":
                    EndpointFunctions.LoadTransports( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "ToggleShipsEnabled":
                    EndpointFunctions.ToggleShipsEnabled( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "Order_Stop":
                    EndpointFunctions.Order_Stop( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                /*
                case "Order_Attack":
                    EndpointFunctions.Order_Attack( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "Order_Defend":
                    EndpointFunctions.Order_Defend( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                case "Order_Patrol":
                    EndpointFunctions.Order_Patrol( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
                */
                case "Order_CustomSystem_1":
                    EndpointFunctions.Order_CustomSystem( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 1 );
                    break;
                case "Order_CustomSystem_2":
                    EndpointFunctions.Order_CustomSystem( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 2 );
                    break;
                case "Order_CustomSystem_3":
                    EndpointFunctions.Order_CustomSystem( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, 3 );
                    break;
                case "TogglePlanetFactionBooleanFlag":
                    EndpointFunctions.TogglePlanetFactionBooleanFlagAtCurrentPlanet( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, ( PlanetFactionBooleanFlag)Int1 );
                    break;
                case "SelectAllMobileMilitary":
                    EndpointFunctions.QuickSelect( QuickSelectionType.MobileMilitary );
                    break;
                case "SplitSelection":
                    EndpointFunctions.SplitSelection( );
                    break;
                case "SelectCommandStation":
                    EndpointFunctions.QuickSelect( QuickSelectionType.CommandStation );
                    break;
                case "SelectFactories":
                    EndpointFunctions.QuickSelect( QuickSelectionType.FactoriesForPlayerMobileFleets );
                    break;
                case "SelectCloakingUnits":
                    EndpointFunctions.QuickSelect( QuickSelectionType.CloakingUnits );
                    break;
                case "SelectMobileFlagships":
                    EndpointFunctions.QuickSelect( QuickSelectionType.MobileFlagships );
                    break;
                case "SelectBattlestationsAndCitadels":
                    EndpointFunctions.QuickSelect( QuickSelectionType.BattlestationsAndCitadels );
                    break;
                case "SelectSnipers":
                    EndpointFunctions.QuickSelect( QuickSelectionType.Snipers );
                    break;
                case "SelectAllNonFlagshipMilitary":
                    EndpointFunctions.QuickSelect( QuickSelectionType.NonFlagships );
                    break;
                case "SelectEngineers":
                    EndpointFunctions.QuickSelect( QuickSelectionType.Engineers );
                    break;
                case "SelectMelee":
                    EndpointFunctions.QuickSelect( QuickSelectionType.Melee );
                    break;
                case "SelectTractors":
                    EndpointFunctions.QuickSelect( QuickSelectionType.TractorUnits );
                    break;
                case "IncreaseFrameSize":
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, true );
                    break;
                case "DecreaseFrameSize":
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, false );
                    break;
                case "IncreaseFrameFrequency":
                    EndpointFunctions.LowerOrRaiseGameSpeedStyle( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, true );
                    break;
                case "DecreaseFrameFrequency":
                    EndpointFunctions.LowerOrRaiseGameSpeedStyle( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, false );
                    break;
                case "OpenEncyclopedia_AndSearch":
                    EndpointFunctions.OpenEncyclopedia_AndSearch();
                    break;
                case "OpenGalaxy_AndSearch":
                    EndpointFunctions.OpenGalaxyMap_AndSearch();
                    break;
                case "OpenBuildTab":
                {
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.DirectBuild )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.DirectBuild;
                    }
                    break;
                case "OpenFleetsTab":
                    {
                        if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Fleets )
                            Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                        else
                            Window_InGameSidebarBase.Current = InGameSidebarType.Fleets;
                    }
                    break;
                case "OpenHackingTab":
                    {
                        if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Hacking )
                            Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                        else
                            Window_InGameSidebarBase.Current = InGameSidebarType.Hacking;
                    }
                    break;
                case "OpenTechTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Science )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Science;
                    break;
                case "OpenOpsTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Outguard )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Outguard;
                    break;
                case "OpenShipsTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Ships )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Ships;
                    break;
                case "OpenObjectivesTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Objectives )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Objectives;
                    break;
                case "OpenJournalTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Journal )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Journal;
                    break;
                case "OpenTipsTab":
                    if ( SidebarKeybindingsAreToggles && Window_InGameSidebarBase.Current == InGameSidebarType.Tips )
                        Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                    else
                        Window_InGameSidebarBase.Current = InGameSidebarType.Tips;
                    break;
                case "ToggleFleetStatusWindowForPlanet":
                    Window_BottomLeftGalaxyMap.btnFleetStatus.ToggleFleetHealthForPlanet();
                    break;
                case "ToggleFleetStatusWindowForAll":
                    Window_BottomLeftGalaxyMap.btnFleetStatus.ToggleFleetHealthForAllPlanets();
                    break;
                case "TogglePingMode":
                    Engine_AIW2.Instance.PendingTargetedAction = null;
                    // todo: make these all ITargetedInputAction(s)
                    Engine_AIW2.Instance.IsInPingLocationMode = !Engine_AIW2.Instance.IsInPingLocationMode;
                    Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                    Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                    break;
                case "OpenFactionWindowWithoutPausing":
                    Window_FactionsWindow.Instance.Open();
                    break;
                case "ToggleFreeLook":
                    EndpointFunctions.ToggleFreeLook();
                    break;
                case "OpenChat":
                    EndpointFunctions.OpenChat();
                    break;
                case "SwitchToControllingNextHumanFaction":
                    EndpointFunctions.SwitchLocalPlayerAccountToControllingNextHumanFaction( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull() );
                    break;
            }
        }
    }
}
