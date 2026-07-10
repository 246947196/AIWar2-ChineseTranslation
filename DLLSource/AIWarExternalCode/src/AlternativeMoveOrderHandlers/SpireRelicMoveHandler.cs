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
                }, null, "在" + TargetPlanet.Name + "上建造尖塔城市？", "确定要把这座尖塔圣物送往星球" + TargetPlanet.Name +
                    "并在那里建造尖塔城市吗？一旦圣物到达该星球上的指定位置（如果没有点击特定位置则为星球中心），它将变成一座城市，此操作无法撤销或移动。", "是，立即前往", "不，等等！" );
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

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球周围" + FallenSpireFactionBaseInfo.MinHopsBetweenCities +
                                "跳内有另一座尖塔城市，距离过近。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIHomeworld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球是（或曾经是）AI 母星，不能建造尖塔城市。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIBastionWorld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球是（或曾经是）AI 堡垒世界，不能建造尖塔城市。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanet:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "此圣物只能送往起始星球。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "此圣物只能送往起始星球，但该星球已有尖塔城市，此圣物已无法使用。建议拆解。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    default:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：" + reasonCode,
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
                }, null, "在" + TargetPlanet.Name + "上建造尖塔城市？", "确定要把这座尖塔圣物送往星球" + TargetPlanet.Name +
                    "并在那里建造尖塔城市吗？一旦圣物到达该星球上的指定位置（如果没有点击特定位置则为星球中心），它将变成一座城市，此操作无法撤销或移动。", "是，立即前往", "不，等等！" );
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

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球周围" + SpireSidekickFactionBaseInfo.MinHopsBetweenCities +
                                "跳内有另一座尖塔城市，距离过近。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIHomeworld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球是（或曾经是）AI 母星，不能建造尖塔城市。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.IsAIBastionWorld:
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = TargetPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：该星球是（或曾经是）AI 堡垒世界，不能建造尖塔城市。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanet:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "此圣物只能送往起始星球。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    case SpireCityBlockingReason.RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "此圣物只能送往起始星球，但该星球已有尖塔城市，此圣物已无法使用。建议拆解。",
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                    default:
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( RelatedEntity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法将圣物送往" + TargetPlanet.Name +
                                "建造城市：" + reasonCode,
                                ChatType.LogToCentralChat, "CannotDoThatThing", chatHandlerOrNull );
                        }
                        break;
                }
            }
        }
    }
}
