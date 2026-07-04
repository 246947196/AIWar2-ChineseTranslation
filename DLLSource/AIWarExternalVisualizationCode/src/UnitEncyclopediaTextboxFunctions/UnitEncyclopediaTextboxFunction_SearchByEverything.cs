using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaTextboxFunction_SearchByEverything : IUnitEncyclopediaTextboxFunctionImplementation
    {
        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData )
        {
            var text = Window_UnitEncyclopedia.CurrentSearchText;
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var buffer = TypeData.EncyclopediaOnly_Tooltip;
            
            if (buffer == null)
                throw new ArgumentNullException("TypeData.EncyclopediaOnly_Tooltip");
            if (buffer.Builder == null)
                throw new ArgumentNullException("TypeData.EncyclopediaOnly_Tooltip.Builder");
            
            lock (buffer)
            {
                if (buffer.GetIsEmpty())
                {
                    var flags = ShipExtraDetailFlags.Encyclopedia | 
                                ShipExtraDetailFlags.HighestDetail | 
                                ShipExtraDetailFlags.PlainText;
                    
                    EntityText.GetTooltip(TypeData.EncyclopediaOnly_Tooltip, null, null, TypeData, 1, null, 0, FromSidebarType.NonSidebar_MultipleUnits, flags, 1.0f, false);
                }
                
                var idx = buffer.Builder.NextIndexOf(-1, text);
                
                /*
                if (TypeData.InternalName == "DiehardSniperGuardPost_LastStand")
                {
                    LOG.Msg("DiehardSniperGuardPost_LastStand.EncyclopediaOnly_Tooltip=\n{0}\n\nsearch={1} result={2}", buffer.ToString(), text, idx);
                }
                */
                
                return idx != -1;
            }
        }
    }
}