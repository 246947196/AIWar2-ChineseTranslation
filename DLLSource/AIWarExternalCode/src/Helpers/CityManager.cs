using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public static class CityManager
    {
        public delegate bool BuildingEvaluator( GameEntity_Squad city, GameEntityTypeData buildingType );

        public static void RecalculateCityBuildingContents(Fleet cityFleet, string buildMenuTag, BuildingEvaluator includeBuilding)
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //this is a host-only thing!

            try {
                List<GameEntityTypeData> cityBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( buildMenuTag );
                if ( cityBuildings == null || cityBuildings.Count <= 0 )
                    return;
                GameEntity_Squad city = cityFleet.Centerpiece.GetSquad();
                if (city == null) {
                    return;
                }
                if ( city.GetIsCrippled() || city.GetIsNonFunctional() )
                {
                    for ( int j = 0; j < cityBuildings.Count; j++ )
                    {
                        FleetMembership fleetMem = cityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( cityBuildings[j] );
                        if ( fleetMem != null ) //can't build anything!
                            fleetMem.ExplicitBaseSquadCap = 0;
                    }
                    return; //no upgrades for me right now, since I'm nonfunctional
                }

                int cityMarkLevel = city.CurrentMarkLevel;
                //okay, make sure we CAN build things based on the mark level of the city!
                for ( int j = 0; j < cityBuildings.Count; j++ )
                {
                    GameEntityTypeData buildingInfo = cityBuildings[j];
                    if ( buildingInfo == null )
                        continue;

                    if (!includeBuilding(city, buildingInfo)) {
                        continue;
                    }

                    FleetMembership fleetMem = null;
                    //if the building type requires a higher mark level of city, don't show it.
                    if ( buildingInfo.MinimumRequiredCityLevelForConstruction > cityMarkLevel )
                    {
                        fleetMem = cityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                        if ( fleetMem != null ) //can't build this!
                            fleetMem.ExplicitBaseSquadCap = 0;
                        continue;
                    }

                    int capWeShouldStartWith = buildingInfo.BaseShipCapInCustomCity;
                    if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel > 0 )
                    {
                        int levelsAboveBase = 0;
                        if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel > 0 )
                            levelsAboveBase = cityMarkLevel - buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel;
                        if ( levelsAboveBase > 0 )
                            capWeShouldStartWith += (levelsAboveBase * buildingInfo.AddedShipCapInCustomCityPerCityLevel);
                    }

                    fleetMem = cityFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                    if ( fleetMem != null ) //set the cap based on the city level!
                    {
                        fleetMem.ExplicitBaseSquadCap = capWeShouldStartWith;
                        
                        // The value seems to always be zero here and the the added
                        // cap from necromancer unlocks, is added in elsewhere
                        // when the effective cap is recalculated. So, this seems
                        // to be unneeded and causes flickering within the build menu.
                        //fleetMem.SetEffectiveSquadCap( capWeShouldStartWith );
                    }
                }
            } catch ( Exception e ) {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RecalculateCityBuildingContents " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
