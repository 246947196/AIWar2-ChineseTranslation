using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    /* To add a new part to the tutorial, you must do the following.
       Add a ConditionGroup (or pick an existing one)
       Add a Condition
       Update GetConditionsInGroup to add your Condition to the appropriate Group
       Update the GetConditionIsMet() function to detect your condition
       Add appropriate text to WriteCurrentOngoingMessageToDisplay

     */
    
    public class Scenario_Tutorial_01 : BaseScenario
    {
        protected override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner()
        {
            //not sure any of this is needed, but just in case
            result_GetConditionsInGroup.Clear();
        }

        public static Scenario_Tutorial_01 Instance;
        public Scenario_Tutorial_01() { Instance = this; }

        public override bool IsTutorial()
        {
            return true;
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {

        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            
        }

        public override bool GetShouldSkipGameSetup()
        {
            return true;
        }

        public enum ConditionGroup
        {
            OpenDirectAIShipGroup,
            QueueInitialFleet,
            BuildInitialFleet,
            CheckObjectivesMenu,
            StartAttackOnMiddlePlanet,
            SwitchToMiddlePlanet,
            FightOnMiddlePlanet,
//            RetreatToFirstPlanet,
//            RebuildFleet,
            TakeMiddlePlanet,
            PrepareToTakeThirdPlanet,
            ActuallyTakeThirdPlanet,
            // NukeLastPlanet,
            // Die,
            Length
        }

        public enum Condition
        {
            FirstSimStepHasHappened,
            UserHasOpenedDirectAIShipGroup,
            UserHasOpenedDocksMenu,
            UserIsOnStartPlanet,
            UserHasBuiltSpaceDock,
            UserHasQueuedAllFleetShips,
            UserHasUnpausedAIShipGroup,
            UserHasBuiltEnoughEngineers,
            UserHasBuiltAllFleetShips,
            UserHasOpenedObjectiveMenu,
            UserHasOpenedShipsMenu,
            UserHasGivenArkMovementOrder,
            UserHasSetSpaceDockToRallyToControlGroup1,
            MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips,
            MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1,
            MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder,
            MovingToMiddlePlanet_UserHasPausedTheGame,
            MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap,
            MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet,
            MovingToMiddlePlanet_UserHasUnpausedTheGame,
            MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived,
            ArkShieldsHaveDroppedBelow50Percent,
            MovingBackToFirstPlanet_UserHasSelectedArk,
            MovingBackToFirstPlanet_UserHasGivenArkMoveOrder,
            MovingBackToFirstPlanet_UserHasSwitchedViewToTargetPlanet,
            MovingBackToFirstPlanet_ArkHasArrived,
            Rebuilding_UserHasBuiltAllFleetShips,
            Rebuilding_UserHasSelectedAllMilitaryShips,
            Rebuilding_UserHasSetAllMilitaryShipsToControlGroup1,
            Rebuilding_UserHasSelectedArkAlone,
            Rebuilding_UserHasSetSpaceDockToRallyToControlGroup1,
            UserHasRidMiddlePlanetOfDefenses,
            UserHasFreedMiddlePlanetController,
            UserHasClaimedMiddlePlanet,
            UserHasBuiltEnergyCollectorOnMiddlePlanet,
            UserHasScoutedFinalPlanet,
            UserHasBuiltFrigateConstructor,
            UserHasBuiltFrigate,
            UserHasOpenedScienceMenu,
            UserHasUpgradedThings,
            UserHasEnoughEnergy,
            UserHasFreedLastPlanet,
            TutorialIsOver,
            
            // UserHasDeployedNuclearWarhead,
            // UserHasGivenNuclearWarheadMovementOrderToLastPlanet,
            // UserHasNukedLastPlanet,
            Length
        }

        private readonly List<Condition> result_GetConditionsInGroup = List<Condition>.Create_WillNeverBeGCed( 20, "Scenario_Tutorial_01-result_GetConditionsInGroup" );
        public List<Condition> GetConditionsInGroup( ConditionGroup Group )
        {
            this.result_GetConditionsInGroup.Clear();
            switch ( Group )
            {
                case ConditionGroup.OpenDirectAIShipGroup:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDirectAIShipGroup );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltEnoughEngineers );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltSpaceDock );
                    break;
                case ConditionGroup.QueueInitialFleet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDocksMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasQueuedAllFleetShips );
                    break;
                case ConditionGroup.BuildInitialFleet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasEnoughEnergy );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasUnpausedAIShipGroup );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltAllFleetShips );
                    break;
                case ConditionGroup.CheckObjectivesMenu:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedObjectiveMenu );
                    break;
                case ConditionGroup.StartAttackOnMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1 );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedDocksMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasSetSpaceDockToRallyToControlGroup1 );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder );
                    break;
                case ConditionGroup.SwitchToMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasPausedTheGame );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_UserHasUnpausedTheGame );
                    break;
                case ConditionGroup.FightOnMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedShipsMenu );
                    this.result_GetConditionsInGroup.Add( Condition.MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived );
