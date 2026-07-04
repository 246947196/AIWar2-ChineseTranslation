
using System.Text.RegularExpressions;

namespace Arcen.AIW2.ExternalVisualization
{
    public static class StringExtensions
    {
        public static string StripHTML( this string input )
        {
            return Regex.Replace( input, "<.*?>", string.Empty );
        }
    }
}