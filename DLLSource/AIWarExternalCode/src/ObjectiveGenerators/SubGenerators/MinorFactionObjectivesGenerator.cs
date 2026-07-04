using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    //TODO: zenith power generator
    //get science
    //colonize shit
    //make sure things are defended

    /* These are objectives coming from various minor factions.
       These objectives fall under variety of headings; Golems,
       Outguard, Capturables, destroyables, etc... They are grouped this
       way because they stem from minor factions */
    
    public static class MinorFactionObjectivesGenerator
    {
        public static void CheckForMinorFactionObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                GenerateZenithDysonSphereObjectives();
                GenerateRiskAnalyzersObjectives();
                GenerateInstigatorObjectives();
                GenerateOutguardObjectives();
                GenerateAstroTrainObjectives();
                GenerateNanocaustObjectives();
                GenerateBeaconObjectives(); //for player-invocable minor factions
                GenerateScourgeObjectives();
                GenerateSpireObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in MinorFactionObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }
        private static void GenerateBeaconObjectives()
        {
            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "Beacon" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                {
                    #region Beacon objective
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "InvestigateBeacon" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        public static List<SafeSquadWrapper> visibleScourgeArmories = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "MinorFactionObjectivesGenerator-visibleScourgeArmories" );
        public static List<SafeSquadWrapper> visibleScourgeSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "MinorFactionObjectivesGenerator-visibleScourgeSpawners" );
        public static List<SafeSquadWrapper> exploredPlanetsButNotVisibleInfrastructure = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "MinorFactionObjectivesGenerator-exploredPlanetsButNotVisibleInfrastructure" );

        private static void GenerateScourgeObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.SpecialFactionData.InternalName != "Scourge" )
                    continue;
                //this is a scourge faction; I need an objective for Allied scourge and for hostile scourge
                visibleScourgeArmories.Clear();
                visibleScourgeSpawners.Clear();
                exploredPlanetsButNotVisibleInfrastructure.Clear();
                foreach ( GameEntity_Squad entity in otherFaction.Squads( "ScourgeArmory" ) )
                {
                    if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    {
                        exploredPlanetsButNotVisibleInfrastructure.Add(entity);
                        continue;
                    }
                    visibleScourgeArmories.Add( entity );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( "ScourgeSpawner" ) )
                {
                    if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    {
                        exploredPlanetsButNotVisibleInfrastructure.Add( entity );
                        continue;
                    }
                    visibleScourgeSpawners.Add( entity );
                }
                if ( visibleScourgeArmories.Count == 0 && visibleScourgeSpawners.Count == 0 && exploredPlanetsButNotVisibleInfrastructure.Count == 0)
                    continue;
                if ( otherFaction.GetIsFriendlyTowards ( playerFaction ) )
                {
                    if ( visibleScourgeArmories.Count + visibleScourgeSpawners.Count < 5 )
                    {
                        {
                            #region Kill Scourge infrastructure
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "ExpandScourgeInfrastructure" );
                            objective.RelatedString = otherFaction.FactionCenterColor.ColorHexBrighter;
                            objective.RelatedInt1 = visibleScourgeArmories.Count;
                            objective.RelatedInt2 = visibleScourgeSpawners.Count;
                            ObjectiveCategory.AddActualObjective( objective );
                             #endregion
                        }
                    }
                    continue;
                }

                {
                    #region Kill Scourge infrastructure
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "DestroyScourgeInfrastructure" );
                    objective.RelatedString = otherFaction.FactionCenterColor.ColorHexBrighter;
                    objective.RelatedInt1 = visibleScourgeArmories.Count;
                    objective.RelatedInt2 = visibleScourgeSpawners.Count;
                    objective.RelatedInt3 = exploredPlanetsButNotVisibleInfrastructure.Count;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateSpireObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName != "AI" )
                {
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "AISpireCitadel" ) )
                    {
                        if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            {
                                #region Destroy Spire Citadel
                                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                objective.SetHook( "DestroySpireCitadel" );
                                objective.RelatedEntity1 = entity;
                                ObjectiveCategory.AddActualObjective( objective );
                                #endregion
                            }
                        }
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "AISpireResearchLab" ) )
                    {
                        if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            #region Destroy Spire Research Lab
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "DestroySpireResearchLab" );
                            objective.RelatedEntity1 = entity;
                            ObjectiveCategory.AddActualObjective( objective );
                            #endregion
                        }
                    }
                }
                if ( otherFaction.SpecialFactionData.InternalName != "FallenSpire" )
                {
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "SpireDebris" ) )
                    {
                        #region Get Spire Debris
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "GetSpireDebris" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "SpireSidekickDebris" ) )
                    {
                        #region Get Spire Debris
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "GetSpireDebris" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }

                }
            }
        }
        private static void GenerateNanocaustObjectives()
        { 
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null || otherFaction.SpecialFactionData.InternalName != "Nanocaust" )
                    continue;
                NanocaustFactionBaseInfo mgr = otherFaction.TryGetExternalBaseInfoAs<NanocaustFactionBaseInfo>();
                if ( mgr == null || mgr.Teams.GetItemCount() == 0 )
                {
                    continue;
                }
                if ( mgr.humanAllied )
                    continue; //no objectives for human allied nanocausts
                if ( !mgr.humanVision )
                {
                    {
                        #region Find Nanocaust
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "FindNanocaust" );
                        objective.RelatedString = otherFaction.FactionCenterColor.ColorHexBrighter;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                    continue;
                }
                GameEntity_Squad hive = mgr.Hive.Display.GetSquad();
                if ( hive == null || hive.Planet == null )
                    continue;
                if ( mgr.humanVision && !mgr.hasBeenHacked )
                {
                    if ( hive.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        {
                            #region Destroy Nanocaust Hive
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "DestroyNanocaustHive" );
                            objective.RelatedEntity1 = hive;
                            ObjectiveCategory.AddActualObjective( objective );
                            #endregion
                        }
                        continue;
                    }
                    else
                    {
                        {
                            #region Fight Nanocaust
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "FightNanocaustInvasion" );
                            objective.RelatedString = otherFaction.FactionCenterColor.ColorHexBrighter;
                            ObjectiveCategory.AddActualObjective( objective );
                            #endregion
                        }
                        continue;
                    }
                }
            }
        }
        private static void GenerateAstroTrainObjectives()
        {
            if ( AstroTrainsFactionBaseInfo.Instance == null )
                return;
            List<SafeSquadWrapper> Depots = GameEntity_Squad.GetTemporarySquadList( "GenerateAstroTrainObjectives-Depots", 10f );
            if ( Depots == null ) //blocked for teardown/shutdown; bail
                return;
            AstroTrainsFactionBaseInfo.Instance.GetAstroTrainDepots_Threadsafe( Depots );
            if ( Depots.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( Depots );
                return;
            }
            int unfoundDepots = 0;
            Faction factionToUse = null;
            for ( int i = 0; i < Depots.Count; i++ )
            {
                GameEntity_Squad entity = Depots[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                {
                    factionToUse = entity.GetFactionOrNull_Safe();
                    unfoundDepots++;
                    continue;
                }
                {
                    #region Destroy astro train depot
                    if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "DestroyAstroTrainDepot" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    else
                    {
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "DestroyUnknownAstroTrainDepot" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    #endregion
                }
            }
            if ( unfoundDepots > 0 )
            {
                {
                    #region Find Astro Train Depot
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "FindAstroTrainDepots" );
                    objective.RelatedString = factionToUse.FactionCenterColor.ColorHexBrighter;
                    objective.RelatedInt1 = unfoundDepots;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            GameEntity_Squad.ReleaseTemporarySquadList( Depots );
        }

        private static void GenerateOutguardObjectives()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) )
                return;

            List<SafeSquadWrapper> beaconsList = GameEntity_Squad.GetTemporarySquadList( "MinorFacObject-GenerateOutguardObjectives-beaconsList", 10f );
            if ( beaconsList == null ) //blocked for teardown/shutdown; bail
                return;
            OutguardBeaconStateForPlanet.GetOutguardBeacons( beaconsList );
            if ( beaconsList.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( beaconsList );
                return;
            }
            int unfoundBeacons = 0;
            for(int i = 0; i < beaconsList.Count; i++)
            {
                GameEntity_Squad entity = beaconsList[i].GetSquad();
                if ( entity == null || entity.Planet == null )
                    continue;
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                {
                    unfoundBeacons++;
                    continue;
                }
                if( OutguardBeaconStateForPlanet.GetHasBeaconBeenHacked(entity))
                {
                    {
                        #region Hire Outguard
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "HireOutguard" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }

                    continue; //this beacon has been found and hacked
                }
                else
                {
                    {
                        #region Contact Outguard Bases
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "HackOutguardBeacon" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                    continue;
                }
            }
            GameEntity_Squad.ReleaseTemporarySquadList( beaconsList );

            if (unfoundBeacons > 0)
            {
                {
                   #region Find Outguard Bases
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "FindOutguardBeacons" );
                    objective.RelatedInt1 = unfoundBeacons;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }

        }
        // Appends the comma-separated Outguard group names a beacon can summon, in the given color.
        // Returns false (and adds nothing) if the beacon has no associated groups. The group list is
        // populated whether or not the beacon has been hacked yet.
        internal static bool AppendOutguardGroupList( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad beacon, string color )
        {
            OutguardBeaconStateForPlanet state = beacon?.Planet?.OutguardBeaconState;
            if ( state == null )
                return false;
            bool any = false;
            for ( int i = 0; i < state.GroupsThatCanHire.Count; i++ )
            {
                OutguardGroupData group = state.GroupsThatCanHire[i];
                if ( group == null )
                    continue;
                string name = group.GetShortDisplayName();
                if ( string.IsNullOrEmpty( name ) )
                    continue;
                if ( any )
                    buffer.Add( ", " );
                buffer.Add( name, color );
                any = true;
            }
            return any;
        }

        private static void GenerateInstigatorObjectives()
        {
            List<SafeSquadWrapper> Bases = GameEntity_Squad.GetTemporarySquadList( "GenerateInstigatorObjectives-Bases", 10f );
            if ( Bases == null ) //blocked for teardown/shutdown; bail
                return;
            InstigatorFactionBaseInfo.GetInstigatorBases_Threadsafe( Bases );
            if ( Bases.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( Bases );
                return;
            }
            int unfoundBases = 0;
            for ( int i = 0; i < Bases.Count; i++ )
            {
                GameEntity_Squad entity = Bases[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                {
                    unfoundBases++;
                    continue;
                }
                {
                    //only show this if we have vision of these bases
                    #region Destroy Instigator Base
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "DestroyInstigatorBase" );
                    objective.DisplayNameBase = "Destroy ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            if ( unfoundBases > 0 )
            {
                {
                    #region Find Instigator Bases
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "FindInstigatorBases" );
                    objective.RelatedInt1 = unfoundBases;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            GameEntity_Squad.ReleaseTemporarySquadList( Bases );
        }
        private static void GenerateRiskAnalyzersObjectives()
        {
            if ( RiskAnalyzerFactionBaseInfo.Instance == null )
                return;
            List<SafeSquadWrapper> RiskAnalyzers = GameEntity_Squad.GetTemporarySquadList( "GenerateRiskAnalyzersObjectives-RiskAnalyzers", 10f );
            if ( RiskAnalyzers == null ) //blocked for teardown/shutdown; bail
                return;
            RiskAnalyzerFactionBaseInfo.GetRiskAnalyzers_Threadsafe( RiskAnalyzers );
            if ( RiskAnalyzers.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( RiskAnalyzers );
                return;
            }
            int unfoundAnalyzers = 0;
            int NetAIPChange = RiskAnalyzerFactionBaseInfo.Instance.GetNetAIPChangeForThisFiring();
            for ( int i = 0; i < RiskAnalyzers.Count; i++ )
            {
                GameEntity_Squad entity = RiskAnalyzers[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                {
                    unfoundAnalyzers++;
                    continue;
                }
                {
                    #region Destroy Instigator Base
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HandleRiskAnalyzer" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }

            if ( unfoundAnalyzers > 0 )
            {
                {
                    #region Find Risk Analyzers
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "FindRiskAnalyzers" );
                    objective.RelatedInt1 = unfoundAnalyzers;
                    objective.RelatedInt2 = NetAIPChange;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            GameEntity_Squad.ReleaseTemporarySquadList( RiskAnalyzers );
        }
        private static void GenerateZenithDysonSphereObjectives()
        {
            int debugStage = -1;
            try
            {
                int unexploredDysonSpheres = 0;
                //Do dyson sphere related objectives
                Faction factionToUse = null;
                foreach ( GameEntity_Squad sphere in World_AIW2.Instance.Squads( SphereFactionBaseInfo.Tag_ZenithSphere ) )
                {
                    debugStage = 10;
                    if ( sphere == null || sphere.Planet == null )
                        continue; //protection just in case
                    debugStage = 30;
                    if ( sphere.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    {
                        unexploredDysonSpheres++;
                        factionToUse = sphere.GetFactionOrNull_Safe();
                        continue;
                    }
                    debugStage = 40;
                    if ( sphere.Planet.GetControllingFactionType() == FactionType.AI )
                    {
                        debugStage = 50;
                        {
                            debugStage = 60;
                            #region Rescue Dyson Sphere
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "RescueDysonSphere" );
                            objective.RelatedEntity1 = sphere;
                            ObjectiveCategory.AddActualObjective( objective );
                            #endregion
                        }
                    }
                }
                    
                debugStage = 100;
                if ( unexploredDysonSpheres > 0 )
                {
                    {
                        debugStage = 100;
                        #region Find Dyson Spheres
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "FindDysonSpheres" );
                        objective.RelatedString = factionToUse.FactionCenterColor.ColorHexBrighter;
                        objective.RelatedInt1 = unexploredDysonSpheres;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in GenerateDysonSphereObjectives at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }
    }



    public class FindDysonSpheres : IObjectiveHookManager
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
            buffer.Add( "There " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "are " );
            else
                buffer.Add( "is " );
            buffer.Add( Objective.RelatedInt1 + " Dyson Spheres in the galaxy that you have not found yet. A Dyson Sphere can be a powerful ally, so explore the galaxy to find " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "them." );
            else
                buffer.Add( "it." );
        }
    }

    public class DestroySpireCitadel : IObjectiveHookManager
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
            buffer.Add( "Destroying the powerful AI Spire Citadel will give you a Spire Relic you can use to build a Spire City. The AI will respond to this relic with less vigor than usual." );
        }
    }

    public class DestroySpireResearchLab : IObjectiveHookManager
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
            buffer.Add( "Destroying the Spire Research Lab will give you a Spire Relic you can use to build a Spire City. You will not be allowed to move the Relic, and must build a city on that planet." );
        }
    }

    public class GetSpireDebris : IObjectiveHookManager
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
            buffer.Add( "Whenever you build a Spire City, some Spire Debris is generated nearby in the  galaxy. Obtaining it can grant you some resources. If you don't get it in time, another faction will claim it and become stronger." );
        }
    }

    public class DestroyDysonAntagonizer : IObjectiveHookManager
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
            buffer.Add( " All Dyson Spheres in the galaxy will attempt to destroy you until the Dyson Antagonizers are destroyed." );
        }
    }

    public class ExpandScourgeInfrastructure : IObjectiveHookManager
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
            buffer.Add( "The scourge can be a powerful ally. To let them reach their full potential you need to help defend their Armories and Spawners, and to conquer or neuter nearby planets so they can build more. Scourge structures can't be built too close to eachother, so you will need to give them space." ).Add( "\n" );
            if ( Objective.RelatedInt2 >= 1 )
                buffer.Add( "There are " ).Add( Objective.RelatedInt2 ).Add( " allied scourge spawners visible in the galaxy. " );
            if ( Objective.RelatedInt1 >= 1 )
                buffer.Add( "There are " ).Add( Objective.RelatedInt1 ).Add( " allied scourge armories visible in the galaxy. " );
        }
    }

    public class DestroyScourgeInfrastructure : IObjectiveHookManager
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
            buffer.Add( "The Scourge can be a powerful enemy.  Early on, you can reduce their strength by destroying the infrastructure they use to build and upgrade their ships.  Later on, especially if they are on a high intensity or there are high-level AIs around, you may instead need to blockade part of the galaxy from them, and periodically cull their warriors before they evolve... and before it is too late." ).Add( "\n" );
            if ( Objective.RelatedInt2 == 1 )
                buffer.Add( "There is " ).Add( Objective.RelatedInt2 ).Add( " scourge spawner visible in the galaxy.\n" );
            else if ( Objective.RelatedInt2 > 1 )
                buffer.Add( "There are " ).Add( Objective.RelatedInt2 ).Add( " scourge spawners visible in the galaxy.\n" );

            if ( Objective.RelatedInt1 == 1 )
                buffer.Add( "There is " ).Add( Objective.RelatedInt1 ).Add( " scourge armory visible in the galaxy.\n" );
            else if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "There are " ).Add( Objective.RelatedInt1 ).Add( " scourge armories visible in the galaxy.\n" );

            if ( Objective.RelatedInt3 == 1 )
                buffer.Add( "We're detecting energy signatures of " ).Add( Objective.RelatedInt3 ).Add( " scourge armory or spawner that was recently built on an explored planet. You should scout for it, since you can't see it yet." );
            else if ( Objective.RelatedInt3 > 1 )
                buffer.Add( "We're detecting energy signatures of " ).Add( Objective.RelatedInt3 ).Add( " scourge armories or spawners that was recently built on an explored planet. You should scout for them, since you can't see them yet." );
        }
    }

    public class RescueDysonSphere : IObjectiveHookManager
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
            buffer.Add( "There is a " ).Add( Objective.RelatedEntity1.TypeData.DisplayName )
                .Add( " on the planet " ).Add( Objective.RelatedEntity1.GetPlanetName_Safe() )
                .Add( " . If you free the Dyson Sphere from AI influence it's ships will assist you against enemies on nearby planets." );
        }
    }

    public class FindRiskAnalyzers : IObjectiveHookManager
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
            buffer.Add( "There " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "are " );
            else
                buffer.Add( "is " );
            buffer.Add( Objective.RelatedInt1 + " Risk Analyzers in the galaxy that you have not found yet. Risk Analyers will increase AIP every hour if the AI controls them, decrease AIP every hour if you control them, and do nothing if they are on a neutral planet.\n" )
                .Add( "Right now the expected AIP change at the next hour will be " + Objective.RelatedInt2 );
        }
    }

    public class HandleRiskAnalyzer : IObjectiveHookManager
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
            buffer.Add( "The Risk Analyzer on " ).Add( Objective.RelatedEntity1.GetPlanetName_Safe() )
                .Add( " needs to be handled. Capturing it will reduce AIP every hour. Or you can prevent the AI from generating AIP with it by destroying the AI command station on the planet or the Risk Analyzer itself" );
        }
    }

    public class FindInstigatorBases : IObjectiveHookManager
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
            buffer.Add( "There " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "are " );
            else
                buffer.Add( "is " );
            buffer.Add( Objective.RelatedInt1 ).Add( " Instigator Bases in the galaxy that you have not found yet. Each insigator base will significantly strengthen the AI in some way" );
        }
    }

    public class DestroyInstigatorBase : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            InstigatorPerUnitBaseInfo localData = entity.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();
            if ( localData == null )
                return;
            InstigatorEffectData data = InstigatorDataTable.Instance.GetRowById( localData.InstigatorEffectIndex );

            Faction facOrNull = entity.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                buffer.Add( "Invalid faction" );
                return;
            }
            InstigatorFactionBaseInfo factionData = facOrNull.GetExternalBaseInfoAs<InstigatorFactionBaseInfo>();
            Faction faction = null;

            if ( factionData.AIFactionIndexForNextSpawn != -1 )
                faction = World_AIW2.Instance.GetFactionByIndex( factionData.AIFactionIndexForNextSpawn );
            if ( faction == null )
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " found on " + entity.GetPlanetName_Safe()
                    ).Add( ". This will trigger in " ).Add( (localData.TimeForNextEffect - World_AIW2.Instance.GameSecond) );
            else
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " found on " + entity.GetPlanetName_Safe()
                    ).Add( ". " + data.GetHoverText( faction ) ).Add( ". This will trigger in "
                    ).Add( (localData.TimeForNextEffect - World_AIW2.Instance.GameSecond) );
        }
    }

    public class FindOutguardBeacons : IObjectiveHookManager
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
            buffer.Add( "There " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "are " );
            else
                buffer.Add( "is " );
            buffer.Add( Objective.RelatedInt1 ).Add( " Outguard Beacons in the galaxy that you have not found yet. Find them to establish contact with small other pockets of humanity who can be brought out of hiding to help you." );
        }
    }

    public class HireOutguard : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            buffer.AddObjectiveEntityHeader( entity, entity.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( "Contact established on " ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "." );
            buffer.Add( "\n\nYou can now hire " );
            if ( !MinorFactionObjectivesGenerator.AppendOutguardGroupList( buffer, entity, ObjectiveColors.Reward ) )
                buffer.Add( "Outguard", ObjectiveColors.Reward );
            buffer.Add( " here to fight your enemies." );
        }
    }

    public class HackOutguardBeacon : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            buffer.AddObjectiveEntityHeader( entity, entity.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( "Found on " ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "." );
            buffer.Add( "\n\nHack it to make contact with " );
            if ( !MinorFactionObjectivesGenerator.AppendOutguardGroupList( buffer, entity, ObjectiveColors.Reward ) )
                buffer.Add( "the Outguard hiding here", ObjectiveColors.Reward );
            buffer.Add( " (survivors who will then fight alongside you)." );
        }
    }

    public class FindAstroTrainDepots : IObjectiveHookManager
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
            buffer.Add( "There " );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "are " );
            else
                buffer.Add( "is " );
            buffer.Add( Objective.RelatedInt1 )
                .Add( " Astro Train Depots in the galaxy that you have not found yet. The AI will be sending trains to those depots which will have powerful benefits to the AI. Find them and destroy them!" );
        }
    }

    public class DestroyAstroTrainDepot : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            AstroTrainsPerDepotBaseInfo localData = entity.GetExternalBaseInfoAs<AstroTrainsPerDepotBaseInfo>();
            AstroTrainBehaviorType trainBehavior = AstroTrainBehaviorTypeTable.Instance.GetRowById( localData.DepotTrainBehaviorID );
            if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " found on " ).Add( entity.GetPlanetName_Safe() ).Add( ". " ).Add( trainBehavior.ToString() ).Add( ". You should blow it up" );
            else
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " is somewhere in the galaxy. You should find it and blow it up" );
        }
    }

    public class FindNanocaust : IObjectiveHookManager
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
            buffer.Add( "We're detecting some strange nanobot related energy signatures. There seems to be another powerful force out in the galaxy" );
        }
    }

    public class DestroyNanocaustHive : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            buffer.Add( "Nanocaust Hive found on " ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( ". Destroy or hack the Hive to end the invasion!" );
        }
    }

    public class FightNanocaustInvasion : IObjectiveHookManager
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
            buffer.Add( "The powerful Nanocause has invaded the galaxy. Their weapons will flood their targets with Nanobots that will take over the targets. Find the hive and destroy them!" );
        }
    }

    public class InvestigateBeacon : IObjectiveHookManager
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
            GameEntity_Squad entity = Objective.RelatedEntity1;
            buffer.Add( "Investigate " ).Add( entity.TypeData.GetDisplayName(), entity.GetFactionCenterColorHexBrighter_Safe() )
                .Add( " found on " ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "." );

        }
    }
}
