using Arcen.AIW2.Core;
using System;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Hacking_Sabotage : HackingImplementation_WithTargetMenu
    {
        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            var log = Trace.Hack;
            log?.Msg("Hacking_Sabotage.DoCompletion_Extra(); Target={0}, Planet={1}, Hacker={2}", Target.OrNull(), planet.OrNull(), Hacker.OrNull());
            
            if ( Target != null )
            {
                if ( Target.TypeData.GetHasTag( "WarpGate" ) )
                {
                    foreach ( PlanetFaction planetFaction in Target.Planet.Factions )
                        planetFaction.AIPLeftFromWarpGate = 0;
                }

                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = Target.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "人类黑客刚刚在 " + Target.GetPlanetName_Safe() + " 摧毁了一个 " + Target.TypeData.GetDisplayName(), ChatType.LogToCentralChat,
                        chatHandlerOrNull );
                }

                if ( AIWar2GalaxySettingQuickAccess.SabotageHacksCauseAIP )
                {
                    if ( Target.TypeData.AIPOnDeath > 0 )
                    {
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                            (FInt) Target.TypeData.AIPOnDeath, AIPChangeReason.EntityDeath, Target.TypeData, Hacker.GetFactionIndex_Safe(), Target.Planet.Index,
                            Target.GetFactionIndex_Safe() );
                    }
                }

                Target.Despawn( Context, false, InstancedRendererDeactivationReason.IWasHackedToDeath );
            }
            else
            {
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = Target.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "人类黑客未能在 " + Target.GetPlanetName_Safe() + " 摧毁任何东西", ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }

            return true;
        }

        public override string GetDynamicDescription(
            GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "Hacking_Sabotage.description" );
            if ( AIWar2GalaxySettingQuickAccess.SabotageHacksCauseAIP )
                buffer.Add( "这并不能规避由目标被摧毁引起的 AIP 增加。\n" );
            else
                buffer.Add( "如果该建筑在正常情况下死亡时会增加 AI 进度，通过这种方式摧毁它不会造成此增加。\n" );

            buffer.Add( base.GetDynamicDescription( target, hackerOrNull, planet, hackerFaction, hackingType ) );

            return buffer.ToStringAndReturnToPool();
        }
    }

    public class Hacking_Reprogram : HackingImplementation_WithTargetMenu
    {
        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( Target != null )
            {
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( Target );

                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "人类黑客刚刚在 " + Target.GetPlanetName_Safe() + " 重新编程了一个 " + Target.TypeData.GetDisplayName(),
                        ChatType.LogToCentralChat, chatHandlerOrNull );
                }

                EndpointFunctions.TransferEntityToFaction( Target, Hacker.PlanetFaction.Faction, "重新编程黑客！" );
            }
            else
            {
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = Target.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "人类黑客未能在 " + Target.GetPlanetName_Safe() + " 重新编程任何东西", ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }

            return true;
        }
    }
}