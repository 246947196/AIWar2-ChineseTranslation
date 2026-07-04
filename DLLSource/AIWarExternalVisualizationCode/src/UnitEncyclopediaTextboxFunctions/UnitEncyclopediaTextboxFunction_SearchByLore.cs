using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaTextboxFunction_SearchByLore : IUnitEncyclopediaTextboxFunctionImplementation
    {
        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData )
        {
            if ( TypeData.FullLore == null || 
                 TypeData.FullLore.Length <= 0 )
            {
                return false; 
            }

            string currentText = Window_UnitEncyclopedia.CurrentSearchText;
            if (string.IsNullOrWhiteSpace(currentText))
                return true;

            if ( TypeData.FullLore.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                return true;

            return false;
        }
    }
}