using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireRelicMoveHandler : IAlternativeMoveOrderHandler
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //nothing to do here
        }

        /// <summary>
        /// Critical to note that this is NOT in sim code -- this is in UI code!
        /// </summary>
        public void MoveToPlanetOrLocation( GameEntity_Squad RelatedEntity, Planet TargetPlanet, ArcenPoint Point, bool CareAboutSpecificPoint )
        {
            if ( RelatedEntity == null || TargetPlanet == null )
                return;
            SpireCityBlockingReason reasonCode = SpireCityBlockingReason.None;
            reasonCode = FallenSpireFactionBaseInfo.Instance.IsPlanetAllowedToBuildCity( TargetPlanet, RelatedEntity );
            if ( reasonCode == SpireCityBlockingReason.None )
            {
                //yay let's build!
                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SendSpireRelicToLocation], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedEntityIDs.Add( RelatedEntity.PrimaryKeyID );
                    command.RelatedIntegers.Add( TargetPlanet.Index );
                    if ( CareAboutSpecificPoint )
                        command.RelatedPoints.Add( Point );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }, null, "Build Spire City On " + TargetPlanet.Name + "?", "Are you sure you want to send this spire relic to the planet " + TargetPlanet.Name +
                    " to build a spire city there?  Once it gets to the selected location on that planet (or the center of the planet if you did not click a specific point), it will turn into a city that you cannot undo or later move.", "Yes, Go Now", "No, Wait!" );
            }
            else
            {
                switch ( reasonCode )
                {
                    case SpireCityBlockingReason.TooNearOtherCity:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: there is another spire city within " + FallenSpireFactionBaseInfo.MinHopsBetweenCities +
                                " hops of that planet, which is too close.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIHomeworld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: that city is (or was) an AI Homeworld, which is not a valid place to construct spire cities.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIBastionWorld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: that city is (or was) an AI Bastion World, which is not a valid place to construct spire cities.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanet:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send this relic to any planet other than the one it started on.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send this relic to any planet other than the one it started on, BUT this planet already has a spire city on it, so this relic can't be used at all.  Better scrap it.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    default:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: " + reasonCode,
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                }
            }
        }
    }
    public class SpireSidekickRelicMoveHandler : IAlternativeMoveOrderHandler
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //nothing to do here
        }

        /// <summary>
        /// Critical to note that this is NOT in sim code -- this is in UI code!
        /// </summary>
        public void MoveToPlanetOrLocation( GameEntity_Squad RelatedEntity, Planet TargetPlanet, ArcenPoint Point, bool CareAboutSpecificPoint )
        {
            if ( RelatedEntity == null || TargetPlanet == null )
                return;
            SpireCityBlockingReason reasonCode = SpireCityBlockingReason.None;
            reasonCode = SpireSidekickFactionBaseInfo.Instance.IsPlanetAllowedToBuildCity( TargetPlanet, RelatedEntity );
            if ( reasonCode == SpireCityBlockingReason.None )
            {
                //yay let's build!
                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SendSpireSidekickRelicToLocation], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedEntityIDs.Add( RelatedEntity.PrimaryKeyID );
                    command.RelatedIntegers.Add( TargetPlanet.Index );
                    if ( CareAboutSpecificPoint )
                        command.RelatedPoints.Add( Point );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }, null, "Build Spire City On " + TargetPlanet.Name + "?", "Are you sure you want to send this spire relic to the planet " + TargetPlanet.Name +
                    " to build a spire city there?  Once it gets to the selected location on that planet (or the center of the planet if you did not click a specific point), it will turn into a city that you cannot undo or later move.", "Yes, Go Now", "No, Wait!" );
            }
            else
            {
                switch ( reasonCode )
                {
                    case SpireCityBlockingReason.TooNearOtherCity:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: there is another spire city within " + SpireSidekickFactionBaseInfo.MinHopsBetweenCities +
                                " hops of that planet, which is too close.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIHomeworld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: that city is (or was) an AI Homeworld, which is not a valid place to construct spire cities.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIBastionWorld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: that city is (or was) an AI Bastion World, which is not a valid place to construct spire cities.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanet:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send this relic to any planet other than the one it started on.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send this relic to any planet other than the one it started on, BUT this planet already has a spire city on it, so this relic can't be used at all.  Better scrap it.",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    default:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot send relic to " + TargetPlanet.Name +
                                " to build a city: " + reasonCode,
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                }
            }
        }
    }
}
