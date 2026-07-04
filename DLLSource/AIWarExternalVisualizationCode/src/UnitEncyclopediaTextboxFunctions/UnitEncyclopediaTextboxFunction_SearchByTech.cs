using Arcen.AIW2.Core;
using System;

using System.Text;
using System.Text.RegularExpressions;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaTextboxFunction_SearchByTech : IUnitEncyclopediaTextboxFunctionImplementation
    {
        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData )
        {
            string currentText = Window_UnitEncyclopedia.CurrentSearchText;
            if (string.IsNullOrWhiteSpace(currentText))
                return true;

            if ( TypeData.TechUpgradesThatBenefitMe.Find( t => t.DisplayName.StripHTML().Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) ) != null )
                return true;

            return false;
        }
    }
}