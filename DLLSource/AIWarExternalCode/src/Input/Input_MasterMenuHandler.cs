using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Input_MasterMenuHandler : BaseInputHandler
    {
        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers.Count > 0 )
                return;
            if ( ArcenInput.IsInputBlockedForAmountOfTime )
                return;
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        
                        if (Engine_AIW2.Instance.PendingTargetedAction != null)
                            Engine_AIW2.Instance.PendingTargetedAction = null;
                        // todo: make these all ITargetedInputAction(s)
                        else if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() )
                            Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                        else if ( Engine_AIW2.Instance.IsInPingLocationMode )
                            Engine_AIW2.Instance.IsInPingLocationMode = false;
                        else if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null )
                            Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                        
                        //if the self-updating text window is open, then close out of that first, too.
                        else if ( Window_ModalSelfUpdatingTextWindow.Instance != null && Window_ModalSelfUpdatingTextWindow.Instance.GetIsOpen() )
                            Window_ModalSelfUpdatingTextWindow.Instance.Close();
                        else if ( Window_ModalSelfUpdatingTextWindow_Wide.Instance != null && Window_ModalSelfUpdatingTextWindow_Wide.Instance.GetIsOpen() )
                            Window_ModalSelfUpdatingTextWindow_Wide.Instance.Close();
                        else if ( Window_ModalSelfUpdatingTextWindow_UltraWide.Instance != null && Window_ModalSelfUpdatingTextWindow_UltraWide.Instance.GetIsOpen() )
                            Window_ModalSelfUpdatingTextWindow_UltraWide.Instance.Close();
                        else if ( Window_ModalOK.Instance != null && Window_ModalOK.Instance.IsOpen )
                            Window_ModalOK.Instance.Close();
                        else if ( Window_ModalOKTall.Instance != null && Window_ModalOKTall.Instance.IsOpen )
                            Window_ModalOKTall.Instance.Close();
                        else if ( Window_ModalOKTall_Wide.Instance != null && Window_ModalOKTall_Wide.Instance.IsOpen )
                            Window_ModalOKTall_Wide.Instance.Close();
                        else if ( Window_ModalOKTall_UltraWide.Instance != null && Window_ModalOKTall_UltraWide.Instance.IsOpen )
                            Window_ModalOKTall_UltraWide.Instance.Close();
                        //if the fleet management sidebar popop is open, then close out of that first, too.
                        else if ( Window_FleetManagementSidebarPopout.Instance != null && Window_FleetManagementSidebarPopout.Instance.GetIsOpen() )
                            Window_FleetManagementSidebarPopout.Instance.Close();
                        //if the hacking choices sidebar popop is open, then close out of that first, too.
                        else if ( Window_HackChoicesSidebarPopout.Instance != null && Window_HackChoicesSidebarPopout.Instance.GetIsOpen() )
                            Window_HackChoicesSidebarPopout.Instance.Close();
                        else if ( GameSettings.Current.GetBoolBySetting("EscapeClosesSidebar") && Window_InGameSidebarBase.Current != InGameSidebarType.Closed )
                            Window_InGameSidebarBase.Current = InGameSidebarType.Closed;
                        else
                        {
                            bool currentlyOpen = Window_InGameEscapeMenu.Instance.GetIsOnStack();
                            if ( currentlyOpen )
                                StackMenuWindowController.CloseEntireStack( true );
                            else
                            {
                                Window_InGameEscapeMenu.Instance.Open();
                            }
                        }
                    }
                    break;
                case "MasterMenuBack":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        StackMenuWindowController.CloseTopItemOnStack();
                    }
                    break;
            }
        }

        public static IArcenUI_Button_Controller GetButtonByIndex( ArcenUI_Window WindowToSearch, int targetButtonIndex )
        {
            bool reverseSearch = false;// WindowToSearch.Controller != Window_InGameMasterMenu.Instance;
            
            List<ArcenUI_Element> elements = WindowToSearch.Elements;
            int indexFound = -1;
            for ( int i = reverseSearch ? elements.Count - 1 : 0;
                  reverseSearch ? i >= 0 : i < elements.Count;
                  i += reverseSearch ? -1 : 1 )
            {
                ArcenUI_Element element = elements[i];
                if ( element.Type != ArcenUI_ElementType.Button )
                    continue;
                indexFound++;
                if ( indexFound < targetButtonIndex )
                    continue;
                ArcenUI_Button elementAsType = (ArcenUI_Button)element;
                return (IArcenUI_Button_Controller)elementAsType.Controller;
            }

            return null;
        }

        public static int GetIndexByButton( ArcenUI_Window WindowToSearch, IArcenUI_Button_Controller buttonController )
        {
            bool reverseSearch = false;// WindowToSearch.Controller != Window_InGameMasterMenu.Instance;
            
            List<ArcenUI_Element> elements = WindowToSearch.Elements;
            int indexFound = -1;
            for ( int i = reverseSearch ? elements.Count - 1 : 0;
                  reverseSearch ? i >= 0 : i < elements.Count;
                  i += reverseSearch ? -1 : 1 )
            {
                ArcenUI_Element element = elements[i];
                if ( element.Type != ArcenUI_ElementType.Button )
                    continue;
                indexFound++;
                if ( element.Controller != buttonController )
                    continue;
                return indexFound;
            }

            return -1;
        }
    }
}
