using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicArmadaCPANotifier : NotifierBaseDataSingleton
    {
        public static PublicArmadaCPANotifier Instance = new PublicArmadaCPANotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_CPA;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_CPA = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spireimperial.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            
            int debugCode = 0;
            try
            {
                if ( Data.Int64List.Count > 0 )
                    tooltipBuffer.Add("AI 将在 ").Add(Data.Int64List[0]).Add(" 秒后对您发动 CPA。");
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in armada building notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
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

                debugStage = 10;
                Image.UpdateWith( sprite_CPA, true, "ArmadaCPA" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "舰队\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.Int64List.Count == 0 )
                    return true;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( Data.Int64List[0]);
                debugStage = 40;
                // if (data != null && data.TimeTillArmadaSphereWin > 0)
                // {
                //     string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime(data.TimeTillArmadaSphereWin);
                //     buffer.Add(data.TimeTillArmadaSphereWin.ToString(), color);
                // }
                // else
                // {
                //     buffer.Add( "Complete", "a1ffa1" );
                // }
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicArmadaCPANotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    
    
}
