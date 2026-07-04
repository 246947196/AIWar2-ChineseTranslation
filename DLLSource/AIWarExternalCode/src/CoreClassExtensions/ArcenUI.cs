using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public static class ArcenUIExtensions
    {
        public static void Tooltip_Margin( this ArcenUI ui, out float margin_h, out float margin_v )
        {
            margin_v = ExternalConstants.Instance.GetCustomFloat_Slow("tooltip_text_margin_v");
            margin_h = ExternalConstants.Instance.GetCustomFloat_Slow("tooltip_text_margin_h");
        }
        
        public static void Tooltip_Width( this ArcenUI ui, int default_width, out int min, out int max )
        {
            if (EntityText.Use == WriterToUse.Formatted)
            {
                min = max = EntityText.GetNeededTooltipWidth();
            }
            else
            {
                min = 0;
                max = EntityText.GetNeededTooltipWidth();
            }
        }
    }
}
