using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class ZenithTraderFactionDeepInfo : LoneWandererFactionDeepInfo
    {
        public ZenithTraderFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZenithTraderFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        protected override bool LongRangePlanning_GetMayPassThroughThisPlanet(Faction faction, Planet planet) => true;
        private const bool RespawnAllowed = true;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            this.BaseInfo.SetStartingFactionRelationships(); //I've seen some issues where the zenith trader was being attacked by things, so just refresh this
            if(World_AIW2.Instance.GameSecond == 2 && this.BaseInfo.SpawnAtPlayer)
            {
                Planet humanKingPlanet = FactionUtilityMethods.Instance.findHumanKing( false );
                if ( humanKingPlanet != null )
                {
                    PlanetFaction pFaction = humanKingPlanet.GetPlanetFactionForFaction( AttachedFaction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithTrader" );
                    /*GameEntity_Squad entity = */
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                            pFaction.FleetUsedAtPlanet, 0, Engine_AIW2.Instance.CombatCenter, Context, "ZenithTrader-ReplaceTrader" );
                }
            }

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if(!otherFaction.IsAllowedToBuyFromZenithTrader_Safe())
                    continue;
                FInt budgetMultiplier = FInt.One;
                if(otherFaction.Type == FactionType.AI)
                {
                    AISentinelsCoreData factionExternal = otherFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if(factionExternal.AIDifficulty.Difficulty < 4)
                        budgetMultiplier = this.BaseInfo.AIBudgetMultiplierLow;
                    else if(factionExternal.AIDifficulty.Difficulty < 7)
                        budgetMultiplier = this.BaseInfo.AIBudgetMultiplierMedium;
                    else
                        budgetMultiplier = this.BaseInfo.AIBudgetMultiplierHigh;
                }
                otherFaction.ZenithTraderBudget += (this.BaseInfo.budgetPerSecondForOtherFactions * budgetMultiplier).IntValue;
            }

            if( this.BaseInfo.LoneWanderers.Count == 0)
            {
                if (RespawnAllowed && this.BaseInfo.TimeLastExisted + this.BaseInfo.RespawnTime > World_AIW2.Instance.GameSecond)
                {
                    Planet respawnPlanet = GetRespawnPlanet(AttachedFaction, Context);
                    if(respawnPlanet == null)
                        return; //no suitable planet to respawn on; this is unlikely (perhaps the humans have conquered everything but the AI homeworlds?

                    //If the player didn't see the zenith trader die and doesn't have knowledge of its spawn point, don't play any messages
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        if ( respawnPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = respawnPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Zenith Trader</color> respawning on " +
                                respawnPlanet.Name, ChatType.LogToCentralChat, "ArkChiefOfStaff_ZenithTraderRespawns", chatHandlerOrNull );
                        }
                        else if ( this.BaseInfo.PlayerSawUnitDie )
                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Zenith Trader</color> respawning somewhere in the galaxy",
                                ChatType.LogToCentralChat, "ArkChiefOfStaff_ZenithTraderRespawns", null );
                    }

                    PlanetFaction pFaction = respawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithTrader" );
                    /*GameEntity entity = */GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                    pFaction.FleetUsedAtPlanet, 0, Engine_AIW2.Instance.CombatCenter, Context, "ZenithTrader-ReplaceTrader" );
                }
            }
        }
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
            if ( entity == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( entity.TypeData.GetHasTag( "ZenithTrader" ) )
                {
                    debugStage = 200;
                    if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        debugStage = 300;
                        this.BaseInfo.PlayerSawUnitDie = true;

                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = entity.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( entity.StartFactionColourForLog_Safe() + "Zenith Trader</color> destroyed on " +
                                entity.GetPlanetName_Safe(), ChatType.LogToCentralChat, "ArkChiefOfStaff_ZenithTraderDestroyed", chatHandlerOrNull );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in ZenithTraderFactionDeepInfo.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