//                    this.result_GetConditionsInGroup.Add( Condition.ArkShieldsHaveDroppedBelow50Percent );
                    break;
                // case ConditionGroup.RetreatToFirstPlanet:
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasSelectedArk );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasGivenArkMoveOrder );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_UserHasSwitchedViewToTargetPlanet );
                //     this.result_GetConditionsInGroup.Add( Condition.MovingBackToFirstPlanet_ArkHasArrived );
                //     break;
                // case ConditionGroup.RebuildFleet:
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasBuiltAllFleetShips );
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasSelectedAllMilitaryShips );
                //     this.result_GetConditionsInGroup.Add( Condition.Rebuilding_UserHasSetAllMilitaryShipsToControlGroup1 );
                //     break;
                case ConditionGroup.TakeMiddlePlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasRidMiddlePlanetOfDefenses );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasFreedMiddlePlanetController );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasClaimedMiddlePlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltEnergyCollectorOnMiddlePlanet );
                    break;
                case ConditionGroup.PrepareToTakeThirdPlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserIsOnStartPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasScoutedFinalPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltFrigateConstructor );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasBuiltFrigate );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasOpenedScienceMenu );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasUpgradedThings );
                    break;
                case ConditionGroup.ActuallyTakeThirdPlanet:
                    this.result_GetConditionsInGroup.Add( Condition.UserHasEnoughEnergy );
                    this.result_GetConditionsInGroup.Add( Condition.UserHasFreedLastPlanet );
                    this.result_GetConditionsInGroup.Add( Condition.TutorialIsOver );                    
                    break;
                    // case ConditionGroup.KillAI:
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasKilledAI );
                    //     break;
                    // case ConditionGroup.NukeLastPlanet:
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasDeployedNuclearWarhead );
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasGivenNuclearWarheadMovementOrderToLastPlanet );
                    //     this.result_GetConditionsInGroup.Add( Condition.UserHasNukedLastPlanet );
                    //     break;
                    // case ConditionGroup.Die:
                    //     break;
            }

            return this.result_GetConditionsInGroup;
        }

        public override bool WriteCurrentOngoingMessageToDisplay( ArcenDoubleCharacterBuffer Buffer )
        {
            //if ( !World.Instance.GetIsTutorialConditionFulfilled( Condition.FirstSimStepHasHappened ) )
            //    return false;

            for ( ConditionGroup group = 0; group < ConditionGroup.Length; group++ )
            {
                List<Condition> conditions = this.GetConditionsInGroup( group );
                bool allAlreadyMet = conditions.Count > 0;
                for ( int i = 0; i < conditions.Count; i++ )
                {
                    Condition condition = conditions[i];
                    if ( condition.GetAlreadyMet() )
                        continue;
                    allAlreadyMet = false;
                    break;
                }
                if ( allAlreadyMet )
                    continue;
                bool allMetNow = conditions.Count > 0;
                for ( int i = 0; i < conditions.Count; i++ )
                {
                    Condition condition = conditions[i];
                    if ( condition.GetMetNow() )
                        continue;
                    allMetNow = false;
                    break;
                }
                if(allMetNow)
                {
                    for ( int i = 0; i < conditions.Count; i++ )
                    {
                        Condition condition = conditions[i];
                        condition.MarkMet();
                    }
                    continue;
                }

                int maxHeader = 34;

                switch ( group )
                {
                    case ConditionGroup.OpenDirectAIShipGroup:
                        if ( !Condition.UserHasOpenedDirectAIShipGroup.GetMetNow() )
                        {
                            WriteHeader( Buffer, 1, maxHeader );
                            Buffer.Add( "Welcome to AI War 2!" ).Add( "\n" );
                            Buffer.Add( "This is a very basic tutorial to help players get up to speed." ).Add( "\n\n" );

                            Buffer.Add( "First let's learn how to control the camera:" ).Add( "\n\n" );

                            Buffer.Add( "--Arrow keys or WASD to move the camera up/down/left/right, or by moving the mouse to the edge of the screen" ).Add( "\n" );
                            Buffer.Add( "--Holding Q and moving the mouse to rotate the camera" ).Add( "\n" );
                            Buffer.Add( "--Mousewheel or page-up/page-down to zoom in and out" ).Add( "\n" );
                            Buffer.Add( "Camera move speed can be changed in the Settings menu, available by pressing Escape.\n" );
                            Buffer.Add( "\n" );
                            Buffer.Add( "Once you're ready to get started, select the Build Menu in the sidebar by clicking the Build tab or pressing ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab")).Add(" until it opens.")
                                  .Add("Hitting ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab"))
                                  .Add("will toggle you between the Build and Docks menus. The Build menu allows you to place structures and other critical units. The Docks menu is used to build your primary combat units." ).Add( "\n" );
                        }
                        //TODO: tell the player "Home Command Stations are important"
                        else if ( !Condition.UserHasBuiltEnoughEngineers.GetMetNow() )
                        {
                            WriteHeader( Buffer, 2, maxHeader );
                            Buffer.Add( "To assist in building things, let's start by building more Engineers. You start with two, but we want more.  Engineers are extremely useful units that can assist in building structures or ships, and they can also heal your units after a battle.\n\n")
                                .Add( "To build Engineers, find them in the Build menu (they look a bit like a gear) and click on the Engineer icon. Once you have selected the Engineer icon, you are in Building Placement mode and your mouse cursor will take the shape of the unit you are building.\n\n")
                                .Add("Left click 10 times on the map to start building 10 engineers, or hold ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("Build5xUnits"))
                                .Add( ", which builds five units at a time, and click twice.")
                                .Add(" Once you are done building them, right click to exit Building Placement mode. You should have at least 10 engineers in all.");
                        }
                        else if(!Condition.UserHasBuiltSpaceDock.GetMetNow() )
                        {
                            WriteHeader( Buffer, 3, maxHeader );
                            Buffer.Add( "Now under the Infrastructure section of the Build menu, you'll find the Space Dock toward the end of the list (mouse over the icons to find it). Click on it, then click on the planet map to start it building. " ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.QueueInitialFleet:
                        if ( !Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 4, maxHeader );
                            Buffer.Add( "To build some ships, open the Docks tab, either by clicking it or pressing ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenBuildTab")).Add(" once." );
                        }
                        else if(Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 5, maxHeader );
                            Buffer.Add( "Now we need to build some small \"fleet ships\" to prepare for combat.\n\nIn the Docks Menu, you see the ships you can build. The number under the icon is the maximum number of ships of that type that you can have at one time.").Add("\n").
                                Add("Build queues are automatically looping; it will build the selected units until you tell it to stop. \nYou can pause construction via the 'Pause' button just above the build queue but don't pause them right now; first we need to build a fleet!\n\nQueue all the available ships by clicking on the icon for each of the ship types.").Add("\n\n").
                                Add("There are 5 types of ships available to you right now. The Scout is used for exploration only, and we will use them later. The others are your initial starting combat ships.");
                        }
                        else if ( !Condition.UserHasQueuedAllFleetShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 6, maxHeader );
                            Buffer.Add( "Under \"Space Dock,\" click once on all of the ship types you can currently build. Once you've clicked a ship model then it becomes highlighted to let you know it's building." ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.BuildInitialFleet:
                        if (! Condition.UserHasEnoughEnergy.GetMetNow() )
                        {
                            WriteHeader( Buffer, 7, maxHeader );
                            Buffer.Add( "It looks like you've run out of energy before building all of your fleetships. You will need to scrap some extra units or structures in order to proceed with the tutorial. To scrap a unit or group of units, select them and hit ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ScrapUnits" )).Add("\n\n");
                        }
                        else if(! Condition.UserHasUnpausedAIShipGroup.GetMetNow() )
                        {
                            WriteHeader( Buffer, 8, maxHeader );
                            Buffer.Add( "You will need to unpause the Space Dock so it can build your fleet." ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasBuiltAllFleetShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 9, maxHeader );
                            Buffer.Add( "Your fleet is now building! This may take a while though. You can see how many ships are left by looking at the 'Docks' menu.\n\n");
                            Buffer.Add( "You can speed up or slow down time by pressing " ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseFrameSize" ) )
                                .Add( " or " ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "DecreaseFrameSize" ) ).Add(".\n\n")
                            .Add( "Now we'll wait until the maximum number of each type is built." ).Add( "\n\n" );
                        }
                        break;
                    case ConditionGroup.CheckObjectivesMenu:
                        if(!Condition.UserHasOpenedObjectiveMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 10, maxHeader );
                            Buffer.Add("Sometimes it can be hard to know what your next goal should be. To get a sense of your in-game goals, let's open the Objectives menu in the sidebar or click")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenObjectivesTab"))
                                .Add(" and mouseover the items there. That menu provides a useful guide for what you should be trying to accomplish in game.\n");
                        }
                        break;
                    case ConditionGroup.StartAttackOnMiddlePlanet:
                        if ( !Condition.MovingToMiddlePlanet_UserHasSelectedAllMilitaryShips.GetMetNow() )
                        {
                            WriteHeader( Buffer, 11, maxHeader );
                            Buffer.Add( "Once you have examined the Objectives, let's marshal your fleet and blow things up." ).Add( "\n" );
                            Buffer.Add( "Select all your military units (band-box select them all, or press ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SelectAllMobileMilitary")).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSetAllMilitaryShipsToControlGroup1.GetMetNow() )
                        {
                            WriteHeader( Buffer, 12, maxHeader );
                            Buffer.Add( "Next, add all your selected units to your first control group pressing ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ModifyControlGroup" )).Add(" + X, where X is a number. So you might use " ).Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ModifyControlGroup")).Add(" + 1 for control group 1. You can define up to 10 control groups in game, but for now let's use one control group with all of your units.").Add( "\n\n" );
                            Buffer.Add( "Once you've done that, let's re-open the Docks menu so you can have newly built ships rally directly to your fleet." ).Add( "\n" );
                        }
                        else if( !Condition.UserHasOpenedDocksMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 13, maxHeader );
                            Buffer.Add( "We'd like to not keep having to assign newly produced units to the control group manually, so let's set them to rally to that group. Make sure you are still at the start planet and select the Docks Menu (or press B once)." ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasSetSpaceDockToRallyToControlGroup1.GetMetNow() )
                        {
                            WriteHeader( Buffer, 14, maxHeader );
                            Buffer.Add("You can rally newly-built ships to a fixed location with the 'Rally' button, or to a group with the 'Group' button.  ")
                                .Add("We want to use the group button, so press that. The icon will turn green when rallying is active.").Add("\n\n")
                                .Add("This will also automatically send all newly built ships to the location of the Control Group, as well as adding them to the control group." ).Add( "\n\n" )
                                .Add( "Note that EACH dock can rally a different way.  So if you want your Frigates and fleetships to all rally together (for now, you do), then click the button on both." );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasGivenAllMilitaryShipsMoveOrder.GetMetNow() )
                        {
                            WriteHeader( Buffer, 15, maxHeader );
                            Buffer.Add("Finally, send your fleet to the next planet. There is a wormhole on the right side of the planet; you may need to pan to the right to see it. With your units selected, hold ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SendThroughWormhole"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" the wormhole or the name floating above it to tell your ships to fly through that wormhole the next planet." ).Add( "\n\n" )
                                .Add("Note!  Later on, if you are traveling far, just tab out to the galaxy map and hover over any planet.  It will show you the route your ships will take.  Then hit ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" and your ships will go straight there.").Add( "\n" );
                        }
                        break;
                    case ConditionGroup.SwitchToMiddlePlanet:
                        if ( !Condition.MovingToMiddlePlanet_UserHasPausedTheGame.GetMetNow() )
                        {
                            WriteHeader( Buffer, 16, maxHeader );
                            Buffer.Add( "But you don't want your units to get there without your being able to see them." ).Add( "\n" );
                            Buffer.Add( "Pause the game by pressing ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause"))
                                .Add(" or ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSwitchedViewToGalaxyMap.GetMetNow() )
                        {
                            WriteHeader( Buffer, 17, maxHeader );
                            Buffer.Add( "Now press ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleGalaxyMap")).Add(" to switch from Planet View to Galaxy View." ).Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasSwitchedViewToTargetPlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 18, maxHeader );
                            Buffer.Add( "This is the Galaxy View, where much of your strategizing takes place in a real game." ).Add( "\n" );
                            Buffer.Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("MakePlanetClickSelectAndSwitchView"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("SelectUnit")).Add(" on the planet in between the other two, which is where you just told your ships to go.").Add( "\n" );
                        }
                        else if ( !Condition.MovingToMiddlePlanet_UserHasUnpausedTheGame.GetMetNow() )
                        {
                            WriteHeader( Buffer, 19, maxHeader );
                            Buffer.Add("Great! Now press ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause")).Add(" (or ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add(") again to unpause the game.").Add( "\n" );
                        }
                        break;
                    case ConditionGroup.FightOnMiddlePlanet:
                        if ( !Condition.UserHasOpenedShipsMenu.GetMetNow() )
                        {
                            WriteHeader( Buffer, 20, maxHeader );
                            Buffer.Add("As we wait for the fleet to arrive, let's open the Ships sidebar menu by clicking on it or hitting ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("OpenShipsTab")).Add(". This will show all of the ships at a planet, and is the usual way to help manage a battle. You can see your units, and also select them by clicking on their icons in the Ships sidebar.").Add("\n\n")
                                .Add("In the sidebar it will also tell you the number of squads from each side and the Strength of that side; Strength is an indication of how powerful your forces are, and it is indicated by a number next to a stylized S." ).Add( "\n" );
                        }
                        if ( !Condition.MovingToMiddlePlanet_EnoughMilitaryShipsHaveArrived.GetMetNow() )
                        {
                            WriteHeader( Buffer, 21, maxHeader );
                            Buffer.Add("If your game is still paused, then hit ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePause")).Add("(or ")
                                .Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("TogglePauseAlt")).Add(" to unpause it.  We'll just wait a moment for our ships to get here and start the party." ).Add( "\n" );
                        }
                        break;
                    case ConditionGroup.TakeMiddlePlanet:
                        if ( !Condition.UserHasRidMiddlePlanetOfDefenses.GetMetNow() )
                        {
                            WriteHeader( Buffer, 22, maxHeader );
                            Buffer.Add( "You can move your selected units by right clicking on a location or target. This planet is defended by Guard Posts, which will spawn AI fleetships when you get too close. Let's move our forces toward the Guard posts and destroy them first. You will want to keep your fleet together to maximize firepower." )
                                .Add(" That said, you can also put your troops in Pursuit Mode by clicking ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleFRD")).Add(", which will have them pick their own targets.").Add( "\n\n" );
                            Buffer.Add( "It may take multiple assaults, so you may need to build a new fleet if you lose the first few. You may observe reinforcements rallying to your fleet; this is from the group-style rally you set." ).Add( "\n\n" );

                            Buffer.Add( "It is generally a good idea to destroy all the AI defensive structures and units before capturing a planet." ).Add( "\n" );
                            Buffer.Add( "This tutorial will proceed when all the AI defenses are destroyed." ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasFreedMiddlePlanetController.GetMetNow() )
                        {
                            WriteHeader( Buffer, 23, maxHeader );
                            Buffer.Add( "Great work! Now give an attack order against the enemy warp gate and \"Command Station\", if you haven't already, so that your ships destroy them." ).Add( "\n\n" );

                            Buffer.Add( "Your units will not normally attack these targets without orders, as destroying them triggers an increase in \"AI Progress\", i.e. the AI's aggressiveness in attacking you." ).Add( "\n" );
                        }
                        else if ( !Condition.UserHasClaimedMiddlePlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 24, maxHeader );
                            Buffer.Add("To capture the planet, go back to your home planet (use ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("ToggleGalaxyMap")).Add(" to go to the Galaxy menu, then transfer your view to your original planet by control-clicking on that planet) and find the \"Colony Ship\" in the build menu. Click on the Colony Ship icon, then click on the planet to choose where it builds. Once built, bring it to the middle planet. Then open the Build menu on the middle planet to build a new Command Station. There are three types of command stations; for the tutorial just pick one." ).Add( "\n\n" );

                            Buffer.Add( "This tutorial will proceed when the planet is yours." ).Add( "\n\n" ).Add("Command Stations can build slowly, so you might want to send a few Engineers over to help it build more quickly. This is one of the many values of Engineers!");
                        }
                        else if(! Condition.UserHasBuiltEnergyCollectorOnMiddlePlanet.GetMetNow())
                        {
                            Buffer.Add( "One of the key resources in the game is Energy; energy is a global resource that allows you to build ships, turrets and other critical structures. The chief means of obtaining energy is by building an Energy Collector on each of your planets. Lets build one on your new planet. You can find it in the Build menu, under Infrastructure" ).Add( "\n\n" )
                                .Add("Note that you can have Energy Collectors auto-build on your planets via the Settings menu, under Automation.") ;
                        }
                        break;
                      case ConditionGroup.PrepareToTakeThirdPlanet:
                          
                          if(!Condition.UserIsOnStartPlanet.GetMetNow() && !Condition.UserHasScoutedFinalPlanet.GetMetNow() )
                          {
                            WriteHeader( Buffer, 25, maxHeader );
                            Buffer.Add( "To finish the tutorial, we will defeat the AI on the third planet. Before attacking, let's first send some Scouts to the final planet to see what their defenses look like. Let's go back to the first planet to find some scouts.\n\n");
                          }
                          else if(!Condition.UserHasScoutedFinalPlanet.GetMetNow() )
                          {
                            WriteHeader( Buffer, 26, maxHeader );
                            Buffer.Add("Scouts are cloaked, fast but weaponless ships that are used to learn information about planets before you attack, or to monitor the enemy's activities. Scouts are fleetships and built from the Space Dock. You should have some on this planet already, so select them and then Tab to the galaxy map." ).Add( "\n\n" )
                                .Add("You can give units orders from the Galaxy map. Since you have selected some scouts, ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("MakePlanetClickSelectAndSwitchView"))
                                .Add(" - ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction("GiveOrdersToUnit")).Add(" on the final planet to send scouts. Having advanced knowledge of enemy defenses will help you choose which planets to attack, and how to attack them.\n");
                          }
                          else if(!Condition.UserHasBuiltFrigateConstructor.GetMetNow() )
                          {
                              WriteHeader( Buffer, 27, maxHeader );
                              Buffer.Add( "The enemy planet is a Mark 2 planet, which is significantly stronger than the Mark 1 planet you just took. You will need to strengthen your fleet to defeat it. First, we will build Frigates. Let's go back to your start planet and build a Frigate Dock; it is available in the Build menu near the Space Dock." ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasBuiltFrigate.GetMetNow() )
                          {
                            WriteHeader( Buffer, 28, maxHeader );
                            Buffer.Add( "Frigates are significantly stronger than fleetships, which makes them valuable tools. First, open the Docks menu and click the 'Group' button for the Frigate Dock to make any units built rally to your fleet. Then click on the Assault Frigate to begin to build it." ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasOpenedScienceMenu.GetMetNow() && !Condition.UserHasUpgradedThings.GetMetNow() )
                          {
                            WriteHeader( Buffer, 29, maxHeader );
                            Buffer.Add( "You will also need to Upgrade some fleetships. Upgrading units makes them significantly more powerful, and allows you to build more of them. Let's open the Tech Menu now and take a look." ).Add( "\n\n" );
                          }
                          else if(!Condition.UserHasUpgradedThings.GetMetNow() )
                          {
                            WriteHeader( Buffer, 30, maxHeader );
                            Buffer.Add( "When you hover over a unit it will tell you how much stronger it gets when you upgrade it. Upgrade a few fleetships (the topmost category), then we'll attack. Note that you probably don't want to upgrade the Scout here, since it's not a combat unit." ).Add( "\n\n" );
                        }
                        break;
                    case ConditionGroup.ActuallyTakeThirdPlanet:
                        if ( !Condition.UserHasEnoughEnergy.GetMetNow() )
                        {
                            WriteHeader( Buffer, 31, maxHeader );
                            Buffer.Add( "You need more energy to build more ships. Your primary way of getting energy is to build an Energy Collector on each planet -- make sure you have a collector on each one!  If you don't have enough territory to support your energy needs, you can also scrap units by selecting them and clicking " )
                                .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ScrapUnits" ) ).Add( ".\n\n" );
                        }
                        else if ( !Condition.UserHasFreedLastPlanet.GetMetNow() )
                        {
                            WriteHeader( Buffer, 33, maxHeader );
                            Buffer.Add( "Destroy the last planet to win the tutorial. You may need to attack multiple times, or upgrade more ships to do it" ).Add( "\n\n" );
                        }
                        else if ( !Condition.TutorialIsOver.GetMetNow() )
                        {
                            WriteHeader( Buffer, 34, maxHeader );
                            Buffer.Add( "Congratulations, you have won the tutorial!  This was a quick and easy taste just to get you used to the controls.  Now give one of the Quick Start options a try.\n\nRemember to pay attention to your Objectives tab!  And remember that it's okay to lose -- some of the most epic stories come out of well-fought losses.  Don't stress, and see what the galaxy throws at you." ).Add( "\n\n" );
                        }
                        break;
                   // case ConditionGroup.KillAI:
                   //     if( !Condition.UserHasKilledAI.GetMetNow())
                   //     {
                   //         Buffer.Add( "If you look at the final planet, you'll see the the AI's homeworld. Normally it's much further away, and very heavily defended." ).Add( "\n" )
                   //             .Add("But since this is a tutorial, the AI forgot to build defenses. Go and kill the AI Overlord and you win!").Add("\n");
                   //     }
                   //     else
                   //     {
                   //         Buffer.Add( "You Win! Now try your hand at a normal game using Quick Start from the main menu").Add("\n");
                   //     }
                   //     break;
                    // case ConditionGroup.NukeLastPlanet:
                    //     if ( !Condition.UserHasDeployedNuclearWarhead.GetMetNow() )
                    //     {
                    //         Buffer.Add( "If you look at the next planet down the line, you'll see lots of nasty units. That's the AI's homeworld. Normally it's much further away, and even nastier." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "The only thing you have to do to win is destroy the AI Master Controller on that planet." ).Add( "\n" );
                    //         Buffer.Add( "You don't have the conventional forces to do that in this tutorial." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "So we'll Nuke it instead." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Select your Ark, click the Warhead button, and then the Nuclear Warhead button above that." ).Add( "\n" );
                    //     }
                    //     else if ( !Condition.UserHasGivenNuclearWarheadMovementOrderToLastPlanet.GetMetNow() )
                    //     {
                    //         Buffer.Add( "Congratulations, you have just exposed an galaxy-threatening fully-annihilating-fusion-reaction bomb to danger. It's also your only one." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Better use it quick." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Select the Nuke and tell it to move to the AI homeworld." ).Add( "\n" );
                    //         Buffer.Add( "" ).Add( "\n" );
                    //         Buffer.Add( "Normally an AI homeworld has devices which counter the effects of nuclear warheads, and it's a big pain to destroy them, but for this tutorial we've somehow neglected to include those." ).Add( "\n" );
                    //     }
                    //     else if ( !Condition.UserHasNukedLastPlanet.GetMetNow() )
                    //     {
                    //         Buffer.Add( "Now we wait for the bang (well, no sound effect just yet, but it will make a lot of stuff just go away)." ).Add( "\n" );
                    //     }
                    //     break;
                    // case ConditionGroup.Die:
                    //     Buffer.Add( "Well done! You've made the AI very angry, and it's now going to find you and facilitate communications." ).Add( "\n" );
                    //     Buffer.Add( "" ).Add( "\n" );
                    //     Buffer.Add( "We can't really help you now, but it will help you learn the very most important lesson in AI War:" ).Add( "\n" );
                    //     Buffer.Add( "" ).Add( "\n" );
                    //     Buffer.Add( "Do not make the AI mad before you can make it dead." ).Add( "\n" );
                    //     break;
                }
                break;
            }

            Buffer.Add( "</size>" );
            return true;
        }

        public void WriteHeader( ArcenDoubleCharacterBuffer Buffer, int Index, int MaxIndex )
        {
            Buffer.Add( "<b><color=#78beff>T</color><color=#6db9ff>u</color><color=#5eb1ff>t</color><color=#5ec4ff>o</color><color=#52c0ff>r</color><color=#52d8ff>i</color><color=#42d5ff>a</color><color=#24deff>l</color><color=#78beff> Step " )
                    .Add( Index ).Add( "/" ).Add( MaxIndex ).Add( "</b>:</color>\n" );

            if ( World.Instance.IsPaused )
                Buffer.Add( "<color=#ffd75e>Game Is Paused!</color>\n" ).Add(FontSizes.BASE_SIZE_STRING);
        }
        public bool GetIsConditionMet(Condition Condition)
        {
            return true; //this is the old style tutorial, not the new kind that is actually used
        }
        private static bool GetAreAllFactionMobileMilitaryMovingToPlanet( Faction localFaction, Planet targetPlanet )
        {
            bool foundAll = true;
            foreach ( GameEntity_Squad entity in localFaction.Squads( EntityRollupType.MobileCombatants ) )
            {
                if ( GetIsShipMovingToPlanet( targetPlanet, entity ) )
                    continue;
                foundAll = false;
                break;
            }
            return foundAll;
        }

        private static bool GetIsShipMovingToPlanet( Planet targetPlanet, GameEntity_Squad entity )
        {
            EntityOrder entityOrder = entity.ForShortTermPlanning_CurrentValidOrder;
            if ( entityOrder.TypeData == null ) 
                return false;
            if ( entityOrder.TypeData.Type != EntityOrderType.Wormhole )
                return false;
            if ( entityOrder.RelatedPlanetIndex != targetPlanet.Index )
                return false;
            return true;
        }

        private static bool GetAreEnoughFactionMobileMilitaryOnPlanet( Faction localFaction, Planet targetPlanet )
        {
            int numOnPlanet = 0;
            int total = 0;
            foreach ( GameEntity_Squad entity in localFaction.Squads( EntityRollupType.MobileCombatants ) )
            {
                total++;
                if ( entity.Planet == targetPlanet )
                    numOnPlanet++;
            }
            if( numOnPlanet * 1.5 > total)
                return true;
            return false;
        }

        public override void DoOnFirstUnpauseLogic_SinceLoadOrGeneration_HostOrClient( ArcenClientOrHostSimContextCore Context )
        {
        }
        public override void DoOnFirstUnpauseLogic_FromGameSetup_HostOnly( bool IsFromMapGen )
        {
            

        }
    }

    public static class Tutorial01_Condition_Extensions
    {
        public static bool GetAlreadyMet(this Scenario_Tutorial_01.Condition Condition)
        {
            return false;// World.Instance.GetIsTutorialConditionFulfilled( Condition );
        }

        public static bool GetMetNow(this Scenario_Tutorial_01.Condition Condition )
        {
            return Scenario_Tutorial_01.Instance.GetIsConditionMet( Condition );
        }

        public static void MarkMet( this Scenario_Tutorial_01.Condition Condition )
        {
            //World.Instance.SetTutorialConditionFulfilled( Condition, true );
        }
    }
}
