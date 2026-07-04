using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Methods for things 
    /// </summary>
    public abstract class HumanFactionSharedDeep
    {
        public static void CheckAndHandleStationDeath( Faction humanFaction, ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            //TODO: this is mostly copied from the GeneralHumanFactionDeepInfo, so that should be refactored
            debugStage = 0;
            try
            {
                debugStage = 1000;
                //            ArcenDebugging.ArcenDebugLogSingleLine("Human Entity destroyed: " + entity.TypeData.InternalName, Verbosity.DoNotShow );
                if ( entity.TypeData.SpecialType == SpecialEntityType.NormalHumanCommandStation )
                {
                    debugStage = 1100;

                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = entity.Planet;

                    if ( FiringSystemOrNull != null && FiringSystemOrNull.ParentEntity.GetFactionTypeSafe() == FactionType.AI )
                    {
                        debugStage = 1200;
                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.GetPlanetName_Safe() + " command station destroyed!", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        if ( Context.RandomToUse.Next( 0, 10 ) < 8 )
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerControllerDestroyed );
                        else
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlanetLost );  //just for variety

                    }
                    else
                    {
                        debugStage = 1300;
                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.GetPlanetName_Safe() + " command station destroyed!", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlanetLost );
                    }

                }

                debugStage = 2000;
                if ( entity.TypeData.IsCommandStation // thing dying is a command station
                    && FiringSystemOrNull != null ) // the game knows who killed it
                {
                    debugStage = 2100;
                    bool dysonEffectPlayed = false;
                    //First check if killing this controller freed the Dyson Sphere; if so then that message takes priority
                    if ( entity.Planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true ) != null )
                    {
                        dysonEffectPlayed = true;

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = entity.Planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.GetPlanetName_Safe() + " command station destroyed!", ChatType.LogToCentralChat,
                            "ArkChiefOfStaff_DysonLiberatedFromPlayer", chatHandlerOrNull );
                    }
                    debugStage = 2400;
                    if ( !dysonEffectPlayed )
                    {
                        //                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.PlayerControllerDestroyed );
                    }

                    debugStage = 3000;
                    // Handle the Shark Plots
                    // ...
                    // if the killer was an ai / part of the ai
                    // if it wasn't still under construction
                    // if it was a HUMAN command station and owned by a player
                    if ( (factionThatKilledEntity.Type == FactionType.AI || factionThatKilledEntity.SpecialFactionData.AlliedToAIByDefault) &&
                         entity.SelfBuildingMetalRemaining <= 0 &&
                         entity.TypeData.SpecialType == SpecialEntityType.NormalHumanCommandStation &&
                         entityOwningFaction != null && entityOwningFaction.Type == FactionType.Player)
                    {
                        debugStage = 3100;
                        
                        bool sharkA = World_AIW2.Instance.Setup.GetBoolBySetting( "SharkA" );
                        if ( sharkA )
                        {
                            GlobalAIWorldBaseInfo.Instance.ChangeAIP( ExternalConstants.Instance.SharkAAIPBoost, AIPChangeReason.EntityDeath, entity.TypeData, factionThatKilledEntity.FactionIndex, entity.Planet.Index, -1 );
                        }
                        debugStage = 3200;
                        //Shark B (exo on command station death) is always enabled
                        GlobalGeneralDeepInfoCommandHandler.TriggerSharkB( entity.PlanetFaction.Faction, factionThatKilledEntity, entity, Context );
                    }
                }
                
                debugStage = 4000;
                if ( entity.TypeData.GetHasTag( "StationaryConstructor" ) && 
                     FiringSystemOrNull != null && 
                     entity.Planet != null )
                {
                    debugStage = 4100;
                    Planet planet = entity.Planet;
                    
                    debugStage = 4200;
                    if ( planet != null )
                    {
                        debugStage = 4300;
                        if ( planet.TimeLastPlayedConstructorDeathAudioCue < 0 || planet.TimeLastPlayedConstructorDeathAudioCue < World_AIW2.Instance.GameSecond - 300 ) //if has not played for 300 seconds
                        {
                            debugStage = 4400;
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerStationaryConstructorDestroyed );
                            planet.TimeLastPlayedConstructorDeathAudioCue = World_AIW2.Instance.GameSecond;
                        }
                    }
                }

                debugStage = 5000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in HumanSpireInfusedEmpireFactionDeepInfo.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
            return;
        }
    }
}