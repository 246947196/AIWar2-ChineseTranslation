using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ScoutingObjectivesGenerator
    {
        public static void CheckForScoutingObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                int unexploredPlanetCount = 0;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        unexploredPlanetCount++;
                }

                //there are some planets we have not yet found
                if ( unexploredPlanetCount > 0 )
                {
                    {
                        #region Tell Me To Explore The Rest
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "ScoutUnexplored" );
                        objective.DisplayNameBase = "Scout " + unexploredPlanetCount + " More Planets";
                        objective.RelatedInt1 = unexploredPlanetCount;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ScoutingObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class ScoutUnexplored : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Winning any war requires intel, and right now yours is incomplete. " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "There are still " ).Add( Objective.RelatedInt1.ToString(), ObjectiveColors.Keyword ).Add( " completely unexplored planets out there.\n\n" );
            else
                buffer.Add( "There is still " ).Add( "one", ObjectiveColors.Keyword ).Add( " completely unexplored planet out there.\n\n" );

            buffer.Add( "An unexplored planet could contain:\n" );
            buffer.Add( "  An enemy superweapon", "ff8888" ).Add( " you need to neutralize or route around.\n" );
            buffer.Add( "  A valuable capturable", ObjectiveColors.Keyword ).Add( " worth planning a detour to claim.\n" );
            buffer.Add( "  An alternative victory path", ObjectiveColors.Keyword ).Add( " that changes your whole strategy.\n\n" );

            buffer.Add( "How to scout:\n" );
            buffer.Add( "  Destroying an AI Command Station", ObjectiveColors.Keyword ).Add( " automatically scouts nearby planets.\n" );
            buffer.Add( "  Use ", "ffeecc" ).Add( "hacking", "3de799" ).Add( " to scout from a distance without sending ships.", "ffeecc" );
        }
    }
}
