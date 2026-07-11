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
                    objective.DisplayNameBase = "摧毁 ";
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
            buffer.Add( "银河中还有" );
            buffer.Add( Objective.RelatedInt1 + "个你尚未发现的戴森球。戴森球可以成为强大的盟友，所以探索银河以找到" );
            if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "它们。" );
            else
                buffer.Add( "它。" );
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
            buffer.Add( "摧毁强大的AI尖塔堡垒将给你一个尖塔遗物，可用于建造尖塔城市。AI对此遗物的反应会比平时弱。" );
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
            buffer.Add( "摧毁尖塔研究实验室将给你一个尖塔遗物，可用于建造尖塔城市。你不能移动该遗物，必须在该星球上建造城市。" );
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
            buffer.Add( "每当你建造尖塔城市时，附近的银河中会产生一些尖塔碎片。获取它可以为你提供一些资源。如果你不及时获取，其他派系会认领它并变得更强大。" );
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
            buffer.Add( "银河中的所有戴森球都会试图摧毁你，直到戴森对抗者被摧毁。" );
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
            buffer.Add( "天灾可以成为强大的盟友。要让他们发挥全部潜力，你需要帮助防御他们的军械库和生成器，并征服或中立附近的星球，以便他们建造更多。天灾建筑不能建造得太近，所以你需要给他们空间。" ).Add( "\n" );
            if ( Objective.RelatedInt2 >= 1 )
                buffer.Add( "银河中有" ).Add( Objective.RelatedInt2 ).Add( "个友方天灾生成器可见。" );
            if ( Objective.RelatedInt1 >= 1 )
                buffer.Add( "银河中有" ).Add( Objective.RelatedInt1 ).Add( "个友方天灾军械库可见。" );
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
            buffer.Add( "天灾可以成为强大的敌人。早期你可以通过摧毁他们用于建造和升级舰船的基础设施来削弱他们。后期，特别是如果他们处于高强度或周围有高级AI时，你可能需要封锁银河部分区域，并定期清除他们的战士，以免他们进化……以免为时过晚。" ).Add( "\n" );
            if ( Objective.RelatedInt2 == 1 )
                buffer.Add( "银河中可见" ).Add( Objective.RelatedInt2 ).Add( "个天灾生成器。\n" );
            else if ( Objective.RelatedInt2 > 1 )
                buffer.Add( "银河中可见" ).Add( Objective.RelatedInt2 ).Add( "个天灾生成器。\n" );

            if ( Objective.RelatedInt1 == 1 )
                buffer.Add( "银河中可见" ).Add( Objective.RelatedInt1 ).Add( "个天灾军械库。\n" );
            else if ( Objective.RelatedInt1 > 1 )
                buffer.Add( "银河中可见" ).Add( Objective.RelatedInt1 ).Add( "个天灾军械库。\n" );

            if ( Objective.RelatedInt3 == 1 )
                buffer.Add( "我们检测到一个最近在已探索星球上建造的天灾军械库或生成器的能量信号。你应该侦察它，因为你目前还看不到它。" );
            else if ( Objective.RelatedInt3 > 1 )
                buffer.Add( "我们检测到" ).Add( Objective.RelatedInt3 ).Add( "个最近在已探索星球上建造的天灾军械库或生成器的能量信号。你应该侦察它们，因为你目前还看不到它们。" );
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
            buffer.Add( "在星球" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe() )
                .Add( "上有一个" ).Add( Objective.RelatedEntity1.TypeData.DisplayName )
                .Add( "。如果你将戴森球从AI的影响中解放出来，它的舰船将协助你对抗附近星球上的敌人。" );
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
            buffer.Add( "银河中还有" + Objective.RelatedInt1 + "个你尚未发现的风险分析仪。" )
                .Add( "如果AI控制它们，风险分析仪每小时会增加AIP；如果你控制它们，每小时会减少AIP；如果在中立星球上则无效果。\n" )
                .Add( "目前下一小时的预期AIP变化为" + Objective.RelatedInt2 );
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
            buffer.Add( Objective.RelatedEntity1.GetPlanetName_Safe() + "上的风险分析仪需要处理。占领它将每小时减少AIP。或者你可以通过摧毁星球上的AI指挥站或风险分析仪本身来阻止AI利用它产生AIP" );
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
            buffer.Add( "银河中还有" + Objective.RelatedInt1 + "个你尚未发现的煽动者基地。每个煽动者基地都会以某种方式显著增强AI" );
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
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " 发现于" + entity.GetPlanetName_Safe()
                    ).Add( "。将在" ).Add( (localData.TimeForNextEffect - World_AIW2.Instance.GameSecond) ).Add( "秒后触发" );
            else
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " 发现于" + entity.GetPlanetName_Safe()
                    ).Add( "。" + data.GetHoverText( faction ) ).Add( "将在"
                    ).Add( (localData.TimeForNextEffect - World_AIW2.Instance.GameSecond) ).Add( "秒后触发" );
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
            buffer.Add( "银河中还有" + Objective.RelatedInt1 + "个你尚未发现的外围守卫信标。找到它们以与可以走出藏身处帮助你的其他小型人类群体建立联系。" );
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
            buffer.Add( "已在" ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "建立联系。" );
            buffer.Add( "\n\n你现在可以雇佣" );
            if ( !MinorFactionObjectivesGenerator.AppendOutguardGroupList( buffer, entity, ObjectiveColors.Reward ) )
                buffer.Add( "外围守卫", ObjectiveColors.Reward );
            buffer.Add( "在此为你而战。" );
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
            buffer.Add( "发现于" ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "。" );
            buffer.Add( "\n\n入侵它以与" );
            if ( !MinorFactionObjectivesGenerator.AppendOutguardGroupList( buffer, entity, ObjectiveColors.Reward ) )
                buffer.Add( "藏匿在此的外围守卫", ObjectiveColors.Reward );
            buffer.Add( "建立联系（幸存者随后将与你并肩作战）。" );
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
            buffer.Add( "银河中还有" + Objective.RelatedInt1 + "个你尚未发现的星舰列车站。" )
                .Add( "AI将向那些车站发送列车，这会给AI带来强大的好处。找到并摧毁它们！" );
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
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " 发现于" ).Add( entity.GetPlanetName_Safe() ).Add( "。" ).Add( trainBehavior.ToString() ).Add( "。你应该炸掉它" );
            else
                buffer.Add( entity.TypeData.GetDisplayName() ).Add( " 在银河的某处。你应该找到它并炸掉它" );
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
            buffer.Add( "我们检测到一些与纳米机器人相关的奇怪能量信号。银河中似乎存在另一股强大的力量" );
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
            buffer.Add( "纳米虫群蜂巢发现于" ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "。摧毁或入侵蜂巢以结束入侵！" );
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
            buffer.Add( "强大的纳米虫群已经入侵了银河。他们的武器会用纳米机器人淹没目标，从而控制目标。找到蜂巢并消灭他们！" );
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
            buffer.Add( "调查" ).Add( entity.TypeData.GetDisplayName(), entity.GetFactionCenterColorHexBrighter_Safe() )
                .Add( "，发现于" ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "。" );

        }
    }
}
