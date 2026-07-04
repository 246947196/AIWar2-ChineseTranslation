using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGamePlanetActionMenu : ToggleableWindowController
    {
        public static Window_InGamePlanetActionMenu Instance;
        public Window_InGamePlanetActionMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        private Int16 PlanetIndex = -1;
        private bool PlanetChangedSinceLastButtonSetUpdate;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

            if ( planet == null )
            {
                this.PlanetIndex = -1;
                return false;
            }

            if ( planet.Index != this.PlanetIndex )
            {
                this.PlanetIndex = planet.Index;
                this.PlanetChangedSinceLastButtonSetUpdate = true;
            }

            return true;
        }

        public class bsItems : ButtonSetAbstractBase
        {
            private static List<IArcenUI_Button_Controller> buttonControllers = List<IArcenUI_Button_Controller>.Create_WillNeverBeGCed( 20, "Window_InGamePlanetActionMenu-bsItems-buttonControllers" );

            public override void OnUpdate()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)this.Element;
                Window_InGamePlanetActionMenu windowController = (Window_InGamePlanetActionMenu)this.Element.Window.Controller;

                if ( windowController.PlanetChangedSinceLastButtonSetUpdate )
                {
                    elementAsType.ClearButtons();

                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( windowController.PlanetIndex );
                    if ( planet != null )
                    {
                        buttonControllers.Clear();
                        for ( PlanetFactionBooleanFlag flag = PlanetFactionBooleanFlag.None + 1; flag < PlanetFactionBooleanFlag.Length; flag++ )
                            buttonControllers.Add( new bPlanetFactionBooleanFlagToggle( flag ) );
                        LayOutButtonSetAndSizeCanvasAndSelfHeight( elementAsType, buttonControllers );
                    }

                    elementAsType.ActuallyPutItemsBackInPoolThatAreStillCleared();

                    windowController.PlanetChangedSinceLastButtonSetUpdate = false;
                }
            }
        }

        private class bPlanetFactionBooleanFlagToggle : ButtonAbstractBase
        {
            private readonly PlanetFactionBooleanFlag Flag;

            public bPlanetFactionBooleanFlagToggle( PlanetFactionBooleanFlag Flag )
            {
                this.Flag = Flag;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                PlanetFaction faction = planet.GetPlanetFactionForFaction( localFaction );
                buffer.Add( this.Flag.GetDisplayText( faction.GetPlanetFactionBooleanFlag( this.Flag ), false ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.TogglePlanetFactionBooleanFlagAtCurrentPlanet( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, this.Flag );
                return MouseHandlingResult.None;
            }
        }
    }

    public static class PlanetFactionBooleanFlagExtensionMethods
    {
        public static string GetDisplayText(this PlanetFactionBooleanFlag Flag, bool State, bool ForSummary)
        {
            switch ( Flag )
            {
                case PlanetFactionBooleanFlag.TryToCapture:
                    if ( ForSummary )
                        if ( State )
                            return string.Empty;
                        else
                            return "不占领";
                    else if ( State )
                        return "占领：开启";
                    else
                        return "占领：关闭";
                case PlanetFactionBooleanFlag.DoNotPathThrough:
                    if ( ForSummary )
                        if ( State )
                            return "禁止路径穿过";
                        else
                            return string.Empty;
                    else if ( State )
                        return "路径穿过：关闭";
                    else
                        return "路径穿过：开启";
                default:
                    return string.Empty;
            }
        }
    }
}
