using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// You can have as many IObjectiveGenerator objects as you want, and simply link those
    /// up via xml in the GameData/Configuration/ObjectiveGenerator folder.  They can be in any dll, it doesn't matter!
    /// Modding is super easy that way.
    /// 
    /// For all the main game objectives, there's not much point in us having more than one generator, though,
    /// so we have just this one.  We then have sub-generator static classes set up simply for ease of organization/readability.
    /// It's easier to keep a single set of objectives in one file rather than having multiple sets of objectives all in that one file.
    /// 
    /// If you're modding, feel free to use the same pattern or do whatever you prefer.  Also a note that you're free to inject
    /// your own objectives into the core categories, or you can make up your own categories if you prefer.
    /// 
    /// If you make new victory conditions, it would be great if you'd put those into "PrimeObjectives", though, so that players only
    /// have to look in one place to find victory conditions.  You can always put a single objective into multiple categories, if you want.
    /// You don't even have to make a copy of it, just put it in each of the categories you want to use by 
    /// calling ObjectiveCategory.AddActualObjective().  Easy peasy!
    /// </summary>
    public class CoreObjectiveGenerator : IObjectiveGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() 
        { 
            //nothing to do here
        }

        //this NORMALLY only happens on a background thread, so can be long-running.
        //However, if it has been more than 0.5 seconds since you last viewed the intel tab, then it runs this directly from the main thread.
        public void GenerateObjectiveOnClientOrHost( ArcenClientOrHostSimContextCore Context )
        {
            KingObjectivesGenerator.CheckForKingObjectives_BackgroundThread_ClientOrHost();
            AdditionalWinConditionObjectivesGenerator.CheckForMiniorFactionObjectives_BackgroundThread_ClientOrHost();
            ScoutingObjectivesGenerator.CheckForScoutingObjectives_BackgroundThread_ClientOrHost();
            MinorFactionObjectivesGenerator.CheckForMinorFactionObjectives_BackgroundThread_ClientOrHost();
            ResourceObjectivesGenerator.CheckForResourceObjectives_BackgroundThread_ClientOrHost();
            BeginnerObjectivesGenerator.CheckForBeginnerObjectives_BackgroundThread_ClientOrHost();
            AIObjectivesGenerator.CheckForAIObjectives_BackgroundThread_ClientOrHost();
            FlagshipObjectivesGenerator.CheckForFlagshipObjectives_BackgroundThread_ClientOrHost();
            FlagshipObjectivesGenerator.CheckForBattlestationObjectives_BackgroundThread_ClientOrHost();

            NecromancerObjectivesGenerator.CheckForNecromancerObjectives_BackgroundThread_ClientOrHost();
            ArmadaObjectivesGenerator.CheckForArmadaObjectives_BackgroundThread_ClientOrHost();
            ApkalluObjectivesGenerator.CheckForApkalluObjectives_BackgroundThread_ClientOrHost();
            MalwareObjectivesGenerator.CheckForMalwareObjectives_BackgroundThread_ClientOrHost();
            ScourgeInfusedObjectivesGenerator.CheckForScourgeInfusedObjectives_BackgroundThread_ClientOrHost();
            DarkZenithObjectivesGenerator.CheckForDarkZenithObjectives_BackgroundThread_ClientOrHost();
            DysonSidekickObjectivesGenerator.CheckForDysonSidekickObjectives_BackgroundThread_ClientOrHost();
        }
    }
}
