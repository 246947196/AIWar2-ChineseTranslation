using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaTextboxFunction_SearchByDescription : IUnitEncyclopediaTextboxFunctionImplementation
    {
        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData )
        {
            string currentText = Window_UnitEncyclopedia.CurrentSearchText;
            if (string.IsNullOrWhiteSpace(currentText))
                return true;
            
            if ( TypeData.Description.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) ||
                TypeData.DescriptionShort.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                return true;

            return false;
        }
    }
}