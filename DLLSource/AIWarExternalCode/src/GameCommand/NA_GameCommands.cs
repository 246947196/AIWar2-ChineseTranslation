using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //This file is used for vassal and sidekick commands
    public class GameCommand_ClaimMoon : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( command.RelatedEntityIDs.Count < 1 )
                throw new Exception( "You must pass in the moon squad you are hacking" );
                if (String.IsNullOrEmpty(command.RelatedString ) )
                throw new Exception( "You must pass in the name of the TypeData of moon you want" );
                if ( Context == null)
                throw new Exception( "You must pass in a Context to ClaimMoon" );
                GameEntity_Squad oldMoon = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );

                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRowByName( command.RelatedString );
                if ( typedata == null )
                {
                    throw new Exception("Could not find " + command.RelatedString);
                }
                debugCode = 200;
                ArcenPoint destinationPoint = oldMoon.WorldLocation;
                PlanetFaction pFaction = oldMoon.Planet.GetPlanetFactionForFaction(command.GetRelatedFaction());
                Faction faction = command.GetRelatedFaction();
                debugCode = 300;
                if ( faction == null )
                {
                    throw new Exception("Could not find faction for ClaimMoon ");
                }
                if ( pFaction == null )
                {
                    throw new Exception("Could not find pFaction for ClaimMoon ");
                }
                debugCode = 400;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                    pFaction.Faction.LooseFleet, 0, destinationPoint, Context.GetHostOnlyContext(), "GameCommand-ClaimMoon" );
                debugCode = 500;
                oldMoon.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in ClaimMoon: " + e.ToString() + " debugCode " + debugCode, Verbosity.DoNotShow );
            }
        }
    }
    public class GameCommand_RequestTerminusBuild : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            Faction faction = command.GetRelatedFaction();
            if ( faction == null )
                throw new Exception( "You must pass in the related faction for RequestTerminusBuild" );
            DarkZenithFactionBaseInfoRoot dzBaseInfo = faction.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            if ( dzBaseInfo == null )
                return;
            DZResource requested = (DZResource)command.RelatedMagnitude;
            if ( dzBaseInfo.RequestedTerminusResource == requested )
                dzBaseInfo.RequestedTerminusResource = DZResource.None; //clicking the same one again cancels the request
            else
                dzBaseInfo.RequestedTerminusResource = requested;
        }
    }
    public class GameCommand_UpdateEpistyleProduction : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs.Count < 1 )
                throw new Exception( "You must pass in an Epistyle ID to update" );
            int _eid0 = 0, _eid1 = 0;
            int _eid_i = 0;
            foreach ( var _eid_v in command.RelatedEntityIDs )
            {
                if ( _eid_i == 0 ) _eid0 = _eid_v;
                else if ( _eid_i == 1 ) { _eid1 = _eid_v; break; }
                _eid_i++;
            }
            GameEntity_Squad epistyle = World_AIW2.Instance.GetEntityByID_Squad( _eid0 );
            DarkZenithPerUnitBaseInfo dzPerUnitInfo = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            if ( command.RelatedEntityIDs.Count == 2 )
            {
                //We are updating the epistyle rally point, either to "default" or to a specific flagship
                if ( command.RelatedBools.First == true )
                {
                    //We are resetting this to use default behavour (closest flagship)
                    dzPerUnitInfo.PreferredFlagship.Clear();
                }
                else
                {
                    GameEntity_Squad flagship = World_AIW2.Instance.GetEntityByID_Squad( _eid1 );
                    dzPerUnitInfo.PreferredFlagship = LazyLoadSquadWrapper.Create(flagship);
                }
                return;
            }
            if ( command.RelatedEntityIDs.Count == 1 && String.IsNullOrEmpty( command.RelatedString ) )
            {
                //We are cycling this Epistyle's offensive production tier cap (0 = no cap)
                dzPerUnitInfo.MaxOffenseTier = (int)command.RelatedMagnitude;
                return;
            }
            if ( String.IsNullOrEmpty( command.RelatedString ) )
            {
                throw new Exception( "Failed to provide the DZ conversion to use" );
            }
            if ( command.RelatedBools.Count != 2 )
            {
                throw new Exception( "Failed to provide the 'keep conversion' and 'high priority' flags" );
            }

            DZResourceConversion conversion = DarkZenithResourceConversionTable.Instance.GetRowByName( command.RelatedString);
            dzPerUnitInfo.NextConversion = conversion;
            bool _kc = false, _hp = false;
            int _b_i = 0;
            foreach ( var _b_v in command.RelatedBools )
            {
                if ( _b_i == 0 ) _kc = _b_v;
                else if ( _b_i == 1 ) { _hp = _b_v; break; }
                _b_i++;
            }
            dzPerUnitInfo.KeepConversion = _kc;
            dzPerUnitInfo.HighPriority = _hp;
            ArcenDebugging.LogSingleLine(epistyle.ToStringWithPlanet() + " is now using " + conversion.ToString() + " as its NextConversion. Lock in? " + dzPerUnitInfo.KeepConversion + ". High Priority? " + dzPerUnitInfo.HighPriority, Verbosity.DoNotShow );
        }
    }
    public class GameCommand_DepositDZResources : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //Deposits the resources of every eligible DZ flagship currently on the epistyle's planet into
            //that epistyle. "Eligible" = the flagship has resources and the epistyle wants at least one of
            //them (same checks the deposit hack uses). Triggered by the DZ Economy sidebar deposit button.
            if ( command.RelatedEntityIDs.Count < 1 )
                throw new Exception( "You must pass in an Epistyle ID to deposit into" );
            GameEntity_Squad epistyle = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( epistyle == null || epistyle.Planet == null )
                return;
            DarkZenithPerUnitBaseInfo targetData = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            if ( targetData == null )
                return;
            Faction faction = epistyle.PlanetFaction.Faction;
            if ( faction == null )
                return;
            string log = "";
            foreach ( GameEntity_Squad flagship in faction.Squads( "DarkZenithFlagship" ) )
            {
                if ( flagship == null || flagship.Planet != epistyle.Planet )
                    continue;
                DarkZenithPerUnitBaseInfo flagshipData = flagship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( flagshipData == null || !flagshipData.HasAnyResourcesAtAll() )
                    continue;
                if ( !targetData.WantsResourcesFrom( flagshipData ) )
                    continue;
                targetData.TransferRequestedInventoryFrom( ref flagshipData, ref log );
            }
        }
    }
    public class GameCommand_UpdateVassalMission : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
                throw new Exception( "Failed to provide planet index for updating vassal missions" );
            if ( command.RelatedIntegers2.Count <= 0 )
                throw new Exception( "Failed to provide the factions we want to update missions to" );
            if ( command.RelatedEntityIDs.Count > 1 )
                throw new Exception( "At most one related entity can be passed in" );
            if ( String.IsNullOrEmpty( command.RelatedString ) )
            {
                //RelatedString is the order type name and either RelatedString2 is the option name that's currently set.
                //or RelatedBools[0] is true/false to say whether this mission is in progress
                if ( command.RelatedBools.Count != 1 && String.IsNullOrEmpty( command.RelatedString2 ) )
                    throw new Exception( "When updating the progress of an order type, you must give the order type and either order progress state or Option To Set" );
            }
            Planet planet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers.First );
            if ( planet == null )
                throw new Exception( "Invalid planet index for update vassal mission" );

            foreach ( int _ri2_v in command.RelatedIntegers2 )
            {
                Faction vassal = World_AIW2.Instance.GetFactionByIndex( _ri2_v );
                if ( !String.IsNullOrEmpty( command.RelatedString ) )
                {
                    //There are a couple ways we can update orders, we can Update (done in this code path)
                    //or add.
                    GameEntity_Squad relatedEntity = null;
                    if ( command.RelatedEntityIDs.Count > 0 )
                        relatedEntity = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
                    string orderTypeName = command.RelatedString;
                    if ( command.RelatedBools.Count == 1 )
                    {
                        bool updated = planet.UpdateMissionProgress( vassal, orderTypeName, command.RelatedBools.First, relatedEntity );
                        if ( updated )
                            continue;
                    }
                    if ( !String.IsNullOrEmpty( command.RelatedString2 ) )
                    {
                        bool updated = planet.UpdateMissionOption( vassal, orderTypeName, command.RelatedString2 );
                        if ( updated )
                            continue;
                    }
                }
            }
        }
    }
    public class GameCommand_AddVassalMission : BaseGameCommand
    {
        //This takes as arguments a faction and a number of possible options about what the mission is.
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            MissionPriority priority = (MissionPriority)command.RelatedMagnitude;

            if ( command.RelatedIntegers.Count != 1 )
                throw new Exception( "Failed to provide planet index for vassal missions" );
            if ( command.RelatedIntegers2.Count <= 0 )
                throw new Exception( "Failed to provide the factions we want to give missions to" );
            
            if ( String.IsNullOrEmpty( command.RelatedString ) )
                throw new Exception("You must pass in the RelatedString for this mission type");

            VassalOrderType orderType = VassalOrderTypeTable.Instance.GetRowByName( command.RelatedString );
            //The bools need to be redone, since now offense/defense is handled in the order type
            if ( command.RelatedBools.Count != 4 && orderType.InternalName == "AttackPlanet" )
                throw new Exception( "Failed to provide the required booleans for adding vassal attack mission" );

            Planet planet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers.First );
            if ( planet == null )
                throw new Exception( "Invalid planet index for update vassal mission" );


            //TODO (either Badger or Chris): the orderType has several booleans that need to be supported here, like
            //"ResetAllFireteamTargets" and "ClearAllOrdersOfSameCategoryWhenIssued".

            bool isSuicide = false;
            bool killCommandStation = false;
            bool forDefense = false;
            bool forOffense = false;
            if ( orderType.InternalName == "AttackPlanet" )
            {
                int _bv_i = 0;
                foreach ( var _bv_v in command.RelatedBools )
                {
                    switch ( _bv_i )
                    {
                        case 0: isSuicide = _bv_v; break;
                        case 1: killCommandStation = _bv_v; break;
                        case 2: forDefense = _bv_v; break;
                        case 3: forOffense = _bv_v; break;
                    }
                    _bv_i++;
                    if ( _bv_i >= 4 ) break;
                }
            }
            Faction liegeFaction = command.GetRelatedFaction(); //the faction who gave this order
            bool debug = true;
            if ( debug )
            {
                if ( liegeFaction != null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding missions for " + planet.Name + " command issues by " + liegeFaction.GetDisplayName() + "  forOffense? " + forOffense + ". Updating " + command.RelatedIntegers2.Count + " missions.", Verbosity.DoNotShow );
                else
                    ArcenDebugging.ArcenDebugLogSingleLine( "Updating missions for " + planet.Name + ", not issued by player. forOffense? " + forOffense + ". Updating " + command.RelatedIntegers2.Count + " missions.", Verbosity.DoNotShow );
            }

            foreach ( int _ri2_v in command.RelatedIntegers2 )
            {
                Faction vassal = World_AIW2.Instance.GetFactionByIndex( _ri2_v );
                if ( forDefense == forOffense )
                    throw new Exception( "Invalid offense/defense settings for update vassal mission" );

                // VassalMission mission = VassalMission.Create( vassal, priority, planet, forOffense, isSuicide, killCommandStation, orderType );
                // if ( liegeFaction != null )
                //     mission.LiegeFactionIndex = liegeFaction.FactionIndex;
                // planet.DeleteMissionForFactionIfPossible( vassal, orderType );
                // planet.Missions.Add( mission );
            }
        }
    }
    public class GameCommand_RemoveVassalMission : BaseGameCommand
    {
        //This takes as arguments a faction and a number of possible options about what the mission is.
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            MissionPriority priority = (MissionPriority)command.RelatedMagnitude;

            if ( command.RelatedIntegers.Count != 1 )
                throw new Exception( "Failed to provide planet index for vassal missions" );
            if ( command.RelatedIntegers2.Count <= 0 )
            {
                throw new Exception( "Failed to provide the factions we want to give missions to" );
            }
            if ( String.IsNullOrEmpty( command.RelatedString ) )
            {
                command.RelatedString = "AttackPlanet"; //TODO: remove this after fixing my MP game
                //                throw new Exception("You must specify the name of the order type");
            }
            VassalOrderType orderType = VassalOrderTypeTable.Instance.GetRowByName( command.RelatedString );
            //The bools need to be redone, since now offense/defense is handled in the order type

            Planet planet = World_AIW2.Instance.GetPlanetByIndex( (short)command.RelatedIntegers.First );
            if ( planet == null )
                throw new Exception( "Invalid planet index for update vassal mission" );

            Faction liegeFaction = command.GetRelatedFaction(); //the faction who gave this order
            bool debug = true;
            if ( debug )
            {
                if ( liegeFaction != null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Removing mission " + orderType + " for " + planet.Name + " command issues by " + liegeFaction.GetDisplayName() + "  . Updating " + command.RelatedIntegers2.Count + " missions.", Verbosity.DoNotShow );
                else
                    ArcenDebugging.ArcenDebugLogSingleLine( "Removing mission " + orderType +" for " + planet.Name + ", not issued by player. Updating " + command.RelatedIntegers2.Count + " missions.", Verbosity.DoNotShow );
            }
            //TODO: when deleting a mission from the Faction object,
            //set the MissionDeleted field on the mission so any units assigned to the mission can check
            foreach ( int _ri2_v in command.RelatedIntegers2 )
            {
                Faction vassal = World_AIW2.Instance.GetFactionByIndex( _ri2_v );
                planet.DeleteMissionForFactionIfPossible( vassal, orderType );
            }
        }
    }
}
