using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_SendSpireRelicToLocation : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_SendSpireRelicToLocation: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );

            if ( command.RelatedIntegers.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_SendSpireRelicToLocation: No RelatedIntegers!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            Planet targetPlanet = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( (Int16)command.RelatedIntegers.First );

            ArcenPoint targetPoint = Engine_AIW2.Instance.CombatCenter;
            if ( command.RelatedPoints.Count > 0 )
                targetPoint = command.RelatedPoints.First;
            FallenSpireFactionBaseInfo.SetRelicDestination( squad, targetPlanet, targetPoint );
        }
    }
    public class GameCommand_UpgradeSpireCity : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_UpgradeSpireCity: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad city = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );

            int nonSpirePlanetsOwnedByPlayers = FallenSpireFactionBaseInfo.GetNonSpirePlanetsOwnedByPlayers();
            if ( !FallenSpireFactionBaseInfo.CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, city ) )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "目前无法升级城市 " + city.GetFleetName_Safe() +
                            "。", ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                }
                return;
            }

            Fleet fleet = city.GetFleetOrNull_Safe();
            if ( fleet == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "目前无法升级城市 " + city.GetFleetName_Safe() +
                            "，因为找不到其舰队。请重试，否则这是一个错误。", ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                }
                return;
            }

            //yay, level up!
            fleet.AddedMarkLevelsForFleet_FromScience++;
            //go ahead and mark the city center higher right away, for other city purposes
            city.SetCurrentMarkLevel( (byte)(city.CurrentMarkLevel + 1) );

            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                World_AIW2.Instance.QueueChatMessageOrCommand( "尖塔城市 " + city.GetFleetName_Safe() + " 在星球 " + city.GetPlanetName_Safe() +
                    " 上已提升至等级 " + city.CurrentMarkLevel + "！", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
            }
        }
    }

    //For Spire Sidekick
    public class GameCommand_SendSpireSidekickRelicToLocation : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_SendSpireSidekickRelicToLocation: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( squad != null && !squad.TypeData.GetHasTag("SpireSidekickRelic"))
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_SendSpireSidekickRelicToLocation: attempted to use this command for a " + squad.ToString() + ". This will not work.", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_SendSpireSidekickRelicToLocation: No RelatedIntegers!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            Planet targetPlanet = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( (Int16)command.RelatedIntegers.First );

            ArcenPoint targetPoint = Engine_AIW2.Instance.CombatCenter;
            if ( command.RelatedPoints.Count > 0 )
                targetPoint = command.RelatedPoints.First;
            //ArcenDebugging.LogSingleLine("Setting relic destination for " + squad.ToStringWithPlanet(), Verbosity.DoNotShow );
            SpireSidekickFactionBaseInfo.SetRelicDestination( squad, targetPlanet, targetPoint );
        }
    }
    public class GameCommand_UpgradeSpireSidekickCity : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "GameCommand_UpgradeSpireCity: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad city = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );

            int nonSpirePlanetsOwnedByPlayers = SpireSidekickFactionBaseInfo.GetNonSpirePlanetsOwnedByPlayers();
            if ( !SpireSidekickFactionBaseInfo.CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, city ) )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "目前无法升级城市 " + city.GetFleetName_Safe() +
                            "。", ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                }
                return;
            }

            Fleet fleet = city.GetFleetOrNull_Safe();
            if ( fleet == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "目前无法升级城市 " + city.GetFleetName_Safe() +
                            "，因为找不到其舰队。请重试，否则这是一个错误。", ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                }
                return;
            }

            //yay, level up!
            fleet.AddedMarkLevelsForFleet_FromScience++;
            //go ahead and mark the city center higher right away, for other city purposes
            city.SetCurrentMarkLevel( (byte)(city.CurrentMarkLevel + 1) );

            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( city );

                World_AIW2.Instance.QueueChatMessageOrCommand( "尖塔城市 " + city.GetFleetName_Safe() + " 在星球 " + city.GetPlanetName_Safe() +
                    " 上已提升至等级 " + city.CurrentMarkLevel + "！", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
            }
        }
    }
}
