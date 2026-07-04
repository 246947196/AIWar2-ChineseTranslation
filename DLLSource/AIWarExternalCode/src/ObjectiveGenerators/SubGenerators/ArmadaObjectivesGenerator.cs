using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ArmadaObjectivesGenerator
    {
        public static void CheckForArmadaObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction( localFaction ) )
                    return;
                ArmadaFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                if ( info == null )
                    return;
                GenerateUnusedSocketsObjectives( info );
                GenerateUnclaimedStarbaseObjectives( info );
                GenerateArmadaStrategyObjectives( info );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ArmadaObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateUnusedSocketsObjectives( ArmadaFactionBaseInfo info )
        {
            foreach ( GameEntity_Squad city in info.Starbases.DisplaySquads() )
            {
                if ( city.FleetMembership?.Fleet == null )
                    continue;
                if ( city.FleetMembership.Fleet.CalculateRemainingCitySockets() < 1 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "UnusedSockets" );
                objective.RelatedPlanet1 = city.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateUnclaimedStarbaseObjectives( ArmadaFactionBaseInfo info )
        {
            foreach ( GameEntity_Squad starbase in info.UnownedStarbases.DisplaySquads() )
            {
                if ( starbase.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ArmadaUnclaimedStarbase" );
                objective.RelatedEntity1 = starbase;
                objective.RelatedPlanet1 = starbase.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateArmadaStrategyObjectives( ArmadaFactionBaseInfo info )
        {
            if ( info.UnownedStarbases.Count > 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaClaimStarbases" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaSwarmTactics" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaMining" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaRangers" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaFightTyderian" );
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class UnusedSockets : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in UnusedSockets: null planet" );
                return;
            }
            buffer.Add( "Your Starbase on " ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add( " has unused building sockets.\n\nFilling sockets strengthens your defenses and unlocks capabilities. Open the fleet menu for that Starbase to see what you can build." );
        }
    }

    public class ArmadaUnclaimedStarbase : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ArmadaUnclaimedStarbase: null entity" );
                return;
            }
            buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
            string starbaseDescription = Objective.RelatedEntity1.TypeData.Description;
            if ( !string.IsNullOrEmpty( starbaseDescription ) )
                buffer.Add( starbaseDescription ).Add( "\n\n" );
            buffer.Add( "An unclaimed Armada Starbase on " ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward ).Add( ".\n\n" );
            buffer.Add( "Capturing it grants access to new " ).Add( "ship types", ObjectiveColors.Keyword ).Add( "." );
        }
    }

    public class ArmadaClaimStarbases : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Armada Empire can expand by claiming ", ObjectiveColors.Hint ).Add( "Starbases", "ffaa44" ).Add( " seeded across the galaxy.\n\n" );
            buffer.Add( "Each Starbase is a base of operations: it houses your fleet, produces Rangers, and anchors your mining supply lines. Starbases seeded on the map give you access to powerful new ship types, and are also cheaper to upgrade.\n\n" );
            buffer.Add( "Unclaimed Starbases are listed in the intel menu under Critical Capturables.", ObjectiveColors.Hint );
        }
    }

    public class ArmadaSwarmTactics : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Armada's signature offense is the ", ObjectiveColors.Hint ).Add( "Locust Swarm", "ffaa44" ).Add( ".\n\n" );
            buffer.Add( "Swarm Launchers", ObjectiveColors.Keyword ).Add( " generate Locusts; expendable swarm units that overwhelm enemies through numbers. More swarm launchers will generate more locusts.\n\n" );
            buffer.Add( "Swarm Lures", ObjectiveColors.Keyword ).Add( " attract locusts from all the launchers. When they arrive, the locusts soak damage that would otherwise hit your Starbases or flagships, as well as dishing out significant damage. Lures attrition once you are winning the battle.\n\n" );

            buffer.Add( "Once the lure dies, the locusts will fan out across the galaxy attacking foes.", ObjectiveColors.Hint );
        }
    }

    public class ArmadaMining : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Armada's primary income source is ", ObjectiveColors.Hint ).Add( "Mining", "ffaa44" ).Add( ".\n\n" );
            buffer.Add( "Build ", ObjectiveColors.Keyword ).Add( "Mines", ObjectiveColors.Keyword ).Add( " on hostile planets to extract resources. Each Mine automatically converts into a Transport when full and flies its cargo back to the nearest Starbase.\n\n" );
            buffer.Add( "The more planets you mine, the more income you generate. Be careful though, each Mine is vulnerable while it travels. Defending the route back to your Starbases is critical.\n\n" );
            buffer.Add( "Check the Tyderian section of the resource bar for a breakdown of your current mining income.", ObjectiveColors.Hint );
        }
    }

    public class ArmadaRangers : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Rangers", "ffaa44" ).Add( " are the Armada's auto-defense units, each tethered to a home Starbase.\n\n" );
            buffer.Add( "They automatically engage threats on their Starbase's planet without player direction. When a Starbase is lost, its Rangers are lost with it.\n\n" );
            buffer.Add( "Ranger Outposts", ObjectiveColors.Keyword ).Add( " increase your Ranger cap, letting you field more defenders per Starbase.\n" );
            buffer.Add( "Dire Ranger Outposts", ObjectiveColors.Keyword ).Add( " unlock Dire Rangers; heavier, more powerful variants suited for serious threats.\n\n" );
            buffer.Add( "Build Outposts in your Starbases' sockets to keep your defensive cap high.", ObjectiveColors.Hint );
        }
    }

    public class ArmadaFightTyderian : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Tyderian Veins", "ffaa44" ).Add( " are both a threat and an opportunity.\n\n" );
            buffer.Add( "Left alone, Veins strengthen the AI and spawn hostile units. But destroying them yields ", ObjectiveColors.Hint ).Add( "Tyderian", "ffaa44" ).Add( ", a resource used for critical Armada upgrades and building new Starbases.\n\n" );
            buffer.Add( "You can also Hack Veins for larger payouts:\n" );
            buffer.Add( "  Summon Tyderian Abomination", ObjectiveColors.Keyword ).Add( ": defeat it for a large Tyderian reward.\n" );
            buffer.Add( "  Summon Tyderian Eschaton", ObjectiveColors.Keyword ).Add( ": defeat it for Science.\n" );
            buffer.Add( "  Empower Vein", ObjectiveColors.Keyword ).Add( ": boost the Vein's yield at increased risk.\n\n" );
        }
    }

}
