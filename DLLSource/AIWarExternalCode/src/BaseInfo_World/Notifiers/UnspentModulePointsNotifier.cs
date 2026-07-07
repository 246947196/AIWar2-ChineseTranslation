using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class UnspentModulePointsNotifier : NotifierBaseDataSingleton
    {
        public static UnspentModulePointsNotifier Instance = new UnspentModulePointsNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_UnspentModulePoints;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_UnspentModulePoints = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/unspentfuelpoints.png" );
        }

        private short lastIndex = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            try
            {
                lastIndex++;
                if ( lastIndex >= Data.ObjectList.Count )
                    lastIndex = 0;
                FleetMembership mem = (FleetMembership)Data.ObjectList[lastIndex];
                Window_ModalFleetMemberModularEditing.Instance.Open( mem );
            }
            catch { }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Add( Data.ObjectList.Count ).Add( " 个你的单位是模块化的且有未使用的模块点数，这使它们比你预期的要弱。点击此处查看列表中的第一个，解决后再点击查看下一个，依此类推。" );
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
                Image.UpdateWith( sprite_UnspentModulePoints, true, "UnspentModulePoints" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "未使用\n" );
                buffer.Add( "模块" );
                SubTexts[0].Text.FinishWritingToBuffer();

                Faction fac = Data.Faction;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( Data.ObjectList.Count );
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in UnspentModulePointsNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
