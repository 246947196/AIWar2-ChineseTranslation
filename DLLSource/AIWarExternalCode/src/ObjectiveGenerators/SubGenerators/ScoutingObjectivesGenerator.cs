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
                        objective.DisplayNameBase = "侦察 " + unexploredPlanetCount + " 个更多星球";
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
            buffer.Add( "赢得任何战争都需要情报，而你的情报目前还不完整。" );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "还有 " ).Add( Objective.RelatedInt1.ToString(), ObjectiveColors.Keyword ).Add( " 个完全未探索的星球。\n\n" );
            else
                buffer.Add( "还有 " ).Add( "一个", ObjectiveColors.Keyword ).Add( " 完全未探索的星球。\n\n" );

            buffer.Add( "一个未探索的星球可能包含：\n" );
            buffer.Add( "  一个需要中和或绕过的敌方超级武器", "ff8888" ).Add( "\n" );
            buffer.Add( "  一个值得绕路去占领的有价值目标", ObjectiveColors.Keyword ).Add( "\n" );
            buffer.Add( "  一条改变你整个策略的替代胜利路径", ObjectiveColors.Keyword ).Add( "\n\n" );

            buffer.Add( "如何侦察：\n" );
            buffer.Add( "  摧毁 AI 指挥站", ObjectiveColors.Keyword ).Add( " 会自动侦察附近星球。\n" );
            buffer.Add( "  使用 ", "ffeecc" ).Add( "破解", "3de799" ).Add( " 从远处侦察，无需派遣舰船。", "ffeecc" );
        }
    }
}
