using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WildHivesNotifier : NotifierBaseDataSingleton
    {
        public static WildHivesNotifier Instance = new WildHivesNotifier();

        // these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite icon;
        private static bool hasInitialized = false;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            icon = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spiredebris.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        public int GetPlayerInfested( NotifierFillData Data ) => Data.Int16List[0];
        public int GetPlayerTotal( NotifierFillData Data ) => Data.Int16List[1];
        public int GetGalaxyInfested( NotifierFillData Data ) => Data.Int16List[2];
        public int GetGalaxyTotal( NotifierFillData Data ) => Data.Int16List[3];
        public int GetClanlings( NotifierFillData Data ) => Data.Int16List[4];

        public int GetPlayerPerc( NotifierFillData Data ) => GetPlayerTotal( Data ) > 0 ? (GetPlayerInfested( Data ) * 100) / GetPlayerTotal( Data ) : 0;
        public int GetGalaxyPerc( NotifierFillData Data ) => GetGalaxyTotal( Data ) > 0 ? (GetGalaxyInfested( Data ) * 100) / GetGalaxyTotal( Data ) : 0;


        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            int debugCode = 0;
            try
            {
                debugCode = 100;
                tooltipBuffer.Add( $"奈祖尔野生蜂巢正在缓慢占领银河系中的所有资源。\n\n" );

                tooltipBuffer.Add( $"玩家领土目前感染程度为 {GetPlayerPerc( Data )}%。\n\n" );

                tooltipBuffer.Add( $"银河系目前感染程度为 {GetGalaxyPerc( Data )}%。\n\n" );

                if ( GetClanlings( Data ) > 0 )
                {
                    debugCode = 150;
                    tooltipBuffer.Add( $"我们领土内目前有 {GetClanlings( Data )} 个族人。" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in WildHives notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                Image.UpdateWith( icon, true, "WildHives" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "感染" );
                SubTexts[0].Text.FinishWritingToBuffer();
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 20;
                buffer.Add( $"玩家：{GetPlayerPerc( Data )}%\n" );
                debugStage = 30;
                buffer.Add( $"银河系：{GetGalaxyPerc( Data )}%" );
                debugStage = 40;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in WildHivesNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
