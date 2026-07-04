
using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class ChromaticHorrorFactionDeepInfo : LoneWandererFactionDeepInfo
    {
        public ChromaticHorrorFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ChromaticHorrorFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        protected override bool LongRangePlanning_GetMayPassThroughThisPlanet(Faction faction, Planet planet)
        {
            if ( planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).HasKingUnitPresent )
                return false;
            return true;
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            UpdateAllegiance( AttachedFaction );

            if ( this.BaseInfo.LoneWanderers.Count <= 0 && this.BaseInfo.TimeLastExisted + this.BaseInfo.RespawnTime < World_AIW2.Instance.GameSecond )
            {
                Planet respawnPlanet = GetRespawnPlanet( AttachedFaction, Context );
                if ( respawnPlanet == null )
                    return; //no suitable planet to respawn on; this is unlikely (perhaps the humans have conquered everything but the AI homeworlds?
                            //If the player didn't see the zenith trader die and doesn't have knowledge of its spawn point, don't play any messages
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    if ( respawnPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = respawnPlanet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Chromatic Horror</color> already reappeared on " + respawnPlanet.Name, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                    else if ( this.BaseInfo.PlayerSawUnitDie )
                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Chromatic Horror</color> already reappeared somewhere in the galaxy", ChatType.LogToCentralChat, string.Empty, null );
                }

                PlanetFaction pFaction = respawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ChromaticHorror" );
                /*GameEntity entity = */
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, Engine_AIW2.Instance.CombatCenter, Context, "ChromaticHorrorRespawn" );

            }
        }

        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( !IsFromBeacon && SquadSeenOrNull != null )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( SquadSeenOrNull );

                World_AIW2.Instance.QueueChatMessageOrCommand( "Commander, we've sighted a " + SquadSeenOrNull.StartFactionColourForLog_Safe() +
                    "Chromatic Horror</color> on " + SquadSeenOrNull.GetPlanetName_Safe(), ChatType.LogToCentralChat, String.Empty, chatHandlerOrNull );
            }
        }
        private void UpdateAllegiance(Faction faction)
        {
            //hates everyone all the time
            AllegianceHelper.EnemyThisFactionToAll( faction );
        }
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;

            int debugStage = 0;
            try
            {
                try
                {
                    if ( entity.TypeData.GetHasTag( "ChromaticHorror" ) )
                    {
                        if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            this.BaseInfo.PlayerSawUnitDie = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = entity.Planet;

                                World_AIW2.Instance.QueueChatMessageOrCommand( entity.StartFactionColourForLog_Safe() + "Chromatic Horror</color> destroyed on " + entity.GetPlanetName_Safe(),
                                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                            }
                        }
                    }
                }
                catch { } //we don't actually care if this errors, because it's just cosmetic.  And things do die.
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_ChromaticHorror.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
