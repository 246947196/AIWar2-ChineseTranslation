using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapTextboxFunction_FindAnything : IGalaxyMapTextboxFunctionImplementation
    {
        public bool GetShouldShowPlanetNameIfNotDimmed()
        {
            return true;
        }
        public bool GetShouldShowUnitsRegardlessOfDistanceIfNotDimmed()
        {
            return true;
        }
        public bool GetShouldShowSideTextRegardlessOfDistanceIfNotDimmed()
        {
            return true;
        }
        public bool GetShouldHideNormalUnitIconsWhenInThisSearchMode()
        {
            return true;
        }

        public bool PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill )
        {
            bool debug = false;

            string currentText = PlayerAccount_AIW2.GetCurrentGalaxyMapTextboxTextSafe();
            if ( currentText == null || currentText.Length < 2 )
                return false;

            if ( planet.Name.Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                return true;

            Dictionary<GameEntityTypeData, int> typeDatasAlreadyShown = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "GalaxyMapTextboxFunction_FindUnit-typeDatasAlreadyShown", 10f );
            if ( typeDatasAlreadyShown == null ) //blocked for teardown/shutdown; bail
                return false;
            typeDatasAlreadyShown.Clear();
            if ( EntityToSkip != null )
                typeDatasAlreadyShown[EntityToSkip.TypeData] = 1;

            int idToSearch = -1;
            if (currentText.StartsWith("#"))
            {
                int num;
                if (int.TryParse(currentText.Substring(1), out num))
                {
                    idToSearch = num;
                }
            }
            
            bool IsMatch( GameEntityTypeData type )
            {
                if ( type.DisplayName.Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Looking for '{0}' in string '{1}' found match!", currentText, type.DisplayName), Verbosity.DoNotShow );
                    return true;
                }

                if ( type.DisplayNameForSidebar.Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Looking for '{0}' in string '{1}' found match!", currentText, type.DisplayNameForSidebar), Verbosity.DoNotShow );
                    return true;
                }

                if ( type.ThematicGroups.Count > 0 )
                {
                    for ( int i = 0; i < type.ThematicGroups.Count; i++ )
                    {
                        if ( type.ThematicGroups[i].DisplayName.Contains( currentText, StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Looking for '{0}' in string '{1}' found match!", currentText, type.ThematicGroups[i].DisplayName), Verbosity.DoNotShow );
                            return true;
                        }
                    }
                }

                return false;
            }

            int arrayIndexFilling = 0;
            if (idToSearch != -1)
            {
                var e = World_AIW2.Instance.GetEntityByID_Squad(idToSearch);
                if (e != null && e.Planet == planet)
                {
                    ArrayToFill[arrayIndexFilling] = e;
                    typeDatasAlreadyShown[e.TypeData] = 1;
                    arrayIndexFilling++;
                }
            }
            
            foreach ( GameEntity_Squad squad in planet.Squads() )
            {
                var data = squad.TypeData;

                if ( arrayIndexFilling >= ArrayToFill.Length )
                    break;

                if ( !squad.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;

                if (debug)
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Checking for match on '{0}'", data.DisplayName), Verbosity.DoNotShow );

                bool match = false;
                
                if ( IsMatch( data ) )
                {
                    if ( typeDatasAlreadyShown.ContainsKey( data ) )
                        continue;

                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Is a match!"), Verbosity.DoNotShow );
                    match = true;
                }
                else 
                if (data.GrantsStuffToBeAddedToPlayerFleets || data.HackableForCommandStationsAndBattleStations_DSSStyle)
                {
                    foreach (var e in squad.ShipGrantsList)
                    {
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine( string.Format("   Checking granted '{0}'", e.TypeData.DisplayName), Verbosity.DoNotShow );

                        if ( IsMatch( e.TypeData ) )
                        {
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("   Is a match!"), Verbosity.DoNotShow );

                            match = true;
                            break;
                        }
                    }
                }
                else 
                if (data.IsFleetLeader)
                {
                    foreach ( FleetMembership mem in squad.FleetMembership.Fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                    {
                        //if (debug)
                            //ArcenDebugging.ArcenDebugLogSingleLine( string.Format("   Checking fleet-member '{0}'", mem.TypeData.DisplayName), Verbosity.DoNotShow );

                        if ( IsMatch( mem.TypeData ) )
                        {
                            //if (debug)
                                //ArcenDebugging.ArcenDebugLogSingleLine( string.Format("   Is a match!"), Verbosity.DoNotShow );

                            match = true;
                            break;
                        }
                    }
                }
                else 
                if (data.GetHasTag(WildHivesFactionBaseInfo.NeinzulHiveTag))
                {
                    var info = squad.TryGetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
                    if (info != null)
                    {
                        if (!string.IsNullOrEmpty(info.BaseEntityName))
                        {
                            var row = GameEntityTypeDataTable.Instance.GetRowByName(info.BaseEntityName);
                            if (row != null)
                            {
                                if (IsMatch(row))
                                {
                                    match = true;
                                }
                            }
                        }

                        foreach ( KeyValuePair<string, short> __fl in info.FleetLines )
                        {
                                string name = __fl.Key;
                                var row = GameEntityTypeDataTable.Instance.GetRowByName( name );
                                if (row != null)
                                {
                                    if (IsMatch(row))
                                    {
                                        match = true;
                                    }
                                }
                        }
                    }
                }
                else
                if (data.GetHasTag("HackAsIfCapturable"))
                {
                    foreach (var grant in squad.EnumerateGrantedShips())
                    {
                        if (IsMatch( grant.TypeData ))
                        {
                            match = true;
                            break;
                        }
                    }
                }
                
                if (match)
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Filling array [{0}] with '{1}'", arrayIndexFilling, squad.TypeData.DisplayName), Verbosity.DoNotShow );

                    ArrayToFill[arrayIndexFilling] = squad;
                    typeDatasAlreadyShown[squad.TypeData] = 1;
                    arrayIndexFilling++;
                }

            }

            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( typeDatasAlreadyShown );

            if (arrayIndexFilling > 0)
                return true;

            return false;
        }
    }
}