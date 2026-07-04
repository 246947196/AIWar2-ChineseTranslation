using Arcen.AIW2.Core;
using System;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class DarkZenithObjectivesGenerator
    {
        public static void CheckForDarkZenithObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction ) )
                    return;
                DarkZenithSidekickFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                if ( info == null )
                    return;
                GenerateDZStrategyObjectives( info );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in DarkZenithObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateDZStrategyObjectives( DarkZenithSidekickFactionBaseInfo info )
        {
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZFleetMenu" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZEconomy" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZTechTree" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZShipVariants" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            if ( info.WinterEnabled )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZFimbulwinter" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZNadir" );
                obj.RelatedInt1 = info.NadirBases.Count;
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class DZFleetMenu : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The ", ObjectiveColors.Hint ).Add( "Fleets Menu", "55aaff" ).Add( " is your primary control panel for the Dark Zenith Empire.\n\n" );
            buffer.Add( "Open it by clicking the ", ObjectiveColors.Hint ).Add( "hacking region", "55aaff" ).Add( " of the resource bar at the top of the screen.\n\n" );
            buffer.Add( "From the Fleets Menu you can:\n" );
            buffer.Add( "  • See your full ", ObjectiveColors.Hint ).Add( "economy overview", "55aaff" ).Add( ": Epistyle assignments, Terminus resource stocks, Transport activity.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Assign what each Epistyle builds", "55aaff" ).Add( ": Ships, Terminii, Structures, or Upgrades.\n" );
            buffer.Add( "  • Browse and understand your ", ObjectiveColors.Hint ).Add( "tech tree upgrades", "55aaff" ).Add( ".\n" );
            buffer.Add( "  • Track ", ObjectiveColors.Hint ).Add( "Constructor", "55aaff" ).Add( " activity and see where new economic structures are being placed.\n\n" );
            buffer.Add( "The Dark Zenith manage themselves on autopilot, but the Fleets Menu is how you direct their priorities.", ObjectiveColors.Hint );
        }
    }

    public class DZEconomy : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Dark Zenith economy runs on six resources and three structure types.\n\n" );
            buffer.Add( "Terminii", "55aaff" ).Add( " are resource extractors. Each planet can host Terminii for one or more of the six resource types: " );
            buffer.Add( "Octiron", "d3d3d3" ).Add( " (base metal), " );
            buffer.Add( "Thaumite", "10ff10" ).Add( ", " );
            buffer.Add( "Chelonium", "4444ff" ).Add( ", " );
            buffer.Add( "Alkahest", "D7BE69" ).Add( ", " );
            buffer.Add( "Izumite", "ff4422" ).Add( ", and " );
            buffer.Add( "Skrith", "8aaa8a" ).Add( ".\n\n" );
            buffer.Add( "Epistyles", "55aaff" ).Add( " are your factories. They convert stored resources into ships, new Terminii, and other structures. " );
            buffer.Add( "Epistyles cannot be built on AI-controlled planets", "ffaa44" ).Add( "; AI command station radiation disrupts them.\n\n" );
            buffer.Add( "Transports", "55aaff" ).Add( " carry resources between planets to keep Epistyles supplied. They require a " );
            buffer.Add( "contiguous chain of planets", "ffaa44" ).Add( "; the AI will snipe your transports if they can.\n\n" );
            buffer.Add( "Harvesters", "55aaff" ).Add( " work alongside Octiron Terminii to extract metal. " );
            buffer.Add( "Privateers", "55aaff" ).Add( " will steal your other transports for Pirate Epistyles, which produce extra-powerful ships.", ObjectiveColors.Hint );
        }
    }

    public class DZTechTree : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Dark Zenith tech tree is driven by ", ObjectiveColors.Hint ).Add( "variant upgrades", "55aaff" ).Add( " (unlocking special infused forms of each ship tier).\n\n" );

            buffer.Add( "Ship Tiers:\n", ObjectiveColors.Hint );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Strikecraft", "55aaff" ).Add( ": always available.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Guardian Tier", "55aaff" ).Add( ": requires 3 variant upgrades.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Dire Tier", "55aaff" ).Add( ": requires 6 variants + Guardian Tier + Mark 2.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Exo Tier", "55aaff" ).Add( ": requires 10 variants + Dire Tier + Mark 3.\n\n" );

            buffer.Add( "Mark Levels", ObjectiveColors.Hint ).Add( " unlock stronger versions of all ships:\n" );
            buffer.Add( "  Mark 2: 3 variants  •  Mark 3: 5 variants + Guardian  •  Mark 4: 7 variants\n" );
            buffer.Add( "  Mark 5: 9 variants + Dire  •  Mark 6: 11 variants  •  Mark 7: 13 variants + Exo\n\n" );

            buffer.Add( "Unlock upgrades via the ", ObjectiveColors.Hint ).Add( "hacking menu on any Epistyle", "55aaff" ).Add( ". Each new variant type you unlock counts toward the gates above, so diversify your variants rather than repeating the same ones.\n\n" );
            buffer.Add( "Epistyles can also be upgraded to produce permanent passive resource income and to raise their Terminus and build limits.", ObjectiveColors.Hint );
        }
    }

    public class DZShipVariants : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Dark Zenith ships can be infused with secondary resources to create ", ObjectiveColors.Hint ).Add( "variant forms", "55aaff" ).Add( " with special abilities. Each tier has five variants:\n\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Stout", "10ff10" ).Add( "  (Thaumite): more durable; increased hull and resilience.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Fortified", "4444ff" ).Add( "  (Chelonium): adds an extra defensive system.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Spirited", "D7BE69" ).Add( "  (Alkahest): cloaked and faster; harder to pin down.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Enraged", "ff4422" ).Add( "  (Izumite): extra weapon specialized against single powerful targets.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Sinister", "8aaa8a" ).Add( "  (Skrith): extra weapon for crowd control against massed enemies.\n\n" );
            buffer.Add( "Variants must be unlocked separately for each ship tier and chain from the previous tier's variant of the same type. " );
            buffer.Add( "Each variant you unlock also counts toward tier and mark level prerequisites.\n\n" );
            buffer.Add( "Unlock variants via the ", ObjectiveColors.Hint ).Add( "hacking menu on any Epistyle", "55aaff" ).Add( ". Unlocking all five variants for a tier is typically the fastest path to the next tier.", ObjectiveColors.Hint );
        }
    }

    public class DZFimbulwinter : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The ", ObjectiveColors.Hint ).Add( "Fimbulwinter", "55aaff" ).Add( " is a terraforming process that permanently alters planets to favor the Dark Zenith.\n\n" );
            buffer.Add( "Hjarnum", "55aaff" ).Add( " units orbit planets and gradually apply the Fimbulwinter transformation. Once complete:\n" );
            buffer.Add( "  • The planet ", ObjectiveColors.Hint ).Add( "generates Science", ObjectiveColors.Reward ).Add( ".\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Dark Zenith and allied ships", "55aaff" ).Add( " move faster on the planet.\n" );
            buffer.Add( "  • ", ObjectiveColors.Hint ).Add( "Enemy ships", "ff8877" ).Add( " entering the planet are slowed.\n\n" );
            buffer.Add( "Every safely Fimbulwintered planet generates Science; this is your primary path to Science income outside of combat kills.\n\n" );
            buffer.Add( "Hjarnum are ", ObjectiveColors.Hint ).Add( "passive", "55aaff" ).Add( " during transformation and will not fight. Protect them while they work, but the AI will generally ignore them.", ObjectiveColors.Hint );
        }
    }

    public class DZNadir : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int count = Objective.RelatedInt1;
            if ( count > 0 )
                buffer.Add( count.ToString(), "ff8877" ).Add( count == 1 ? " Nadir Base is" : " Nadir Bases are", ObjectiveColors.Hint ).Add( " present in the galaxy.\n\n" );

            buffer.Add( "Nadir Bases", "ff8877" ).Add( " are AI-deployed structures specifically designed to counter the Dark Zenith. They continuously spawn anti-DZ ships to wear down your forces and economy.\n\n" );
            buffer.Add( "Destroying a Nadir Base:\n" );
            buffer.Add( "  • Removes a persistent source of anti-DZ pressure.\n" );
            buffer.Add( "  • Yields ", ObjectiveColors.Hint ).Add( "Alkahest", "D7BE69" ).Add( " and other DZ resources.\n" );
            buffer.Add( "  • Grants ", ObjectiveColors.Hint ).Add( "Hacking points", ObjectiveColors.Reward ).Add( " for your Epistyles.\n\n" );
            buffer.Add( "Nadir Bases ", ObjectiveColors.Hint ).Add( "increase in mark level", "ff8877" ).Add( " as the AI threat escalates, so prioritize them before they become significantly harder to crack.", ObjectiveColors.Hint );
        }
    }
}
