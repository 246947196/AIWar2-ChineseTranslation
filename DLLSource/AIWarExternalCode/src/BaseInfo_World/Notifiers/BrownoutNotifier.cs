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
                    tooltipBuffer.Add( "Brownout", "ffa1a1" ).Add( "：你的能量余额为负值！在电力不足期间，你的工厂将以一半速度运转。此外，你的泡泡力场将在能量恢复后再经过 " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " 秒才能重新投射防护力场。" );
                else
                    tooltipBuffer.Add( "Brownout Recovery", "a1ffa1" ).Add( "：你的泡泡力场将在再经过 " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " 秒后才能重新投射防护力场。" );
            }
            else if ( Data.Faction != null )
            {
                if ( Data.Faction.NetEnergy < 0 )
                    tooltipBuffer.Add( "Brownout", "ffa1a1" ).Add( "：你的盟友 " ).Add( Data.Faction.GetDisplayName() ).Add( " 的能量余额为负值！他们的工厂将以一半速度运转，且他们的泡泡力场将在能量恢复后再经过 " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " 秒才能重新投射防护力场。" );
                else
                    tooltipBuffer.Add( "Brownout Recovery", "a1ffa1" ).Add( "：你的盟友 " ).Add( Data.Faction.GetDisplayName() ).Add( " 的泡泡力场将在再经过 " ).Add( Data.eventTimeRemaining, "00ff00" ).Add( " 秒后才能重新投射防护力场。" );
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
                buffer.Add( "电力不足" );
                SubTexts[0].Text.FinishWritingToBuffer();

                Faction fac = Data.Faction;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.IsLocalFaction )
                {
                    if ( fac == null || fac.NetEnergy < 0 )
                        buffer.Add( "持续中" );
                    else
                        buffer.Add( Data.eventTimeRemaining );
                }
                else
                {
                    buffer.Add( "盟友\n<size=60%>" );
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
