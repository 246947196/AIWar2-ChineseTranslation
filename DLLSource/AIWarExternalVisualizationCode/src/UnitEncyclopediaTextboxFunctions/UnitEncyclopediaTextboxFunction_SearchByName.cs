using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaTextboxFunction_SearchByName : IUnitEncyclopediaTextboxFunctionImplementation
    {
        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData )
        {
            string currentText = Window_UnitEncyclopedia.CurrentSearchText;
            if (string.IsNullOrWhiteSpace(currentText))
                return true;
            
            if ( TypeData.DisplayName.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) ||
                TypeData.DisplayNameForSidebar.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
            { 
                return true;
            }

            return false;
        }
    }
}