using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class BrownoutNotifier : NotifierBaseDataSingleton
    {
        public static BrownoutNotifier Instance = new BrownoutNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Brownout;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Brownout = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/brownout.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            if ( Data == null )
                return true;
            if ( Data.IsLocalFaction )
            {
                if ( Data.Faction.NetEnergy < 0 )
                    tooltipBuffer.Add( "Brownout", "ffa1a1" ).Add( ": You have a negative Energy balance! While in brownout your factories will work at half speed. Also, your bubble forcefields won't be able to project their protective fields for another " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " seconds after the energy is restored." );
                else
                    tooltipBuffer.Add( "Brownout Recovery", "a1ffa1" ).Add( ": Your bubble forcefields won't be able to project their protective fields for another " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " seconds." );
            }
            else if ( Data.Faction != null )
            {
                if ( Data.Faction.NetEnergy < 0 )
                    tooltipBuffer.Add( "Brownout", "ffa1a1" ).Add( ": Your ally " ).Add( Data.Faction.GetDisplayName() ).Add( " has a negative Energy balance! Their factories will work at half speed and Their bubble forcefields won't be able to project their protective fields for another " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " seconds after the energy is restored." );
                else
                    tooltipBuffer.Add( "Brownout Recovery", "a1ffa1" ).Add( ": Your ally " ).Add( Data.Faction.GetDisplayName() ).Add( " is has bubble forcefields that won't be able to project their protective fields for another " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " seconds." );
            }
            //tooltipBuffer.Add("Brownout: You have a negative energy balance!  Your bubble forcefields won't be able to project their protective field for another ").Add( timer ).Add(" seconds after the energy is restored");
            ArcenExternalUIUtilities.ShowTooltipWide( tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();

                if ( Data == null )
                    return false;

                debugStage = 10;
                Image.UpdateWith( sprite_Brownout, true, "Brownout" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Brownout" );
                SubTexts[0].Text.FinishWritingToBuffer();

                Faction fac = Data.Faction;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.IsLocalFaction )
                {
                    if ( fac == null || fac.NetEnergy < 0 )
                        buffer.Add( "Ongoing" );
                    else
                        buffer.Add( Data.eventTimeRemaining );
                }
                else
                {
                    buffer.Add( "Ally\n<size=60%>" );
                    if ( fac == null )
                        buffer.Add( "???" );
                    else
                        buffer.Add( fac.GetDisplayName() );
                }
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in BrownoutNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
