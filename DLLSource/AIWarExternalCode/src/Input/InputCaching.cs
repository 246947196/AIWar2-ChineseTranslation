using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class InputCaching
    {
        /*
         * Heya!  You don't have to cache all your InputActionTypeData in this location.  
         * Depending on what you are doing, it might be advantageous to cache it in your own class.
         * If you're in a custom class or DLL, or in general you're a modder, you want to definitely do that, 
         * because this file is maintained by Arcen.
         * 
         * This does show you a good pattern for how to handle it, though.  
         * The idea is that you want to call into ActionsByName as few times as possible,
         * and then beyond that just use the cached versions of the InputActionTypeData objects.
         * */

        public static InputActionTypeData inputAddToSelection = null;
        public static InputActionTypeData inputRemoveFromSelection = null;
        public static InputActionTypeData inputSendThroughWormhole = null;
        public static InputActionTypeData inputHoldToGiveOrdersToStationaryFlagships = null;

        private static InputActionTypeData inputSuppressTooltips = null;
        private static InputActionTypeData inputIncreaseShipAndPlanetTooltipDetailBy1_Key1 = null;
        private static InputActionTypeData inputIncreaseShipAndPlanetTooltipDetailBy1_Key2 = null;
        private static InputActionTypeData inputHoldAndClickToSuppressTechUpgradePrompt = null;
        private static InputActionTypeData inputHoldAndClickToViewDetailsOfContents = null;
        private static InputActionTypeData inputHoldToSeeShipStrengthsAndWeaknesses = null;

        public static InputActionTypeData inputCameraSlowDown = null;
        public static InputActionTypeData inputCameraSpeedUp = null;

        public static InputActionTypeData inputTakeSafestPath = null;
        public static InputActionTypeData inputTakeShortestPath = null;
        
        /*
         * The various private variables are usually things to be used as an 
         * Axis (-1 to 1 from 2 keys instead of 0 to 1 from 1 key), or
         * which have a main version and an Alt version that both need to be checked 
         * succinctly from elsewhere.
         * 
         * Keeping those private makes it so that modders (and devs) can't typo their 
         * way into omitting a keybind usage elsewhere.
         * */

        private static InputActionTypeData inputCameraGrabPanMode = null;
        private static InputActionTypeData inputCameraGrabPanModeAlt = null;

        private static InputActionTypeData inputCameraLeft = null;
        private static InputActionTypeData inputCameraLeftAlt = null;
        private static InputActionTypeData inputCameraRight = null;
        private static InputActionTypeData inputCameraRightAlt = null;
        private static InputActionTypeData inputCameraForward = null;
        private static InputActionTypeData inputCameraForwardAlt = null;
        private static InputActionTypeData inputCameraBackward = null;
        private static InputActionTypeData inputCameraBackwardAlt = null;

        public static InputActionTypeData inputCameraRotateTiltMode = null;
        public static InputActionTypeData inputCameraRotateTiltModeAlt = null;

        private static InputActionTypeData inputCameraZoomInKeyboard = null;
        private static InputActionTypeData inputCameraZoomOutKeyboard = null;
        private static InputActionTypeData inputCameraZoomInOutMouse = null;

        private static InputActionTypeData inputCameraRotateLeft = null;
        private static InputActionTypeData inputCameraRotateRight = null;
        private static InputActionTypeData inputCameraTiltDown = null;
        private static InputActionTypeData inputCameraTiltUp = null;

        public static InputActionTypeData inputSelectUnit = null;
        public static InputActionTypeData inputGiveOrdersToUnit = null;
        public static InputActionTypeData inputSelectOnlyTurrets = null;
        public static InputActionTypeData inputBuild5xUnits = null;
        public static InputActionTypeData inputBuild10xUnits = null;
        public static InputActionTypeData inputBuildHalfUnits = null;
        public static InputActionTypeData inputBuildThirdUnits = null;
        public static InputActionTypeData inputAssignFleetGroup = null;
        public static InputActionTypeData inputRemoveFleetGroup = null;
        public static InputActionTypeData inputAddFleetGroup = null;

        private static InputActionTypeData inputHoldForOrangePings = null;
        private static InputActionTypeData inputHoldForBluePings = null;
        private static InputActionTypeData inputHoldForPinkPings = null;
        private static InputActionTypeData inputHoldForYellowPings = null;

        public static void UpdateInputCacheIfNeeded()
        {
            if ( !InputActionTypeDataTable.IsInitialized )
                return;

            if ( inputAddToSelection == null )
            {
                inputAddToSelection = InputActionTypeDataTable.GetActionByName_FairlySlow( "AddToSelection" );
                inputRemoveFromSelection = InputActionTypeDataTable.GetActionByName_FairlySlow( "RemoveFromSelection" );
                inputSendThroughWormhole = InputActionTypeDataTable.GetActionByName_FairlySlow( "SendThroughWormhole" );
                inputHoldToGiveOrdersToStationaryFlagships = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToGiveOrdersToStationaryFlagships" );
                inputSuppressTooltips = InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTooltips" );
                inputIncreaseShipAndPlanetTooltipDetailBy1_Key1 = InputActionTypeDataTable.GetActionByName_FairlySlow( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" );
                inputIncreaseShipAndPlanetTooltipDetailBy1_Key2 = InputActionTypeDataTable.GetActionByName_FairlySlow( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" );
                inputHoldAndClickToSuppressTechUpgradePrompt = InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" );
                inputHoldAndClickToViewDetailsOfContents = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldAndClickToViewDetailsOfContents" );
                inputHoldToSeeShipStrengthsAndWeaknesses = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToSeeShipStrengthsAndWeaknesses" );

                inputCameraSlowDown = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraSlowDown" );
                inputCameraSpeedUp = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraSpeedUp" );

                inputTakeShortestPath = InputActionTypeDataTable.GetActionByName_FairlySlow( "TakeShortestPath" );
                inputTakeSafestPath = InputActionTypeDataTable.GetActionByName_FairlySlow( "TakeSafestPath" );

                inputCameraGrabPanMode = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraGrabPanMode" );
                inputCameraGrabPanModeAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraGrabPanModeAlt" );

                inputCameraLeft = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraLeft" );
                inputCameraLeftAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraLeftAlt" );
                inputCameraRight = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRight" );
                inputCameraRightAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRightAlt" );
                inputCameraForward = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraForward" );
                inputCameraForwardAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraForwardAlt" );
                inputCameraBackward = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraBackward" );
                inputCameraBackwardAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraBackwardAlt" );

                inputCameraRotateTiltMode = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRotateTiltMode" );
                inputCameraRotateTiltModeAlt = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRotateTiltModeAlt" );

                inputCameraZoomInKeyboard = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraZoomInKeyboard" );
                inputCameraZoomOutKeyboard = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraZoomOutKeyboard" );
                inputCameraZoomInOutMouse = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraZoomInOutMouse" );

                inputCameraRotateLeft = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRotateLeft" );
                inputCameraRotateRight = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraRotateRight" );
                inputCameraTiltDown = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraTiltDown" );
                inputCameraTiltUp = InputActionTypeDataTable.GetActionByName_FairlySlow( "CameraTiltUp" );

                inputSelectUnit = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectUnit" );
                inputGiveOrdersToUnit = InputActionTypeDataTable.GetActionByName_FairlySlow( "GiveOrdersToUnit" );
                inputSelectOnlyTurrets = InputActionTypeDataTable.GetActionByName_FairlySlow( "OnlySelectTurrets" );
                inputBuild5xUnits = InputActionTypeDataTable.GetActionByName_FairlySlow( "Build5xUnits" );
                inputBuild10xUnits = InputActionTypeDataTable.GetActionByName_FairlySlow( "Build10xUnits" );
                inputBuildHalfUnits = InputActionTypeDataTable.GetActionByName_FairlySlow( "BuildHalfUnits" );
                inputBuildThirdUnits = InputActionTypeDataTable.GetActionByName_FairlySlow( "BuildThirdUnits" );
                inputAssignFleetGroup = InputActionTypeDataTable.GetActionByName_FairlySlow( "AssignFleetGroup" );
                inputRemoveFleetGroup = InputActionTypeDataTable.GetActionByName_FairlySlow( "RemoveFleetGroup" );
                inputAddFleetGroup = InputActionTypeDataTable.GetActionByName_FairlySlow( "AddFleetGroup" );

                inputHoldForOrangePings = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldForOrangePings" );
                inputHoldForBluePings = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldForBluePings" );
                inputHoldForPinkPings = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldForPinkPings" );
                inputHoldForYellowPings = InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldForYellowPings" );
            }
        }

        public static bool CalculatesZoomInMovesTowardsCursor()
        {
            return GameSettings_AIW2.Current != null && GameSettings_AIW2.Current.GetBool(ArcenBoolSetting_AIW2.ZoomInMovesTowardsCursor);
        }

        //public static bool CalculatesInvertMouseZoom()
        //{
        //    return GameSettings_AIW2.Current != null && GameSettings_AIW2.Current.GetBool(ArcenBoolSetting_AIW2.InvertMouseZoom);
        //}

        public static float CalculateCameraZoomInOutMouseValue()
        {
            //if ( Engine_Universal.IsAnyTextboxFocused )
            //    return 0;
            float value = inputCameraZoomInOutMouse.CalculateValue();
            if (value == 0)
                return 0;
            if (GameSettings_AIW2.Current != null && GameSettings_AIW2.Current.GetBool(ArcenBoolSetting_AIW2.InvertMouseZoom))
                return -value;
            return value;
        }

        //When you're setting up a control with an alternate, this is the pattern to use.
        //Make sure that the caller calls UpdateInputCacheIfNeeded first.
        public static bool CalculateCameraGrabPanMode()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            return inputCameraGrabPanMode.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraGrabPanModeAlt.CalculateIsKeyDownNow_IfNothingElseConflicts();
        }

        //When you have a -1 to 1 "axis" (in this case with alts), this is the pattern to use.
        //Make sure that the caller calls UpdateInputCacheIfNeeded first.
        public static float CalculateCameraLeftRightAxis()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return 0;
            int value = 0;
            if ( inputCameraLeft.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraLeftAlt.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                value -= 1;
            if ( inputCameraRight.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraRightAlt.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                value += 1;
            return value;
        }

        public static float CalculateCameraForwardBackwardAxis()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return 0;
            int value = 0;
            if ( inputCameraForward.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraForwardAlt.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                value += 1;
            if ( inputCameraBackward.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraBackwardAlt.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                value -= 1;
            return value;
        }

        public static bool CalculateCameraRotateTiltMode()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            return inputCameraRotateTiltMode.CalculateIsKeyDownNow_IfNothingElseConflicts() || inputCameraRotateTiltModeAlt.CalculateIsKeyDownNow_IfNothingElseConflicts();
        }

        public static float CalculateCameraZoomInOutKeyboard()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return 0;
            if ( inputCameraZoomInKeyboard.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return 1;
            if ( inputCameraZoomOutKeyboard.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return -1;
            return 0;
        }

        public static float CalculateCameraRotateLeftRight()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return 0;
            if ( inputCameraRotateLeft.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return -1;
            if ( inputCameraRotateRight.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return 1;
            return 0;
        }

        public static float CalculateCameraTiltUpDown()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return 0;
            if ( inputCameraTiltDown.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return -1;
            if ( inputCameraTiltUp.CalculateIsKeyDownNow_IfNothingElseConflicts() )
                return 1;
            return 0;
        }

        public static bool CalculateShouldTooltipsBeSuppressed()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputSuppressTooltips == null )
                return false;
            if ( Engine_AIW2.Instance.IsInPingLocationMode )
                return false;
            return inputSuppressTooltips.CalculateIsKeyDownNow_IgnoreConflicts();
        }

        public static PlanetPingColor CalculatePlanetPingColor()
        {
            if ( inputHoldForYellowPings != null && inputHoldForYellowPings.CalculateIsKeyDownNow_IgnoreConflicts() )
                return PlanetPingColor.Yellow;
            if ( inputHoldForPinkPings != null && inputHoldForPinkPings.CalculateIsKeyDownNow_IgnoreConflicts() )
                return PlanetPingColor.Pink;
            if ( inputHoldForBluePings != null && inputHoldForBluePings.CalculateIsKeyDownNow_IgnoreConflicts() )
                return PlanetPingColor.Blue;
            if ( inputHoldForOrangePings != null && inputHoldForOrangePings.CalculateIsKeyDownNow_IgnoreConflicts() )
                return PlanetPingColor.Orange;
            return PlanetPingColor.Green;
        }

        public static int CalculateNumDetailLevelIncreaseKeysHeld()
        {
            int num = 0;
            if (CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1())
                num++;
            if (CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2())
                num++;
            return num;
        }
        
        public static bool CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputIncreaseShipAndPlanetTooltipDetailBy1_Key1 == null )
                return false;
            return inputIncreaseShipAndPlanetTooltipDetailBy1_Key1.CalculateIsKeyDownNow_IgnoreConflicts();
        }
        public static bool CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputIncreaseShipAndPlanetTooltipDetailBy1_Key2 == null )
                return false;
            return inputIncreaseShipAndPlanetTooltipDetailBy1_Key2.CalculateIsKeyDownNow_IgnoreConflicts();
        }
        public static int CalculateNumberOfTooltipDetailKeysHeld()
        {
            int num = 0;
            if (CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1())
                num++;
            if (CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2())
                num++;
            return num;
        }

        public static bool CalculateHoldAndClickToViewDetailsOfContents()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputHoldAndClickToViewDetailsOfContents == null )
                return false;
            return inputHoldAndClickToViewDetailsOfContents.CalculateIsKeyDownNow_IgnoreConflicts();
        }

        public static bool CalculateShouldUseShortestPath()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputTakeShortestPath == null )
                return false;
            return inputTakeShortestPath.CalculateIsKeyDownNow_IgnoreConflicts();
        }
        public static bool CalculateShouldUseSafestPath()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputTakeSafestPath == null )
                return false;
            return inputTakeSafestPath.CalculateIsKeyDownNow_IgnoreConflicts();

        }
        public static bool CalculateHoldAndSuppressTechUpgradePrompt()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputHoldAndClickToSuppressTechUpgradePrompt == null )
                return false;
            return inputHoldAndClickToSuppressTechUpgradePrompt.CalculateIsKeyDownNow_IgnoreConflicts();
        }

        public static bool CalculateHoldToSeeShipStrengthsAndWeaknesses()
        {
            if ( Engine_Universal.IsAnyTextboxFocused )
                return false;
            if ( inputHoldToSeeShipStrengthsAndWeaknesses == null )
                return false;
            return inputHoldToSeeShipStrengthsAndWeaknesses.CalculateIsKeyDownNow_IgnoreConflicts();
        }
    }
}
