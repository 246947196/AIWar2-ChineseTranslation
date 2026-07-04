using Arcen.AIW2.Core;
using System;

using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ScourgeInfusedObjectivesGenerator
    {
        public static void CheckForScourgeInfusedObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ScourgeInfusedHumanEmpireFactionBaseInfo.GetIsThisAScourgeEmpireFaction( localFaction ) )
                    return;
                ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo = localFaction.GetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
                if ( empireInfo == null )
                    return;
                ScourgeVassalFactionBaseInfo vassalInfo = GetVassalInfoSafe( empireInfo );
                GenerateScourgeInfusedStrategyObjectives( empireInfo, vassalInfo );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ScourgeInfusedObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static ScourgeVassalFactionBaseInfo GetVassalInfoSafe( ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo )
        {
            try
            {
                return empireInfo.GetVassalBaseInfo();
            }
            catch
            {
                return null;
            }
        }

        private static void GenerateScourgeInfusedStrategyObjectives( ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo, ScourgeVassalFactionBaseInfo vassalInfo )
        {
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeSpawners" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeArmories" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeCorbomite" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            if ( vassalInfo != null && vassalInfo.UpgradableInfrastructureCount.Display > 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeUpgradeInfrastructure" );
                obj.RelatedInt1 = vassalInfo.UpgradableInfrastructureCount.Display;
                ObjectiveCategory.AddActualObjective( obj );
            }

            int unlockedCount = empireInfo.UnlockedRaces.Count;
            int totalRaceCount = ScourgeTypeDataTable.Instance != null ? ScourgeTypeDataTable.Instance.Rows.Count : 7;
            if ( unlockedCount < totalRaceCount )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeUnlockRaces" );
                obj.RelatedInt1 = unlockedCount;
                obj.RelatedInt2 = totalRaceCount;
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class ScourgeSpawners : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Spawners", "ff8877" ).Add( " are the foundation of your Scourge forces.\n\n" );
            buffer.Add( "Each Spawner generates a steady stream of Scourge Warriors. The more Spawners you build and the higher their mark level, the larger and more powerful your Scourge army becomes.\n\n" );
            buffer.Add( "Spawners are built using Corbomite. Place them across the galaxy to project Scourge strength into new regions. Keep in mind that Spawners need time to mark up again if destroyed.\n\n" );
            buffer.Add( "You can upgrade Spawners via the hacking menu once they have accumulated enough experience.", ObjectiveColors.Hint );
        }
    }

    public class ScourgeArmories : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Armories", "ff8877" ).Add( " allow your Scourge Warriors to evolve.\n\n" );
            buffer.Add( "When a Warrior reaches an Armory, it can evolve into that Armory's race-specific variant; a stronger Evolved Warrior, or further into a powerful Hybrid. Higher mark Armories allow Warriors to reach higher mark levels.\n\n" );
            buffer.Add( "Each race you unlock via the tech tree enables a new type of Armory and Fortress. Build at least one Armory per unlocked race to let your Warriors specialize.\n\n" );
            buffer.Add( "Bestiaries", "ff8877" ).Add( " are a related structure: each produces a single exceptionally powerful ship to defend nearby planets. " );
            buffer.Add( "Fortresses", "ff8877" ).Add( " produce defensive fleets for their surrounding planets.", ObjectiveColors.Hint );
        }
    }

    public class ScourgeCorbomite : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Corbomite", "ff8877" ).Add( " is the resource used to build all Scourge structures.\n\n" );
            buffer.Add( "It is grown from ", ObjectiveColors.Hint ).Add( "Seeds", "ff8877" ).Add( " into ", ObjectiveColors.Hint ).Add( "Flowers", "ff8877" ).Add( ", which then produce Corbomite Crystals that can be harvested.\n\n" );
            buffer.Add( "The AI will actively try to destroy your Flowers; protecting them is important. Plant additional Seeds and Flowers to maintain a steady Corbomite income.\n\n" );
            buffer.Add( "You can also use the hacking menu on Scourge structures to perform various Corbomite-related actions.", ObjectiveColors.Hint );
        }
    }

    public class ScourgeUpgradeInfrastructure : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int count = Objective.RelatedInt1;
            buffer.Add( count.ToString(), "ffaa44" ).Add( count == 1 ? " Scourge structure has" : " Scourge structures have", ObjectiveColors.Hint ).Add( " accumulated enough experience to be upgraded.\n\n" );
            buffer.Add( "Open the hacking menu on the structure to upgrade them. Higher mark Spawners generate more warriors, higher mark Armories allow warriors to reach higher marks, and higher mark Bestiaries produce more powerful ships.\n\n" );
            buffer.Add( "Check the notification panel for the specific structures ready to upgrade.", ObjectiveColors.Hint );
        }
    }

    public class ScourgeUnlockRaces : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int unlocked = Objective.RelatedInt1;
            int total = Objective.RelatedInt2;
            buffer.Add( unlocked.ToString(), ObjectiveColors.Reward ).Add( " of " ).Add( total.ToString(), "ffaa44" ).Add( " alien races unlocked.\n\n" );
            buffer.Add( "Unlocking a race via the tech tree enables:\n" );
            buffer.Add( "  • A race-specific ", ObjectiveColors.Hint ).Add( "Armory", "ff8877" ).Add( " lets Warriors evolve into that race's variant.\n" );
            buffer.Add( "  • A race-specific ", ObjectiveColors.Hint ).Add( "Fortress", "ff8877" ).Add( " produces defensive ships of that race.\n\n" );
            buffer.Add( "Unlocking a second level of a race enables the Hybrid forms.\n\n" );
            buffer.Add( "Races available: Burlust, Evuck, Thoraxian, Peltian, Neinzul, Spire, Zenith.", ObjectiveColors.Hint );
        }
    }
}
