using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public class TutorialStepCondition : Tutorial.ITutorialStepCondition
    {
        public TutorialConditionType Type;
        public Int16 PlanetByIndex = -1;
        public string PlanetByName = string.Empty;
        public Int16 PlanetByIndex2 = -1;
        public string PlanetByName2 = string.Empty;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public GameEntityTypeData EntityType = null;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public GameEntityTypeData EntityType2 = null;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public TechUpgrade Tech = null;
        private int Count;
        private int Count2;
        public SpecialEntityType SpecialEntityType= SpecialEntityType.None;
        public string Tag = string.Empty;
        public float Ratio = 0;

        public void ParseXml( ArcenXMLElement node )
        {
            string typeString = string.Empty;
            node.Fill( "type", ref typeString, true );
            this.Type = this.GetEnumFromThisDLL( node, typeString, TutorialConditionType.Invalid );

            node.Fill( "planet_by_index", ref this.PlanetByIndex, false );
            node.Fill( "planet_by_name", ref this.PlanetByName, false );
            node.Fill( "planet_by_index2", ref this.PlanetByIndex2, false );
            node.Fill( "planet_by_name2", ref this.PlanetByName2, false );
            node.Fill( "entity_type", GameEntityTypeDataTable.Instance, ref this.EntityType, false );
            node.Fill( "entity_type2", GameEntityTypeDataTable.Instance, ref this.EntityType2, false );
            node.Fill( "tech_upgrade", TechUpgradeTable.Instance, ref this.Tech, false );
            node.Fill( "count", ref this.Count, false );
            node.Fill( "count2", ref this.Count2, false );
            node.FillEnum( "special_entity_type", ref this.SpecialEntityType, false );
            node.FillEnum( "ship_tag", ref this.Tag, false );
            node.FillEnum( "ratio", ref this.Ratio, false );
        }

        #region Planet
        public Planet GetPlanet()
        {
            Planet p = null;
            if ( this.PlanetByIndex >= 0 )
                p = World_AIW2.Instance.GetPlanetByIndex( this.PlanetByIndex );
            if ( p == null )
                p = World_AIW2.Instance.GetPlanetByName( true, this.PlanetByName );
            return p;
        }

        public string PlanetName
        {
            get
            {
                Planet p = this.GetPlanet();
                if ( p == null )
                    return "null";
                else
                    return p.Name;
            }
        }
        #endregion

        #region Planet2
        public Planet GetPlanet2()
        {
            Planet p = null;
            if ( this.PlanetByIndex >= 0 )
                p = World_AIW2.Instance.GetPlanetByIndex( this.PlanetByIndex2 );
            if ( p == null )
                p = World_AIW2.Instance.GetPlanetByName( true, this.PlanetByName2 );
            return p;
        }

        public string PlanetName2
        {
            get
            {
                Planet p = this.GetPlanet2();
                if ( p == null )
                    return "null";
                else
                    return p.Name;
            }
        }
        #endregion

        #region EntityName
        public string EntityName
        {
            get
            {
                if ( this.EntityType == null )
                    return "null";
                return this.EntityType.DisplayName;
            }
        }
        #endregion

        #region EntityName2
        public string EntityName2
        {
            get
            {
                if ( this.EntityType2 == null )
                    return "null";
                return this.EntityType2.DisplayName;
            }
        }
        #endregion

        #region Count
        public int CountRequired
        {
            get
            {
                return this.Count;
            }
        }
        
        public int CountFound
        {
            get
            {
                return this.lastCountFound;
            }
        }

        public int CountRemaining
        {
            get
            {
                return Math.Max( this.Count - this.lastCountFound, 0 );
            }
        }

        public int ReverseCountRemaining
        {
            get
            {
                return Math.Max( this.lastCountFound - this.Count, 0 );
            }
        }
        #endregion

        #region Count2
        public int CountRequired2
        {
            get
            {
                return this.Count2;
            }
        }

        public int CountFound2
        {
            get
            {
                return this.lastCount2Found;
            }
        }

        public int Count2Remaining
        {
            get
            {
                return Math.Max( this.Count2 - this.lastCount2Found, 0 );
            }
        }

        public int ReverseCount2Remaining
        {
            get
            {
                return Math.Max( this.lastCount2Found - this.Count2, 0 );
            }
        }
        #endregion

        #region TechName
        public string TechName
        {
            get
            {
                if ( this.Tech == null )
                    return "null";
                return this.Tech.DisplayName;
            }
        }
        #endregion

        #region Ratio
        public string RatioAsPercent
        {
            get
            {
                return UnityEngine.Mathf.RoundToInt( this.Ratio * 100f ) + "%";
            }
        }

        public string RatioFoundAsPercent
        {
            get
            {
                return UnityEngine.Mathf.RoundToInt( this.lastRatioFound * 100f ) + "%";
            }
        }
        #endregion

        [NotForDumping]
        private int lastCountFound = 0;
        [NotForDumping]
        private int lastCount2Found = 0;
        [NotForDumping]
        private float lastRatioFound = 0;

        #region ResetAnythingForThisStepStarting
        public void ResetAnythingForThisStepStarting()
        {
            //just do these every time to keep it simple
            World_AIW2.Instance.HasDoneAnyCameraAngling = false;
            World_AIW2.Instance.HasDoneAnyCameraPanning = false;
            World_AIW2.Instance.HasDoneAnyCameraZooming = false;
        }
        #endregion

        public bool HasConditionBeenMet()
        {
            switch ( this.Type )
            {
                case TutorialConditionType.PlayerViewIsOnSpecificPlanet:
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                        return Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() == this.GetPlanet();
                    break;
                case TutorialConditionType.PlayerViewIsOnAnyPlanet:
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                        return true;
                    break;
                case TutorialConditionType.PlayerViewIsOnGalaxyMap:
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Local:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Ships )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Build:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.DirectBuild )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Tech:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Science )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Hacking:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Hacking )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Outguard:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Outguard )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Intel:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Objectives )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Journal:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Journal )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Tips:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Tips )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasSidebarMenuOpen_Fleets:
                    if ( Window_InGameSidebarBase.Current == InGameSidebarType.Fleets )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasDoneAnyOnPlanetCameraAngling:
                    if ( World_AIW2.Instance.HasDoneAnyCameraAngling )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasDoneAnyOnPlanetCameraPanning:
                    if ( World_AIW2.Instance.HasDoneAnyCameraPanning )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasDoneAnyOnPlanetCameraZooming:
                    if ( World_AIW2.Instance.HasDoneAnyCameraZooming )
                        return true;
                    break;
                case TutorialConditionType.PlayerHasCountOfSpecificShipAnywhere:
                    #region PlayerHasCountOfSpecificShipAnywhere
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                        {
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem.TypeData == this.EntityType )
                                    numFound += mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfSpecialEntityTypeAnywhere:
                    #region PlayerHasCountOfSpecialEntityTypeAnywhere
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                        {
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem.TypeData.SpecialType == this.SpecialEntityType )
                                    numFound += mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfShipTagAnywhere:
                    #region PlayerHasCountOfShipTagAnywhere
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                        {
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem.TypeData.GetHasTag( this.Tag ) )
                                    numFound += mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfAnyShipAnywhere:
                    #region PlayerHasCountOfAnyShipAnywhere
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                        {
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                numFound += mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfSpecificShipOnPlanet:
                    #region PlayerHasCountOfSpecificShipOnPlanet
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                        {
                            if ( squad.TypeData == this.EntityType )
                                numFound += 1 + squad.ExtraStackedSquadsInThis;
                            if ( squad.TypeData.IsFleetLeader )
                            {
                                Fleet fleet = squad.GetFleetOrNull_Safe();
                                if ( fleet != null )
                                {
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        if ( mem.TypeData == this.EntityType )
                                            numFound += mem.CalculateTransportedContentsCount();
                                        if ( mem.TypeData == this.EntityType && mem.NumberCreatedButNotDeployed > 0 )
                                            numFound += mem.NumberCreatedButNotDeployed;
                                    }
                                }
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfSpecialEntityTypeOnPlanet:
                    #region PlayerHasCountOfSpecialEntityTypeOnPlanet
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                        {
                            if ( squad.TypeData.SpecialType == this.SpecialEntityType )
                                numFound += 1 + squad.ExtraStackedSquadsInThis;
                            if ( squad.TypeData.IsFleetLeader )
                            {
                                Fleet fleet = squad.GetFleetOrNull_Safe();
                                if ( fleet != null )
                                {
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        if ( mem.TypeData.SpecialType == this.SpecialEntityType )
                                            numFound += mem.CalculateTransportedContentsCount();
                                        if ( mem.TypeData.SpecialType == this.SpecialEntityType && mem.NumberCreatedButNotDeployed > 0 )
                                            numFound += mem.NumberCreatedButNotDeployed;
                                    }
                                }
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfShipTagOnPlanet:
                    #region PlayerHasCountOfShipTagOnPlanet
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                        {
                            if ( squad.TypeData.GetHasTag( this.Tag ) )
                                numFound += 1 + squad.ExtraStackedSquadsInThis;
                            if ( squad.TypeData.IsFleetLeader )
                            {
                                Fleet fleet = squad.GetFleetOrNull_Safe();
                                if ( fleet != null )
                                {
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        if ( mem.TypeData.GetHasTag( this.Tag ) )
                                            numFound += mem.CalculateTransportedContentsCount();
                                        if ( mem.TypeData.GetHasTag( this.Tag ) && mem.NumberCreatedButNotDeployed > 0 )
                                            numFound += mem.NumberCreatedButNotDeployed;
                                    }
                                }
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasCountOfAnyShipOnPlanet:
                    #region PlayerHasCountOfAnyShipOnPlanet
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( GameEntity_Squad squad in pFaction.Entities.Squads() )
                        {
                            numFound += 1 + squad.ExtraStackedSquadsInThis;
                            if ( squad.TypeData.IsFleetLeader )
                            {
                                Fleet fleet = squad.GetFleetOrNull_Safe();
                                if ( fleet != null )
                                {
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        numFound += mem.CalculateTransportedContentsCount();
                                        if ( mem.NumberCreatedButNotDeployed > 0 )
                                            numFound += mem.NumberCreatedButNotDeployed;
                                    }
                                }
                            }
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasResearchedTechNumberOfTimes:
                    #region PlayerHasResearchedTechNumberOfTimes
                    {
                        if ( this.Tech == null )
                            return false;
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int upgradesSoFar = localFaction.TechUnlocks[this.Tech.RowIndexNonSim];//okay to use here, as it will be consistent per run
                        this.lastCountFound = upgradesSoFar;
                        if ( upgradesSoFar >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerTotalEnergyAtLeast:
                    #region PlayerTotalEnergyAtLeast
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        this.lastCountFound = localFaction.EnergyProduction;
                        if ( this.lastCountFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerNetEnergyAtLeast:
                    #region PlayerNetEnergyAtLeast
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        this.lastCountFound = localFaction.NetEnergy;
                        if ( this.lastCountFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerControlsAtLeastXPlanets:
                    #region PlayerControlsAtLeastXPlanets
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int controlledCount = 0;
                        foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                        {
                            if ( planet.GetControllingFaction() == localFaction )
                                controlledCount++;
                        }
                        this.lastCountFound = controlledCount;
                        if ( this.lastCountFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerControlsSpecificPlanet:
                    #region PlayerControlsSpecificPlanet
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        if ( planet.GetControllingFaction() == localFaction )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.GameIsPaused:
                    if ( World.Instance.IsPaused )
                        return true;
                    break;
                case TutorialConditionType.GameIsUnpaused:
                    if ( World.Instance.IsPaused )
                        return true;
                    break;
                case TutorialConditionType.GameIsWon:
                    if ( World.Instance.ConclusionType == CampaignConclusionType.Won )
                        return true;
                    break;
                case TutorialConditionType.GameIsLost:
                    if ( World.Instance.ConclusionType == CampaignConclusionType.Lost )
                        return true;
                    break;
                case TutorialConditionType.EnemyCombatantsOnPlanetLessThanOrEqualTo:
                    #region EnemyCombatantsOnPlanetLessThanOrEqualTo
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( PlanetFaction enemyFaction in pFaction.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                        {
                             foreach ( GameEntity_Squad squad in enemyFaction.Entities.Squads() )
                             {
                                 if ( squad.TypeData.IsCombatant )
                                     numFound += 1 + squad.ExtraStackedSquadsInThis;
                                 if ( squad.TypeData.IsFleetLeader )
                                 {
                                     Fleet fleet = squad.GetFleetOrNull_Safe();
                                     if ( fleet != null )
                                     {
                                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                        {
                                             if ( mem.TypeData.IsCombatant )
                                                 numFound += mem.CalculateTransportedContentsCount();
                                             if ( mem.NumberCreatedButNotDeployed > 0 && mem.TypeData.IsCombatant )
                                                 numFound += mem.NumberCreatedButNotDeployed;
                                         }
                                     }
                                 }
                                 if ( squad.AIReinforcementPointContents != null )
                                 {
                                     RefPair<GameEntityTypeData, int> reinforcement;
                                     for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                                     {
                                         reinforcement = squad.AIReinforcementPointContents[i];
                                         if ( reinforcement.RightItem > 0 && reinforcement.LeftItem.IsCombatant )
                                             numFound += reinforcement.RightItem;

                                     }
                                 }
                             }
                         }

                        this.lastCountFound = numFound;
                        if ( numFound <= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.EnemyShipsOfSpecificTypeOnPlanetLessThanOrEqualTo:
                    #region EnemyShipsOfSpecificTypeOnPlanetLessThanOrEqualTo
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( PlanetFaction enemyFaction in pFaction.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                        {
                            foreach ( GameEntity_Squad squad in enemyFaction.Entities.Squads() )
                            {
                                if ( squad.TypeData == this.EntityType )
                                    numFound += 1 + squad.ExtraStackedSquadsInThis;
                                if ( squad.TypeData.IsFleetLeader )
                                {
                                    Fleet fleet = squad.GetFleetOrNull_Safe();
                                    if ( fleet != null )
                                    {
                                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                        {
                                            if ( mem.TypeData == this.EntityType )
                                                numFound += mem.CalculateTransportedContentsCount();
                                            if ( mem.NumberCreatedButNotDeployed > 0 && mem.TypeData == this.EntityType )
                                                numFound += mem.NumberCreatedButNotDeployed;
                                        }
                                    }
                                }
                                if ( squad.AIReinforcementPointContents != null )
                                {
                                    RefPair<GameEntityTypeData, int> reinforcement;
                                    for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                                    {
                                        reinforcement = squad.AIReinforcementPointContents[i];
                                        if ( reinforcement.RightItem > 0 && reinforcement.LeftItem == this.EntityType )
                                            numFound += reinforcement.RightItem;

                                    }
                                }
                            }
                        }

                        this.lastCountFound = numFound;
                        if ( numFound <= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.EnemyShipsOfSpecialEntityOnPlanetLessThanOrEqualTo:
                    #region EnemyShipsOfSpecialEntityOnPlanetLessThanOrEqualTo
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( PlanetFaction enemyFaction in pFaction.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                        {
                            foreach ( GameEntity_Squad squad in enemyFaction.Entities.Squads() )
                            {
                                if ( squad.TypeData.SpecialType == this.SpecialEntityType )
                                    numFound += 1 + squad.ExtraStackedSquadsInThis;
                                if ( squad.TypeData.IsFleetLeader )
                                {
                                    Fleet fleet = squad.GetFleetOrNull_Safe();
                                    if ( fleet != null )
                                    {
                                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                        {
                                            if ( mem.TypeData.SpecialType == this.SpecialEntityType )
                                                numFound += mem.CalculateTransportedContentsCount();
                                            if ( mem.NumberCreatedButNotDeployed > 0 && mem.TypeData.SpecialType == this.SpecialEntityType )
                                                numFound += mem.NumberCreatedButNotDeployed;
                                        }
                                    }
                                }
                                if ( squad.AIReinforcementPointContents != null )
                                {
                                    RefPair<GameEntityTypeData, int> reinforcement;
                                    for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                                    {
                                        reinforcement = squad.AIReinforcementPointContents[i];
                                        if ( reinforcement.RightItem > 0 && reinforcement.LeftItem.SpecialType == this.SpecialEntityType )
                                            numFound += reinforcement.RightItem;

                                    }
                                }
                            }
                        }

                        this.lastCountFound = numFound;
                        if ( numFound <= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.EnemyShipsOfShipTagOnPlanetLessThanOrEqualTo:
                    #region EnemyShipsOfShipTagOnPlanetLessThanOrEqualTo
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localFaction );
                        if ( pFaction == null )
                            return false;

                        int numFound = 0;
                        foreach ( PlanetFaction enemyFaction in pFaction.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                        {
                            foreach ( GameEntity_Squad squad in enemyFaction.Entities.Squads() )
                            {
                                if ( squad.TypeData.GetHasTag( this.Tag ) )
                                    numFound += 1 + squad.ExtraStackedSquadsInThis;
                                if ( squad.TypeData.IsFleetLeader )
                                {
                                    Fleet fleet = squad.GetFleetOrNull_Safe();
                                    if ( fleet != null )
                                    {
                                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                        {
                                            if ( mem.TypeData.GetHasTag( this.Tag ) )
                                                numFound += mem.CalculateTransportedContentsCount();
                                            if ( mem.NumberCreatedButNotDeployed > 0 && mem.TypeData.GetHasTag( this.Tag ) )
                                                numFound += mem.NumberCreatedButNotDeployed;
                                        }
                                    }
                                }
                                if ( squad.AIReinforcementPointContents != null )
                                {
                                    RefPair<GameEntityTypeData, int> reinforcement;
                                    for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                                    {
                                        reinforcement = squad.AIReinforcementPointContents[i];
                                        if ( reinforcement.RightItem > 0 && reinforcement.LeftItem.GetHasTag( this.Tag ) )
                                            numFound += reinforcement.RightItem;

                                    }
                                }
                            }
                        }

                        this.lastCountFound = numFound;
                        if ( numFound <= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.HostileStrengthToSelfRatioOnSpecificPlanetIsAtLeast:
                    #region HostileStrengthToSelfRatioOnSpecificPlanetIsAtLeast
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        StrengthData_PlanetFaction_Stance hostileData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Hostile];
                        float hostileStrength = hostileData.TotalStrength;
                        if ( hostileStrength < 1f ) hostileStrength = 1f;
                        StrengthData_PlanetFaction_Stance selfData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Self];
                        float selfStrength = selfData.TotalStrength;
                        if ( selfStrength < 1f ) selfStrength = 1f;

                        float ratioFound = hostileStrength / selfStrength;
                        this.lastRatioFound = ratioFound;
                        if ( ratioFound >= this.Ratio )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.HostileStrengthToSelfRatioOnSpecificPlanetIsLessThan:
                    #region HostileStrengthToSelfRatioOnSpecificPlanetIsLessThan
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        StrengthData_PlanetFaction_Stance hostileData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Hostile];
                        float hostileStrength = hostileData.TotalStrength;
                        if ( hostileStrength < 1f ) hostileStrength = 1f;
                        StrengthData_PlanetFaction_Stance selfData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Self];
                        float selfStrength = selfData.TotalStrength;
                        if ( selfStrength < 1f ) selfStrength = 1f;

                        float ratioFound = hostileStrength / selfStrength;
                        this.lastRatioFound = ratioFound;
                        if ( ratioFound < this.Ratio )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsAtLeast:
                    #region HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsAtLeast
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        StrengthData_PlanetFaction_Stance hostileData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Hostile];
                        float hostileStrength = hostileData.TotalStrength;
                        if ( hostileStrength < 1f ) hostileStrength = 1f;
                        StrengthData_PlanetFaction_Stance selfData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Self];
                        StrengthData_PlanetFaction_Stance friendlyData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Friendly];
                        float selfStrength = selfData.TotalStrength + friendlyData.TotalStrength;
                        if ( selfStrength < 1f ) selfStrength = 1f;

                        float ratioFound = hostileStrength / selfStrength;
                        this.lastRatioFound = ratioFound;
                        if ( ratioFound >= this.Ratio )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsLessThan:
                    #region HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsLessThan
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        StrengthData_PlanetFaction_Stance hostileData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Hostile];
                        float hostileStrength = hostileData.TotalStrength;
                        if ( hostileStrength < 1f ) hostileStrength = 1f;
                        StrengthData_PlanetFaction_Stance selfData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Self];
                        StrengthData_PlanetFaction_Stance friendlyData = planet.GetPlanetFactionForFaction( localFaction ).DataByStance[FactionStance.Friendly];
                        float selfStrength = selfData.TotalStrength + friendlyData.TotalStrength;
                        if ( selfStrength < 1f ) selfStrength = 1f;

                        float ratioFound = hostileStrength / selfStrength;
                        this.lastRatioFound = ratioFound;
                        if ( ratioFound < this.Ratio )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasSelectedCountOfSpecificShip:
                    #region PlayerHasSelectedCountOfSpecificShip
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                        {
                            if ( selected.TypeData == this.EntityType )
                                numFound += 1 + selected.ExtraStackedSquadsInThis;
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasSelectedCountOfSpecialEntityType:
                    #region PlayerHasSelectedCountOfSpecialEntityType
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                        {
                            if ( selected.TypeData.SpecialType == this.SpecialEntityType )
                                numFound += 1 + selected.ExtraStackedSquadsInThis;
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlayerHasSelectedCountOfShipTag:
                    #region PlayerHasSelectedCountOfShipTag
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null ) //for spectator mode, should not be possible
                            localFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        int numFound = 0;
                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                        {
                            if ( selected.TypeData.GetHasTag( this.Tag ) )
                                numFound += 1 + selected.ExtraStackedSquadsInThis;
                        }
                        this.lastCountFound = numFound;
                        if ( numFound >= this.Count )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlanetHasBeenExplored:
                    #region PlanetHasBeenExplored
                    {
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlanetIsWatched:
                    #region PlanetIsWatched
                    {
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                            return true;
                    }
                    #endregion
                    break;
                case TutorialConditionType.PlanetIsPermanentlyWatched:
                    #region PlanetIsPermanentlyWatched
                    {
                        Planet planet = this.GetPlanet();
                        if ( planet == null )
                            return false;
                        if ( planet.IntelLevel >= PlanetIntelLevel.PermanentlyWatched )
                            return true;
                    }
                    #endregion
                    break;
            }
            return false;
        }

        #region GetEnumFromThisDLL
        /// <summary>
        /// Has to be duplicated here or we have problems
        /// </summary>
        public T GetEnumFromThisDLL<T>( ArcenXMLElement node, string EnumValue, T DefaultValue )
        {
            try
            {
                return (T)Enum.Parse( typeof( T ), EnumValue, true );
            }
            catch
            {
                ArcenXML.LogError( node, "Enum from dll: Unknown enum '" + typeof( T ) + "' '" + EnumValue + "'", false );
            }

            return DefaultValue;
        }
        #endregion
    }

    #region Condition Children
    /// <summary>
    /// These are here for purposes of being able to identify them by type name in our ReplaceMacro call.
    /// </summary>

    public class c1 : TutorialStepCondition { }
    public class c2 : TutorialStepCondition { }
    public class c3 : TutorialStepCondition { }
    public class c4 : TutorialStepCondition { }
    public class c5 : TutorialStepCondition { }
    public class c6 : TutorialStepCondition { }
    public class c7 : TutorialStepCondition { }
    public class c8 : TutorialStepCondition { }
    public class c9 : TutorialStepCondition { }
    #endregion

    public enum TutorialConditionType
    {
        Invalid = 0,
        PlayerViewIsOnSpecificPlanet,
        PlayerViewIsOnAnyPlanet,
        PlayerViewIsOnGalaxyMap,
        PlayerHasDoneAnyOnPlanetCameraAngling,
        PlayerHasDoneAnyOnPlanetCameraPanning,
        PlayerHasDoneAnyOnPlanetCameraZooming,
        PlayerHasSidebarMenuOpen_Local,
        PlayerHasSidebarMenuOpen_Build,
        PlayerHasSidebarMenuOpen_Tech,
        PlayerHasSidebarMenuOpen_Hacking,
        PlayerHasSidebarMenuOpen_Outguard,
        PlayerHasSidebarMenuOpen_Intel,
        PlayerHasSidebarMenuOpen_Fleets,
        PlayerHasCountOfSpecificShipAnywhere,
        PlayerHasCountOfSpecialEntityTypeAnywhere,
        PlayerHasCountOfShipTagAnywhere,
        PlayerHasCountOfAnyShipAnywhere,
        PlayerHasCountOfSpecificShipOnPlanet,
        PlayerHasCountOfSpecialEntityTypeOnPlanet,
        PlayerHasCountOfShipTagOnPlanet,
        PlayerHasCountOfAnyShipOnPlanet,
        PlayerHasResearchedTechNumberOfTimes,
        PlayerTotalEnergyAtLeast,
        PlayerNetEnergyAtLeast,
        PlayerControlsAtLeastXPlanets,
        PlayerControlsSpecificPlanet,
        GameIsPaused,
        GameIsUnpaused,
        GameIsWon,
        GameIsLost,
        EnemyCombatantsOnPlanetLessThanOrEqualTo,
        EnemyShipsOfSpecificTypeOnPlanetLessThanOrEqualTo,
        EnemyShipsOfSpecialEntityOnPlanetLessThanOrEqualTo,
        EnemyShipsOfShipTagOnPlanetLessThanOrEqualTo,
        HostileStrengthToSelfRatioOnSpecificPlanetIsAtLeast,
        HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsAtLeast,
        HostileStrengthToSelfRatioOnSpecificPlanetIsLessThan,
        HostileStrengthToSelfAndAlliedRatioOnSpecificPlanetIsLessThan,
        PlayerHasSelectedCountOfSpecificShip,
        PlayerHasSelectedCountOfSpecialEntityType,
        PlayerHasSelectedCountOfShipTag,
        PlanetHasBeenExplored,
        PlanetIsWatched,
        PlanetIsPermanentlyWatched,
        PlayerHasSidebarMenuOpen_Journal,
        PlayerHasSidebarMenuOpen_Tips,
    }

    public class TutorialStepConditionGenerator : Tutorial.ITutorialStepConditionGenerator
    {
        public Tutorial.ITutorialStepCondition ParseStepConditionFromXml( ArcenXMLElement node, Tutorial.TutorialStep step )
        {
            TutorialStepCondition condition = (TutorialStepCondition)CreateNewConditionBasedOnExistingNumberOfSteps( step );
            condition.ParseXml( node );
            return condition;
        }

        #region CreateNewConditionBasedOnExistingNumberOfSteps
        /// <summary>
        /// Each condition has its own unique class because of how ReplaceMacro works.  It looks for class names, and this was the easiest way to handle that.
        /// Yes it's a bit hacky, but time was short and this is actually really efficient in the grang scheme.
        /// Also, if you need more than 9 conditions on a single step... DoingItWrong(tm).  Good grief that would be overwhelming to the player.
        /// </summary>
        public Tutorial.ITutorialStepCondition CreateNewConditionBasedOnExistingNumberOfSteps( Tutorial.TutorialStep step )
        {
            switch ( step.Conditions.Count )
            {
                case 0:
                    return new c1();
                case 1:
                    return new c2();
                case 2:
                    return new c3();
                case 3:
                    return new c4();
                case 4:
                    return new c5();
                case 5:
                    return new c6();
                case 6:
                    return new c7();
                case 7:
                    return new c8();
                case 8:
                    return new c9();
                default:
                    throw new Exception( "Attempted to add more than " + step.Conditions.Count + " conditions to a single tutorial step.  That many isn't supported!" );
            }
        }
        #endregion

        public string ParseFinalTextToShowFromTutorialStep( Tutorial.TutorialStep step )
        {
            string text = step.TextToShow;
            for ( int i = 0; i < step.Conditions.Count; i++ )
                text = ArcenStrings.ReplaceMacro( text, step.Conditions[i] );

            return text;
        }
    }
}
