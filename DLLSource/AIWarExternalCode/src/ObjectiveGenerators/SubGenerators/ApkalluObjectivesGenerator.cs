using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ApkalluObjectivesGenerator
    {
        public static void CheckForApkalluObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction ) )
                    return;
                GenerateApkalluJourneyObjectives();
                GenerateDivesObjectives();
                GenerateConvergenceObjectives();
                GenerateZigguratObjectives();
                GenerateFlagshipProgressionObjectives();
                GenerateInfrastructureObjectives();
                GenerateResourceIncomeObjective();
                // GenerateZenithPowerGeneratorObjectives();
                // GenerateZenithMatterConverterObjectives();
                // GenerateGrantsAddedToCommandStationObjectives();
                // GenerateSpireArchiveObjectives();
                // GenerateAcquireHackingObjectives();
                // GenerateAcquireScienceByDestructionObjectives();
                // GenerateAcquireHackingByDestructionObjectives();
                // GenerateAcquireTechObjectives();
                // GenerateAcquireScienceAndHackingByDestructionObjectives();            
            }
            catch (Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ApkalluObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        private static Faction GetApkalluPlayerFaction()
        {
            Faction f = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( f == null || !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( f ) )
                return null;
            return f;
        }

        private static void GenerateApkalluJourneyObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            int zigguratCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                zigguratCount++;

            if ( zigguratCount == 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "AnchorTheApkallu" );
                ObjectiveCategory.AddActualObjective( obj );
            }
            else if ( zigguratCount == 1 )
            {
                ActualObjective obj1 = ActualObjective.GetFromPoolOrCreate();
                obj1.SetHook( "ResourcesOfDeep" );
                ObjectiveCategory.AddActualObjective( obj1 );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "ReclaimDepths" );
                ObjectiveCategory.AddActualObjective( obj2 );
                ActualObjective obj3 = ActualObjective.GetFromPoolOrCreate();
                obj3.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj3 );
            }
            else if ( zigguratCount == 2 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "UncoverInvasion" );
                ObjectiveCategory.AddActualObjective( obj );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj2 );
            }
            else
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "BuildDestabilizer" );
                ObjectiveCategory.AddActualObjective( obj );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj2 );
            }
        }

        private static void GenerateDivesObjectives()
        {
            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            foreach ( GameEntity_Squad nexus in mBaseInfo.Nexuses.DisplaySquads() )
            {
                if ( !nexus.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "BreachMalwareNexus" );
                objective.RelatedEntity1 = nexus;
                objective.RelatedPlanet1 = nexus.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }

            foreach ( GameEntity_Squad fissure in mBaseInfo.Fissures.DisplaySquads() )
            {
                if ( !fissure.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "MalwareFissure" );
                objective.RelatedEntity1 = fissure;
                objective.RelatedPlanet1 = fissure.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateConvergenceObjectives()
        {
            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            // Convergence is only "live" while the countdown is running or the staggered
            // main strike is still pending. Outside that window there is nothing to show.
            bool countingDown = mBaseInfo.ConvergenceCountdownEndTime != -1;
            bool mainStrikePending = mBaseInfo.ConvergenceMainStrikeTime != -1;
            if ( !countingDown && !mainStrikePending )
                return;

            // Always-shown header so the player learns about the threat even if no
            // Phasic generator is on an explored planet yet.
            ActualObjective header = ActualObjective.GetFromPoolOrCreate();
            header.SetHook( "MalwareConvergence" );
            ObjectiveCategory.AddActualObjective( header );

            // Per-generator entries (only while counting down; generators are despawned
            // once the final wave launches) so the player can route to and kill them.
            if ( !countingDown )
                return;
            foreach ( GameEntity_Squad generator in mBaseInfo.ConvergenceGenerators.DisplaySquads() )
            {
                if ( generator == null )
                    continue;
                if ( !generator.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "DestroyPhasicGenerator" );
                objective.RelatedEntity1 = generator;
                objective.RelatedPlanet1 = generator.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateZigguratObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            int claimedCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                claimedCount++;

            bool anyVisible = false;
            foreach ( GameEntity_Squad ziggurat in mBaseInfo.CorruptedZiggurats.DisplaySquads() )
            {
                if ( !ziggurat.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                anyVisible = true;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CorruptZiggurat" );
                objective.RelatedPlanet1 = ziggurat.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( !anyVisible && claimedCount < 3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FindZiggurat" );
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateFlagshipProgressionObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            int zigguratCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                zigguratCount++;

            bool hasT2 = false;
            bool hasT3 = false;
            bool hasMarkOneFlagship = false;
            foreach ( GameEntity_Squad flagship in info.Flagships.DisplaySquads() )
            {
                if ( flagship.TypeData.GetHasTag( "TierTwo" ) )
                    hasT2 = true;
                if ( flagship.TypeData.GetHasTag( "TierThree" ) )
                    hasT3 = true;
                if ( flagship.CurrentMarkLevel == 1 )
                    hasMarkOneFlagship = true;
            }

            if ( zigguratCount >= 1 && !hasT2 && !hasT3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FlagshipProgressionT2" );
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( zigguratCount >= 2 && !hasT3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FlagshipProgressionT3" );
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( hasMarkOneFlagship )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "UpgradeFlagship" );
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateInfrastructureObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            foreach ( GameEntity_Squad duru in info.Durus.DisplaySquads() )
            {
                if ( duru.FleetMembership == null || duru.FleetMembership.Fleet == null )
                    continue;
                if ( duru.FleetMembership.Fleet.CalculateRemainingCitySockets() >= 1 )
                {
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "ApkalluUnusedSockets" );
                    objective.RelatedPlanet1 = duru.Planet;
                    ObjectiveCategory.AddActualObjective( objective );
                }
            }

            foreach ( GameEntity_Squad lamassu in info.Lamassus.DisplaySquads() )
            {
                // Sockets belong to the Ziggurat fleet, not the Lamassu 鈥?find the owner Ziggurat.
                GameEntity_Squad ownerZiggurat = null;
                foreach ( GameEntity_Squad ziggurat in info.Ziggurats.DisplaySquads() )
                {
                    ApkalluPerUnitBaseInfo zigUnit = ziggurat.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                    if ( zigUnit != null && zigUnit.LamassuEntityPrimaryKeyID == lamassu.PrimaryKeyID )
                    {
                        ownerZiggurat = ziggurat;
                        break;
                    }
                }
                if ( ownerZiggurat == null || ownerZiggurat.FleetMembership == null || ownerZiggurat.FleetMembership.Fleet == null )
                    continue;
                if ( ownerZiggurat.FleetMembership.Fleet.CalculateRemainingCitySockets() >= 1 )
                {
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "LamassuUnusedSockets" );
                    objective.RelatedEntity1 = lamassu;
                    objective.RelatedPlanet1 = lamassu.Planet;
                    ObjectiveCategory.AddActualObjective( objective );
                }
            }

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ApkalluTemen" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( entity.PlanetFaction.Faction == playerFaction )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ClaimTemen" );
                objective.RelatedEntity1 = entity;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateResourceIncomeObjective()
        {
            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
            objective.SetHook( "ApkalluResourceIncome" );
            ObjectiveCategory.AddActualObjective( objective );
        }
    }


    public class BreachMalwareNexus : IObjectiveHookManager
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
                buffer.Add( "Bug in BreachMalwareNexus: null entity" );
                return;
            }
            try
            {
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( "On " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( ", it is bringing in reinforcements from the Apsu. Breaching the Nexus with a Hack will allow the Apkallu to dive briefly back into the Apsu and strike against the Malware.\n\n" );

                MalwarePerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                if ( data == null || data.Breaches.Count == 0 )
                    return;

                buffer.Add( "Available Breaches:\n", ObjectiveColors.Hint );
                for ( int i = 0; i < data.Breaches.Count; i++ )
                {
                    MalwareBreach breach = data.Breaches[i];
                    buffer.Add( "\t" ).Add( breach.DisplayName, "ffffff" );
                    if ( breach.Difficulty != MalwareBreachDifficulty.None )
                        buffer.Add( "  [" ).Add( breach.Difficulty.ToString(), "ffaaaa" ).Add( "]" );
                    if ( breach.UnlockFactionResource != ApkalluFactionResource.None )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockFactionResource.ToString(), ObjectiveColors.Reward );
                    else if ( breach.UnlockZigguratStructure != null )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockZigguratStructure.DisplayName, ObjectiveColors.Reward );
                    else if ( breach.UnlockDuruStructures.Count > 0 )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockDuruStructures[0].DisplayName, ObjectiveColors.Reward );
                    buffer.Add( "\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in BreachMalwareNexus.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class MalwareFissure : IObjectiveHookManager
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
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in MalwareFissure: null entity" );
                return;
            }
            try
            {
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( "On " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( ", it is letting the Malware bring in reinforcements from the Apsu.\n\nBreaching it with a hack will allow you to dive back into the Apsu to strike against the Malware.\n\n" );

                MalwarePerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                if ( data == null || data.Breaches.Count == 0 )
                    return;

                buffer.Add( "Available Breaches:\n", ObjectiveColors.Hint );
                for ( int i = 0; i < data.Breaches.Count; i++ )
                {
                    MalwareBreach breach = data.Breaches[i];
                    buffer.Add( "\t" ).Add( breach.DisplayName, "ffffff" );
                    if ( breach.Difficulty != MalwareBreachDifficulty.None )
                        buffer.Add( "  [" ).Add( breach.Difficulty.ToString(), "ffaaaa" ).Add( "]" );
                    if ( breach.UnlockFactionResource != ApkalluFactionResource.None )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockFactionResource.ToString(), ObjectiveColors.Reward );
                    else if ( breach.UnlockZigguratStructure != null )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockZigguratStructure.DisplayName, ObjectiveColors.Reward );
                    else if ( breach.UnlockDuruStructures.Count > 0 )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockDuruStructures[0].DisplayName, ObjectiveColors.Reward );
                    buffer.Add( "\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in MalwareFissure.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class MalwareConvergence : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            try
            {
                Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
                MalwareFactionBaseInfo mBaseInfo = malware?.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                if ( mBaseInfo == null )
                    return;
                int now = World_AIW2.Instance.GameSecond;

                buffer.Add( "The Malware are channeling a " ).Add( "Multi-Phasic Convergence", "ff5555" )
                    .Add( ": a mass strike funneled through Phasic generators they have raised across the galaxy.\n\n" );

                if ( mBaseInfo.ConvergenceCountdownEndTime != -1 )
                {
                    int remaining = mBaseInfo.ConvergenceCountdownEndTime - now;
                    if ( remaining > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( remaining );
                        buffer.Add( "The convergence completes in " ).Add( remaining.ToString(), color ).Add( " seconds.\n\n" );
                    }
                    else
                        buffer.Add( "The convergence is completing now!\n\n" );

                    buffer.Add( "Destroy the " ).Add( "Phasic Resonators and Conduits", "ffaaaa" )
                        .Add( " before then to shrink the incoming wave. Destroy every one and the convergence is cancelled outright. They are listed individually below.", ObjectiveColors.Hint );
                }
                else if ( mBaseInfo.ConvergenceMainStrikeTime != -1 )
                {
                    int remaining = mBaseInfo.ConvergenceMainStrikeTime - now;
                    buffer.Add( "The convergence wave has launched. A secondary strike already hit; the main thrust against your most developed Ziggurat lands in " )
                        .Add( remaining > 0 ? remaining.ToString() : "0", "ff8888" ).Add( " seconds. Brace your defenses.", ObjectiveColors.Hint );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in MalwareConvergence.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class DestroyPhasicGenerator : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in DestroyPhasicGenerator: null entity" );
                return;
            }
            try
            {
                bool isConduit = Objective.RelatedEntity1.TypeData.GetHasTag( "MalwarePhasicConduit" );
                buffer.Add( isConduit ? "A Malware Phasic Conduit" : "A Malware Phasic Resonator" )
                    .Add( " on " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( ".\n\n" );
                buffer.Add( isConduit
                        ? "Conduits feed the largest share of the incoming wave. "
                        : "Each Resonator adds to the incoming wave. " );
                buffer.Add( "Destroy it before the convergence completes to shrink the Multi-Phasic strike against your Ziggurats.", ObjectiveColors.Hint );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in DestroyPhasicGenerator.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class CorruptZiggurat : IObjectiveHookManager
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
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in CorruptZiggurat: null planet" );
                return;
            }
            buffer.Add( "One of the Apkallu's lost Ziggurats is on " ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add(", where it has been taken over by the Malware.\n\nBy restoring it to normal space and defeating the infection, the Apkallu will be greatly strengthened by access to higher tier resources and units.\n\nThe Ziggurat will remain permanently on this planet when brought out of solo-phase.");
        }
    }

    public class ApkalluUnusedSockets : IObjectiveHookManager
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
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in UnusedSockets: null planet" );
                return;
            }
            buffer.Add( "Your Duru on " ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add( " has unused building sockets.\n\nBuilding structures in your Duru's sockets strengthens your fleet and unlocks new capabilities." );
        }
    }

    public class ClaimTemen : IObjectiveHookManager
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
                buffer.Add( "Bug in ClaimTemen: null entity" );
                return;
            }
            buffer.Add( "An unclaimed Temen on " ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward )
                .Add( ". Hacking it claims it for the Apkallu, allowing construction of structures there and boosting resources for Pilgrims that visit. Pilgrims will get more resources the longer they travel and the more Temen they visit." );
        }
    }

    public class RallyScatteredFleet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The ambush scattered more than the Ziggurats. Parts of the Apkallu fleet were trapped in the Apsu during the strike, fighting desperately against the main Malware armada.\n\n" );
            buffer.Add( "Dive through a Malware Nexus to aid them. Bringing them into normal space will save them from the Malware, and allow them to aid you in your fight.\n\n" );
            buffer.Add( "It was a motley flotilla at the best of times. ", "ffbb88" ).Add( "Not everyone will make it.", "aa7755" );
        }
    }

    public class AnchorTheApkallu : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Apkallu sages were ambushed by the Malware, their fleet scattered and their Ziggurats seized. Help them survive and recover their people.\n\n" );
            buffer.Add( "Capturing a Ziggurat will greatly strengthen the Apkallu and unlock new capabilities. Look for corrupted Ziggurats on the map.", ObjectiveColors.Hint );
        }
    }

    public class ResourcesOfDeep : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Apkallu are far from home and must sustain their fleet in a hostile galaxy.\n\n" );
            buffer.Add( "Breaching Malware Fissures is their primary source of resources. Rescuing pilgrims, who will travel between the Temen you have claimed and return to the Ziggurats with resources from our galaxy, is important.", ObjectiveColors.Hint );
        }
    }

    public class ReclaimDepths : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "A second Ziggurat is still held by the Malware. The Apkallu need it; capturing it may reveal more about what the Malware are truly doing in this galaxy.\n\n" );
            buffer.Add( "Find and claim the next corrupted Ziggurat.", "ffaa66" );
        }
    }

    public class UncoverInvasion : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "With two Ziggurats recovered, the Apkallu have begun to understand the Malware's pattern. They are not merely raiding, they are enhancing AI infrastructure.\n\n" );
            buffer.Add( "Claim the third and final Ziggurat to fully understand how the Malware are entering this galaxy, and what can be done to stop them.", "ff9944" );
        }
    }

    public class BuildDestabilizer : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Apkallu believe the Destabilizer can overload the Nebular Splice (the Malware's engineered connection to the Apsu), and sever their ability to send forces into this galaxy.\n\n" );
            buffer.Add( "WARNING: ", "ff4400" ).Add( "Building the Destabilizer will trigger the final battle. The Malware will respond in force. Prepare accordingly.\n\n", "ffaa88" );
            buffer.Add( "The Apkallu ask for your help. They have not said what this costs them.", ObjectiveColors.Muted );
        }
    }

    public class FindZiggurat : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "There are Apkallu Ziggurats elsewhere in the galaxy, held by the Malware. None are currently visible from your explored planets.\n\n" );
            buffer.Add( "Explore further to locate a corrupted Ziggurat you can reclaim.", "bb77ee" );
        }
    }

    public class FlagshipProgressionT2 : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "With a Ziggurat recovered, the Apkallu can now unlock a Tier 2 flagship; a significant step up in combat power.\n\n" );
            buffer.Add( "Use your Nexus Breach rewards to unlock a T2 flagship form.", "44ccff" );
        }
    }

    public class FlagshipProgressionT3 : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Two Ziggurats have been recovered. The Apkallu's most powerful flagship forms are now within reach.\n\n" );
            buffer.Add( "Use your Nexus Breach rewards to unlock a T3 flagship form.", "33bbff" );
        }
    }

    public class UpgradeFlagship : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Your flagship is at Mark 1. Spending Lapis to upgrade its mark level will significantly improve its combat effectiveness.", "66ddff" );
        }
    }

    public class LamassuUnusedSockets : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in LamassuUnusedSockets: null entity" );
                return;
            }
            buffer.Add( "Your Lamassu on " ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward )
                .Add( " can be upgraded by building at its Ziggurat. Open the Build Menu on that planet to see what can be constructed." );
        }
    }

    public class ApkalluResourceIncome : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            GameEntityTypeData fissureType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApsuFissure" );
            GameEntityTypeData temenType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApkalluTemen" );
            GameEntityTypeData duruType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MajorDuru" );
            GameEntityTypeData pilgrimType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApkalluPilgrimTierOne" );

            buffer.Add( "The Apkallu generate resources through several means:\n\n" );

            if ( fissureType != null ) buffer.AddShipIconInline( fissureType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Fissure Hacks:", ObjectiveColors.Header ).Add( "\n\nBreaching a Malware Fissure with a hack can yield resources on completion. This is the primary income source.\n\n" );

            if ( temenType != null ) buffer.AddShipIconInline( temenType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Temens:", ObjectiveColors.Header ).Add( "\n\nClaimed Temens generate passive income and boost Pilgrim rewards.\n\n" );

            if ( duruType != null ) buffer.AddShipIconInline( duruType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Duru Structures:", ObjectiveColors.Header ).Add( "\n\nCertain structures built in your Duru's sockets contribute to resource generation.\n\n" );

            if ( pilgrimType != null ) buffer.AddShipIconInline( pilgrimType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Pilgrims:", ObjectiveColors.Header ).Add( "\n\nLesser and Greater Pilgrims traveling between planets earn resources on arrival to a Ziggurat." );
        }
    }

}
