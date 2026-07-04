using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_InGameStandingPlanetOrders : WindowControllerAbstractBase
    {
        public Window_InGameStandingPlanetOrders()
        {
            this.OnlyShowInGame = true;
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int debugStage = 0;
                try
                {
                    debugStage = 1;
                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    if ( planet == null )
                        return;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    PlanetFaction faction = planet.GetPlanetFactionForFaction( localFaction );
                    bool isFirst = true;
                    for ( PlanetFactionBooleanFlag flag = PlanetFactionBooleanFlag.None + 1; flag < PlanetFactionBooleanFlag.Length; flag++ )
                    {
                        debugStage++;
                        string displayText = flag.GetDisplayText( faction.GetPlanetFactionBooleanFlag( flag ), true );
                        if ( displayText.Length <= 0 )
                            continue;
                        if ( isFirst )
                            isFirst = false;
                        else
                            buffer.Add( "\n" );
                        buffer.Add( displayText );
                    }

                    debugStage = 25;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception 在 standing planet orders text generation at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }
        }
        
        public class bToggle : WindowTogglingButtonController
        {
            public bToggle() : base( "行星驻留命令", "行星驻留命令" ) { }
            public override ToggleableWindowController GetRelatedController() { return Window_InGamePlanetActionMenu.Instance; }
        }
    }
}
