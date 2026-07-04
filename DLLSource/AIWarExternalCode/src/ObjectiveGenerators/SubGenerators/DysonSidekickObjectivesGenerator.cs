using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class DysonSidekickObjectivesGenerator
    {
        public static void CheckForDysonSidekickObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction ) )
                    return;
                GenerateCuendillarAsteroidObjectives();
                GenerateCuendillarPlanetoidObjectives();
                GenerateReaperChrysalisObjectives();
                GenerateDysonUnusedSocketObjectives( localFaction );
                GenerateDysonStrategyObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in DysonSidekickObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateCuendillarAsteroidObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarAsteroid" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                DysonSidekickPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null && data.CuendillarRemaining <= 0 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CuendillarAsteroid" );
                objective.RelatedEntity1 = entity;
                objective.RelatedPlanet1 = entity.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateCuendillarPlanetoidObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarPlanetoid" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                DysonSidekickPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null && data.CuendillarRemaining <= 0 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CuendillarPlanetoidObj" );
                objective.RelatedEntity1 = entity;
                objective.RelatedPlanet1 = entity.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateReaperChrysalisObjectives()
        {
            ReapersFactionBaseInfo reapers = ReapersFactionBaseInfo.Instance;
            if ( reapers == null )
                return;
            foreach ( GameEntity_Squad chrysalis in reapers.Chrysalises.DisplaySquads() )
            {
                if ( chrysalis.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ReaperChrysalisObj" );
                objective.RelatedEntity1 = chrysalis;
                objective.RelatedPlanet1 = chrysalis.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateDysonStrategyObjectives()
        {
            string[] hooks = new string[] { "DysonDrilling", "DysonReapers", "DysonMoons", "DysonRaceSpecializations", "DysonSphere" };
            foreach ( string hook in hooks )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( hook );
                ObjectiveCategory.AddActualObjective( obj );
            }
        }

        private static void GenerateDysonUnusedSocketObjectives( Faction playerFaction )
        {
            DysonSidekickFactionBaseInfo info = playerFaction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if ( info == null )
                return;
            foreach ( GameEntity_Squad stronghold in info.Strongholds.DisplaySquads() )
            {
                if ( stronghold.FleetMembership?.Fleet == null )
                    continue;
                Fleet fleet = stronghold.FleetMembership.Fleet;
                if ( fleet.CalculateRemainingCitySockets() < 1 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "DysonUnusedSockets" );
                objective.RelatedPlanet1 = stronghold.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }
    }

    public class CuendillarAsteroid : IObjectiveHookManager
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
                buffer.Add( "Bug in CuendillarAsteroid: null entity" );
                return;
            }
            try
            {
                buffer.Add( "A Cuendillar Asteroid on " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( ".\n\n" );
                DysonSidekickPerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null )
                    buffer.Add( "Cuendillar remaining: " ).Add( data.CuendillarRemaining.ToString(), "8888ff" ).Add( "\n\n" );
                buffer.Add( "Build a Cuendillar Asteroid Drill here to extract its Cuendillar and send it to your Dyson Sphere." );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in CuendillarAsteroid.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class CuendillarPlanetoid : IObjectiveHookManager
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
                buffer.Add( "Bug in CuendillarPlanetoid: null entity" );
                return;
            }
            try
            {
                buffer.Add( "A Cuendillar Planetoid on " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", "88ff88" ).Add( ".\n\n" );
                DysonSidekickPerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null )
                    buffer.Add( "Cuendillar remaining: " ).Add( data.CuendillarRemaining.ToString(), "88ff88" ).Add( "\n\n" );
                buffer.Add( "Build a Dyson Drill on the planet to extract its Cuendillar over time." );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in CuendillarPlanetoid.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    // Note: class name matches the typo in DS_ObjectiveDetailsHooks.xml (type_name="Arcen.AIW2.External.ReaperChrsyalis")
    public class ReaperChrsyalis : IObjectiveHookManager
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
                buffer.Add( "Bug in ReaperChrsyalis: null entity" );
                return;
            }
            try
            {
                buffer.Add( "A Reaper Chrysalis on " ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", "ff8888" ).Add( ".\n\n" );
                ReapersPerUnitBaseInfo rData = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if ( rData != null )
                {
                    int spawnTime = rData.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;
                    if ( spawnTime > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                        buffer.Add( "Hatches in " ).Add( spawnTime.ToString(), color ).Add( " seconds, spawning Reaper forces.\n" );
                    }
                    else
                        buffer.Add( "Hatching imminently (Reaper forces incoming!)\n" );
                    buffer.Add( "Cuendillar within: " ).Add( rData.CuendillarRemaining.ToString(), "ff4444" ).Add( "\n\n" );
                }
                buffer.Add( "Destroy this Chrysalis before it hatches to eliminate the threat and recover its Cuendillar." );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in ReaperChrsyalis.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class DysonDrilling : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Cuendillar", "e16cff" ).Add( " is your primary resource, extracted from three sources.\n\n" );
            buffer.Add( "Cuendillar Asteroids", "8888ff" ).Add( " and " ).Add( "Cuendillar Planetoids", "88ff88" ).Add( " provide a modest but steady income; enough to keep things running. Both are listed as objectives in the intel menu.\n\n" );
            buffer.Add( "Drilling an entire planet", "e16cff" ).Add( " provides large volumes of Cuendillar; enough for a real power spikes. Completely drilling a planet will Ravage it.\n\n" );
            buffer.Add( "Ravaging planets allows you to build a Dyson Sphere there; Dyson Spheres provide powerful flagships that automatically defend your planets.", "ffccff" );
        }
    }

    public class DysonReapers : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Reapers", "ff8888" ).Add( " are the primary faction-specific threat to the Dyson coalition. They hunt Cuendillar directly.\n\n" );
            buffer.Add( "Reaper Chrysalises", "ff4444" ).Add( " are the key threat to watch. Destroying a Chrysalis before it hatches eliminates the wave it would have spawned and refunds its stored Cuendillar. If a Chrysalis hatches, it can trigger a full-scale Reaper invasion.\n\n" );
            buffer.Add( "Do not ignore the Reapers entirely.", "ffcccc" ).Add( " Left unchecked they can grow extremely powerful by harvesting AI ships.\n\n" );
            buffer.Add( "Chrysalises are listed as objectives in the intel menu.", "ffaaaa" );
        }
    }

    public class DysonMoons : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Moons", "aaddff" ).Add( " are critical defensive platforms for the Dyson coalition. They provide buildable space for defenses and support structures that cannot be placed elsewhere.\n\n" );
            buffer.Add( "The Reapers periodically launch ", "ffcccc" ).Add( "Lunar Invasions", "ff8888" ).Add( ": events that spawn new Moons on the map. However, the invading Reaper force must be defeated before you can claim the Moon. These events are both a threat and an opportunity to expand your defensive network.\n\n" );
            buffer.Add( "Prioritizing Moon defense and claiming new Moons during Lunar Invasions is key to sustaining a strong late-game position.", "aaddff" );
        }
    }

    public class DysonRaceSpecializations : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "The Dyson coalition is made up of four races, each contributing different ships and resource income when you build their Strongholds.\n\n" );
            buffer.Add( "Spire", "88aaff" ).Add( " Strongholds can produce large quantities of Science.\n" );
            buffer.Add( "Zenith", "aaffaa" ).Add( " Strongholds can produce smaller quantities of both Metal and Science.\n" );
            buffer.Add( "Neinzul", "ffaaaa" ).Add( " Strongholds can produce Metal and Hacking.\n" );
            buffer.Add( "Templar", ObjectiveColors.Keyword ).Add( " Strongholds provide the best defense; Guardians in particular can defend multiple planets.\n\n" );
            buffer.Add( "Choosing your Stronghold mix is the main long-term strategic decision. You can dismantle Strongholds via the hacking menu to recover some resources and respec your composition.", "ffeecc" );
        }
    }

    public class DysonSphere : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "You can build a ", "ffccff" ).Add( "Dyson Sphere", "e16cff" ).Add( " at a Ravaged planet.\n\n" );
            buffer.Add( "Dyson Spheres produce powerful flagships that will automatically defend your empire, and they are also destinations for your Transports when drilling.\n\n" );
            buffer.Add( "Bringing the Dyson Sphere Tech to Mark 7 will cause your spheres to produce more powerful more golems, at the cost of very significant AI response.", "ffccff" );
        }
    }

    public class DysonUnusedSockets : IObjectiveHookManager
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
                buffer.Add( "Bug in DysonUnusedSockets: null planet" );
                return;
            }
            buffer.Add( "Your Stronghold on " ).Add( Objective.RelatedPlanet1.Name, "8092ff" ).Add( " has unused building sockets.\n\nFilling sockets strengthens your defenses and unlocks additional capabilities. Use the Build menu on that planet to see what you can build." );
        }
    }
}
