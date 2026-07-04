using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    public class BolsteringManager {
        private readonly Dictionary<string, int> shipsToAdd = Dictionary<string, int>.Create_WillNeverBeGCed( 100, "BolsteringManager-shipsToAdd" );

        public void HandleBolstering(
                string buildingTag,
                ConcurrentDictionary<SafeSquadWrapper, bool> cities,
                List<SafeSquadWrapper> bolsteredFleets,
                ArcenHostOnlySimContext Context )
        {
            List<GameEntityTypeData> cityBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull(buildingTag);

            foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in cities )
            {
                SafeSquadWrapper cityWrap = _kv.Key;
                GameEntity_Squad city = cityWrap.GetSquad();
                if ( city == null )
                    continue;
                Fleet fleetForCity = city.GetFleetOrNull_Safe();
                if ( fleetForCity == null )
                    continue;

                //what fleet is this city bolstering?  Might be null, that's okay
                Fleet fleetForMobile = fleetForCity.GetFleetBolsteredByThisCity();

                List<SafeSquadWrapper> fleetsThatCanBeBolstered = fleetForCity.GetFleetsThatCanBeBolstered();
                Fleet nearestFleet = null;
                int hopsToFleet = 999;

                foreach ( SafeSquadWrapper wrap in bolsteredFleets)
                {
                    GameEntity_Squad otherMobileFleetLeader = wrap.GetSquad();
                    if ( otherMobileFleetLeader == null )
                        continue;
                    Fleet otherMobileFleet = otherMobileFleetLeader.FleetMembership.Fleet;

                    // If we aren't bolstering any fleet, we find the nearest fleet to bolster.
                    if (fleetForMobile == null
                            && fleetsThatCanBeBolstered != null
                            && city.Planet != null
                            && otherMobileFleetLeader != null) {
                        int hops = city.Planet.GetHopsTo(otherMobileFleetLeader.Planet);
                        if (hops < hopsToFleet) {
                            hopsToFleet = hops;
                            nearestFleet = otherMobileFleet;
                        }
                    }

                    if (otherMobileFleet != fleetForMobile)
                    {
                        //this is a bolstered fleet that is NOT the one we are bolstering.
                        //time to make sure there's not anything in here that we're granting that has expired
                        //Backwards iterate because we remove matching memberships (DF's RemoveAndContinue semantics).
                        for ( int memIdx = otherMobileFleet.MemberGroupCount - 1; memIdx >= 0; memIdx-- )
                        {
                            FleetMembership mem = otherMobileFleet.GetMemberGroupAt( memIdx );
                            if ( mem == null )
                                continue;
                            //whoops, this is fed here from us!  Time to clear it, since we're not bolstering that.
                            if ( mem.FedHereFromCityFleetID == fleetForCity.FleetID )
                            {
                                mem.DespawnAllContentsFromNoLongerBolstering(Context);
                                otherMobileFleet.RemoveMemGroupExplicit( mem ); //remove the entire line!
                            }
                            //whatever this is, it's not here from our fleet, so ignore it
                        }
                    }
                };

                // Make sure we are bolstering something.
                if ( fleetForMobile == null )
                {
                    if ( nearestFleet == null )
                        continue;

                    fleetForCity.CityBolstersFleetID = nearestFleet.FleetID;
                    fleetForMobile = nearestFleet;
                }

                //if the city is crippled, buildings it contains still provide the mobile fleet stuff anyway
                //sort of.  They won't reduce cap, but won't raise it either
                bool isCityItselfDisabled = city.GetIsCrippled() || city.GetIsNonFunctional();

                shipsToAdd.Clear();
                #region City Building Construction
                if ( cityBuildings != null )
                {
                    for (int j = 0; j < cityBuildings.Count; j++) {
                        FleetMembership fleetMem = fleetForCity.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates(cityBuildings[j]);
                        if (fleetMem != null)
                        {
                            //if we EVER had any of this type, then make sure we zero it out (for ship granting buildings)
                            if (fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet != null && fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet.Length > 0 &&
                                fleetMem.TypeData.ShipTypeCountToGrantMoreOfInCustomFleet > 0)
                            {
                                if (!shipsToAdd.ContainsKey( fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet))
                                    shipsToAdd[fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet] = 0;
                            }
                        }
                        
                        //if we currently have some of this type
                        if (fleetMem != null && 
                            fleetMem.EntitiesOfFMem.GetItemCount() > 0)
                        {
                            foreach ( GameEntity_Squad e in fleetMem.Entities )
                            {
                                    if ( e == null ||
                                         e.HasBeenRemovedFromSim || 
                                         e.SecondsSpentAsRemains > 0 ||
                                         e.SelfBuildingMetalRemaining > 0 )
                                    {
                                        //only count stuff that is complete!
                                        continue; 
                                    }

                                    if (fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet != null && 
                                        fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet.Length > 0 &&
                                        fleetMem.TypeData.ShipTypeCountToGrantMoreOfInCustomFleet > 0)
                                    {
                                        //we definitely have the type from above.
                                        shipsToAdd[fleetMem.TypeData.ShipTypeNameToGrantMoreOfInCustomFleet] += fleetMem.TypeData.ShipTypeCountToGrantMoreOfInCustomFleet;
                                    }
                            }
                        }
                    }
                }
                #endregion

                //neural nets now add to our bolstered fleet
                foreach (KeyValuePair<string, int> kv in shipsToAdd) {
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound(kv.Key);
                    if (typeData == null) {
                        ArcenDebugging.ArcenDebugLogSingleLine("Could not find the ship type '" + kv.Key + "' to add to a city-fed fleet.", Verbosity.ShowAsError);
                        continue;
                    }
                    int numberToHave = kv.Value;
                    FleetMembership mem = fleetForMobile.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( typeData, fleetForCity.FleetID );
                    //increase the cap only if the city is not disabled
                    if (mem.ExplicitBaseSquadCap < numberToHave && !isCityItselfDisabled)
                        mem.ExplicitBaseSquadCap = numberToHave;
                    //decrease the cap no matter what
                    if (mem.ExplicitBaseSquadCap > numberToHave)
                        mem.ExplicitBaseSquadCap = numberToHave;
                }
            }
        }
    }
}
