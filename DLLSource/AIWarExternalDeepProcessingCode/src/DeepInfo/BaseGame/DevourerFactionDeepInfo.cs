
using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class DevourerFactionDeepInfo : LoneWandererFactionDeepInfo
    {
        public DevourerFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DevourerFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //this may kinda-sorta matter if you restarted just the wrong way.
            Allegiance = string.Empty;
            aiAllied = false;
            humanAllied = false;
        }

        private string Allegiance;
        public bool aiAllied = false;
        public bool humanAllied = false;
        //TheThingamabob on discord suggested having the Devourer drawn to
        //combat once the game is a good ways in. An interesting idea, but for later

        protected override bool LongRangePlanning_GetMayPassThroughThisPlanet(Faction faction, Planet planet)
        {
            if ( planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).HasKingUnitPresent )
                return false;
            return true;
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            Allegiance = AttachedFaction.BaseInfo.Allegiance;
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

                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Devourer</color> respawning on " + respawnPlanet.Name, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                    else if ( this.BaseInfo.PlayerSawUnitDie )
                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Devourer</color> respawning somewhere in the galaxy", ChatType.LogToCentralChat, string.Empty, null );
                }

                PlanetFaction pFaction = respawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "Devourer" );
                /*GameEntity entity = */
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, Engine_AIW2.Instance.CombatCenter, Context, "DevourerWasMissing" );

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

                World_AIW2.Instance.QueueChatMessageOrCommand( "指挥官，我们与一颗巨大的" + SquadSeenOrNull.StartFactionColourForLog_Safe() +
                    "吞噬魔像</color>在" + SquadSeenOrNull.GetPlanetName_Safe() + "上首次接触。", ChatType.LogToCentralChat, "ArkChiefOfStaff_PlayerGainsDevourerIntel", chatHandlerOrNull );
            }
        }
        private void UpdateAllegiance(Faction faction)
        {
            bool localDebug = false;
            if(ArcenStrings.Equals(this.Allegiance, "对所有敌对") || ArcenStrings.Equals(this.Allegiance, "HostileToAll") ||
               string.IsNullOrEmpty(this.Allegiance ))
            {
                this.humanAllied = false;
                this.aiAllied = false;
                if(string.IsNullOrEmpty(this.Allegiance ))
                    throw new Exception("empty Devourer allegiance '" + this.Allegiance +"'");
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction should be hostile to all (default)", Verbosity.DoNotShow );
                //make sure this isn't set wrong somehow
                AllegianceHelper.EnemyThisFactionToAll( faction );
            }
            else if(ArcenStrings.Equals(this.Allegiance, "仅对玩家敌对") ||
                    ArcenStrings.Equals(this.Allegiance, "HostileToPlayers"))
            {
                this.aiAllied = true;
                AllegianceHelper.AllyThisFactionToAI(faction);
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow );
            }
            else if(ArcenStrings.Equals(this.Allegiance, "小派系小队红") )
            {
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "小派系小队红");
            }
            else if(ArcenStrings.Equals(this.Allegiance, "小派系小队蓝") )
            {
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "小派系小队蓝");
            }
            else if(ArcenStrings.Equals(this.Allegiance, "小派系小队绿") )
            {
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam(faction, "小派系小队绿");
            }

            else if(ArcenStrings.Equals(this.Allegiance, "HostileToAI") ||
                    ArcenStrings.Equals(this.Allegiance, "对玩家友好"))
            {
                this.humanAllied = true;
                AllegianceHelper.AllyThisFactionToHumans(faction);
                if(localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("This Devourer faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow );
            }
            else
            {
                throw new Exception("unknown Devourer allegiance '" + this.Allegiance +"'");
            }
            //The Devourer is always hostile to the zenith trader
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue; 
                if ( otherFaction.SpecialFactionData != null && otherFaction.SpecialFactionData.InternalName == "ZenithTrader" )
                {
                    faction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( faction );
                }
            }
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
                    if ( entity.TypeData.GetHasTag( "Devourer" ) )
                    {
                        if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            this.BaseInfo.PlayerSawUnitDie = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = entity.Planet;

                                World_AIW2.Instance.QueueChatMessageOrCommand( entity.StartFactionColourForLog_Safe() + "吞噬魔像</color>在" + entity.GetPlanetName_Safe() + "被摧毁。",
                                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                            }
                        }
                    }
                }
                catch { } //we don't actually care if this errors, because it's just cosmetic.  And things do die.
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_Devourer.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
