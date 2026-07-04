using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class BeginnerObjectivesGenerator
    {
        public static void CheckForBeginnerObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                GenerateScienceSpendingObjectives();
                GenerateWarpGateObjectives();
                GenerateEngineerRelatedObjectives();
                GenerateEnergyRelatedObjectives();
                GenerateFlagshipCapturingObjectives();
                GenerateCPACapturingObjectives();
                GenerateARSHackingObjectives();
                GenerateEarlyExpansionObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in FlagshipObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }
        
        private static bool GetIsThisAHumanEmpireFaction( Faction faction )
        {
            if ( faction == null )
                return false;
            if ( faction.Type != FactionType.Player )
                return false;
            PlayerTypeData playerTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return false;
            switch ( playerTypeData.InternalName )
            {
                case "HumanEmpire":
                case "HumanArkEmpire":
                case "SpireInfusedEmpire":
                case "ScourgeInfusedHumanEmpire":
                case "ApkalluInfusedEmpire":
                    return true;
            }
            return false;
        }

        private static void GenerateFlagshipCapturingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numFlagships = 0;
            foreach ( GameEntity_Squad flagship in playerFaction.Squads( EntityRollupType.MobileCombatFlagships ) )
            {
                numFlagships++;
            }
            if ( numFlagships <= 1 )
            {
                {
                    #region Get Flagships
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedFlagships" );
                    ObjectiveCategory.AddActualObjective( objective);
                    #endregion
                }
            }
        }
        private static void GenerateCPACapturingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            //TSS
            if ( playerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count == 0 )
            {
                {
                    #region Get TSS
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedTSSes" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateARSHackingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            if ( playerFaction.NumARSsHacked_ForUI == 0 )
            {
                {
                    #region Get ARS
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedARSs" );
                    ObjectiveCategory.AddActualObjective(objective);
                    #endregion
                }
            }
        }
        
        private static void GenerateEnergyRelatedObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int energyRemaining = playerFaction.NetEnergy;
            if (energyRemaining <= 40000)
            {
                {
                    #region Get Energy
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedEnergy" );
                    ObjectiveCategory.AddActualObjective( objective);
                    #endregion
                }
            }
        }
        private static void GenerateEngineerRelatedObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numEngineers = 0;
            foreach ( GameEntity_Squad entity in playerFaction.Squads( "Engineer" ) )
            {
                numEngineers++;
                if ( numEngineers >= 5 )
                    break;
            }
            int minForObjective = 5;
            if(numEngineers < minForObjective)
            {
                {
                    #region Build Engineers
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "BuildEngineers" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateWarpGateObjectives()
        {
            //The rules for this are as follows:
            //If you have a Warp Gate next to your homeworld, Objective: Defend Homeworld from Waves
            //If you have a Warp Gate next to any planet, Objective: Defend planet frmo Waves
            //If you have >= 3 planets with adjacent warp gates, Objective: Gate Raiding
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            GameEntity_Squad aRandomWarpGate = null;
            int numWaveVulnerablePlanets = 0;
            int minumumForGateRaidWarning = 3;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction() != playerFaction )
                    continue;
                bool warpGateFound = false;
                //                ArcenDebugging.ArcenDebugLogSingleLine("Checking for warp gates adjacent to " + planet.Name, Verbosity.DoNotShow );
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    //Check for a warp gate on any neighbor
                    foreach ( GameEntity_Squad WarpPoint in neighbor.Squads( EntityRollupType.WarpEntryPoints ) )
                    {
                        warpGateFound = true;
                        aRandomWarpGate = WarpPoint;
                        break;
                    }
                }
                if ( warpGateFound )
                {
                    numWaveVulnerablePlanets++;
                    {
                        #region Defend from waves
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "DefendFromWaves" );
                        objective.RelatedPlanet1 = planet;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
            if ( numWaveVulnerablePlanets >= minumumForGateRaidWarning )
            {
                {
                    #region Gate Raiding
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "GateRaiding" );
                    objective.RelatedInt1 = numWaveVulnerablePlanets;
                    objective.RelatedEntity1 = aRandomWarpGate;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateScienceSpendingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;
            int WarningLevel = 8000;
            if(playerFaction.StoredScience.IntValue > WarningLevel)
            {
                {
                    #region Spend Science
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "SpendScience" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateEarlyExpansionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numPlanetsOwned = 0;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction() == playerFaction )
                    numPlanetsOwned++;
            }
            if(numPlanetsOwned < 3)
            {
                {
                    #region Capture a few planets
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "EarlyExpansion" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
    }



    public class GateRaiding : IObjectiveHookManager
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
            buffer.Add( "Gate Raiding", ObjectiveColors.Keyword ).Add( " means destroying just the " ).Add( "Warp Gate", ObjectiveColors.Keyword ).Add( " on an AI planet (without capturing the planet itself) so the AI cannot send waves through it.\n\n" );
            buffer.Add( "The AI only sends waves against planets adjacent to a Warp Gate. If you control too many planets with Warp Gates nearby, you're forced to defend all of them. Raiding those gates lets you choose where attacks will land, then fortify that location.\n\n" );
            buffer.Add( "Remember, you can also hack to destroy a Warp Gate.", "ffeecc" );
        }
    }

    public class DefendFromWaves : IObjectiveHookManager
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
            buffer.Add( "AI ", "ff8888" ).Add( "Waves", "ff8888" ).Add( " are launched through " ).Add( "Warp Gates", ObjectiveColors.Keyword ).Add( " against adjacent human planets.\n\n" );
            buffer.Add( "Planets next to a Warp Gate are at elevated risk and should be heavily fortified. Consider Gate Raiding to reduce the number of planets you need to defend.", "ffeecc" );
        }
    }

    public class EarlyExpansion : IObjectiveHookManager
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
            buffer.Add( "Capturing planets early gives you more " ).Add( "science", "7ce9ff" ).Add( ", " ).Add( "metal", "ccccee" ).Add( ", and " ).Add( "hacking", "3de799" ).Add( ". Aim for around 3-4 planets (enough to build a strong economy without raising " ).Add( "AI Progress", "ff8888" ).Add( " too quickly).\n\n" );
            buffer.Add( "Good early targets:\n" );
            buffer.Add( "  Flagships", ObjectiveColors.Keyword ).Add( ": grant new ship lines for increased offensive power.\n" );
            buffer.Add( "  Advanced Research Stations (ARSes)", ObjectiveColors.Keyword ).Add( ": hack them to grant a new ship line to the hacking fleet.\n" );
            buffer.Add( "  Turret Schematic Servers (TSSes)", ObjectiveColors.Keyword ).Add( ": hack them to give all your planets access to more turrets.\n\n" );
            buffer.Add( "Check the Intel menu and galaxy map to find these nearby.", "ffeecc" );
        }
    }

    public class SpendScience : IObjectiveHookManager
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
            buffer.Add( "Spending ", "7ce9ff" ).Add( "science", "7ce9ff" ).Add( " on tech is one of the most impactful things you can do. Focus early on one or two " ).Add( "weapon techs", ObjectiveColors.Keyword ).Add( " that benefit several of your ship lines, then branch into " ).Add( "hull tech", ObjectiveColors.Keyword ).Add( " for offense, " ).Add( "turret tech", ObjectiveColors.Keyword ).Add( " for defense, or other weapon techs as your fleet evolves.\n\n" );
            buffer.Add( "Strong early picks:\n" );
            buffer.Add( "  Forcefield 1", ObjectiveColors.Keyword ).Add( ": strengthens all forcefields, buying more time during attacks.\n" );
            buffer.Add( "  Engineers 1", ObjectiveColors.Keyword ).Add( ": makes all engineers work much faster, saving significant time.\n\n" );
            buffer.Add( "You can also invest science directly into a planet or fleet to increase its " ).Add( "mark", ObjectiveColors.Keyword ).Add( ". Critical early priority: upgrade your " ).Add( "homeworld to mark 3", ObjectiveColors.Keyword ).Add( " (the boost to its output is significant and this is one of the best early uses of science).\n\n" );
            buffer.Add( "You gain more science by capturing planets or hacking AI planets. It's fine to hold science in reserve until you've scouted more of the galaxy and know what ships and opportunities are available.", "ffeecc" );
        }
    }

    public class BuildEngineers : IObjectiveHookManager
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
            buffer.Add( "Engineers", ObjectiveColors.Keyword ).Add( " build units faster and repair ships damaged in combat. Having enough engineers on active planets makes a significant difference in how quickly you recover from attacks.\n\n" );
            buffer.Add( "You can auto-build or auto-FRD engineers across all planets via the " ).Add( "Settings → Automation", ObjectiveColors.Keyword ).Add( " menu.\n\n" );
            buffer.Add( "At mark 3, engineers gain cloaking, making them much harder to kill during fights.", "ffeecc" );
        }
    }

    public class NeedEnergy : IObjectiveHookManager
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
            buffer.Add( "The primary way to generate more " ).Add( "energy", "ffde00" ).Add( " is to build " ).Add( "Economic", ObjectiveColors.Keyword ).Add( " and " ).Add( "Logistical Command Stations", ObjectiveColors.Keyword ).Add( ". Capture more planets to place these, or convert existing Military Command Stations.\n\n" );
            buffer.Add( "If you need more energy beyond what stations provide:\n" );
            buffer.Add( "  Matter Converters", ObjectiveColors.Keyword ).Add( ": convert metal into energy.\n" );
            buffer.Add( "  Scrapping unneeded ships", ObjectiveColors.Keyword ).Add( ": provides a temporary energy boost.\n\n" );
            buffer.Add( "Increasing the mark of a Command Station also increases its energy output.", "ffeecc" );
        }
    }

    public class NeedFlagships : IObjectiveHookManager
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
            buffer.Add( "Capturing ", ObjectiveColors.Keyword ).Add( "mobile combat flagships", ObjectiveColors.Keyword ).Add( " is one of the primary ways to grow your offensive strength. Each flagship unlocks new ship lines.\n\n" );
            buffer.Add( "You can mix and match ship lines from flagships to build fleets to your taste. You can think of flagships as mutant chess pieces, each with strengths and weaknesses.\n\n" );
            buffer.Add( "Use the galaxy map or the Intel menu to find fleets worth capturing nearby.", "ffeecc" );
        }
    }

    public class NeedTSSes : IObjectiveHookManager
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
            buffer.Add( "Turret Schematic Servers", ObjectiveColors.Keyword ).Add( " (TSSes) and " ).Add( "Other Defensive Schematic Servers", ObjectiveColors.Keyword ).Add( " (ODSSes) are hacked to unlock additional turrets, minefields, and defenses for all your planets, Battlestations, and citadels.\n\n" );
            buffer.Add( "These are " ).Add( "hacked rather than captured", ObjectiveColors.Keyword ).Add( "; you don't need to hold the planet permanently. Note that destroying the AI Command Station on the planet will reduce the hacking cost.\n\n" );
            buffer.Add( "Use the galaxy map or the Intel menu to find good nearby targets.", "ffeecc" );
        }
    }

    public class NeedGCAs : IObjectiveHookManager
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
            buffer.Add( "Global Command Augmenters", ObjectiveColors.Keyword ).Add( " (GCAs) give all your planets access to additional turret slots.\n\n" );
            buffer.Add( "You can either " ).Add( "capture", ObjectiveColors.Keyword ).Add( " a GCA planet or " ).Add( "hack", ObjectiveColors.Keyword ).Add( " it for the same benefit. Hacking is cheaper if you've already captured the planet first; you don't need to hold it permanently otherwise.\n\n" );
            buffer.Add( "Use the galaxy map or the Intel menu to find good nearby targets.", "ffeecc" );
        }
    }

    public class NeedARSs : IObjectiveHookManager
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
            buffer.Add( "Hacking an " ).Add( "Advanced Research Station", ObjectiveColors.Keyword ).Add( " (ARS) grants the hacking fleet a new ship line.\n\n" );
            buffer.Add( "Hacking ARSes is a critical way to strengthen your fleets. \n\n" );
            buffer.Add( "Use the galaxy map or the Intel menu to find good nearby targets.", "ffeecc" );
        }
    }

}
