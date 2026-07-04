using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine.Profiling;

namespace Arcen.AIW2.External
{
    public class Window_DebugInfo : WindowControllerAbstractBase
    {
        public static Window_DebugInfo Instance = new Window_DebugInfo();
        public Window_DebugInfo()
        {
            Instance = this;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_Universal.IsProfilerEnabled )
                return true;
            return false;
        }

        public class tText : TextAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                if ( Engine_Universal.IsProfilerEnabled )
                    return false;
                return true;
            }

            public override void OnUpdate()
            {
                Instance.ExtraOffsetY = Window_NotificationsDisplay.CurrentRowOffsetInView;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( Engine_Universal.IsProfilerEnabled )
                    this.DoSingleCase_ProfilerEnabled( buffer );
            }

            private void DoSingleCase_ProfilerEnabled( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "分析器已开启！" );
            }
        }
    }
}