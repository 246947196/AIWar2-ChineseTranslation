using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithTraderFactionBaseInfo : LoneWandererFactionBaseInfo
    {
        //not serialized
        private readonly Dictionary<SafeSquadWrapper, IndividualTraderData> individualTraderData = 
            Dictionary<SafeSquadWrapper, IndividualTraderData>.Create_WillNeverBeGCed( 10, "ZenithTraderFactionBaseInfo-individualTraderData" );

        //constants
        public override int RespawnTime => 600;
        public override string LoneUnitTag => "ZenithTrader";

        public ZenithTraderFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
            individualTraderData.Clear();

            HaveLoadedData = false; //cause a reload of xml data
        }

        #region Xml Data
        //central constants, do not need to be cleared or reset
        bool HaveLoadedData = false;
        public int budgetPerSecondForOtherFactions = 0;
        public int StructureCost = 0;
        public FInt AIBudgetMultiplierLow;
        public FInt AIBudgetMultiplierMedium;
        public FInt AIBudgetMultiplierHigh;
        public bool SpawnAtPlayer; // for debug

        private void LoadCustomDataIfNeeded()
        {
            if ( HaveLoadedData )
                return;
            HaveLoadedData = true;
            budgetPerSecondForOtherFactions = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_zenithtrader_budgetpersecondforotherfactions" );
            AIBudgetMultiplierLow = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_zenithtrader_aibudgetmultiplierlow" );
            AIBudgetMultiplierMedium = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_zenithtrader_aibudgetmultipliermedium" );
            AIBudgetMultiplierHigh = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_zenithtrader_aibudgetmultiplierhigh" );
            SpawnAtPlayer = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_zenithtrader_spawnonplayerhomeplanet" ); //for debug
            StructureCost = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_zenithtrader_structurecost" );
            if ( budgetPerSecondForOtherFactions <= 0 )
                throw new Exception( "Failed to parse xml for zenith trader" );
        }
        #endregion

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant for zenith trader
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "5 来自天顶商人的负载" );
            return 5;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();
        }

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;
                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                    case FactionType.AI:
                        faction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( faction );
                        break;
                    case FactionType.SpecialFaction:
                        if ( otherFaction.SpecialFactionData != null && otherFaction.SpecialFactionData.InternalName == "DevourerGolem" ||
                                                                        otherFaction.SpecialFactionData.InternalName == "ZenithMiners" )
                        {
                            faction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( faction );
                        }
                        else
                        {
                            faction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( faction );
                        }
                        break;
                }
            }
        }
        #endregion

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( this.LoneWanderers.Count <= 0 ) //if it has died, clear the trade rememberance data
                individualTraderData.Clear();
        }

        //This is what we set the Devourer to every frame
        protected override void GivePerStanceStanceOrdersToFoundWanderers( GameEntity_Squad entity, ArcenClientOrHostSimContextCore Context )
        {
            if ( !this.individualTraderData.ContainsKey( entity ) )
                this.individualTraderData.Set( entity, new IndividualTraderData() );

            this.individualTraderData.Get( entity ).DoPerSecondLogic( entity, false, StructureCost, Context.GetHostOnlyContext() );
        }

        public class IndividualTraderData
        {
            //track the last few planets the Zenith Trader has been on.
            //if it is just coming back to human space (or if you have just reloaded),
            //play a more elaborate "Zenith Trader has arrived" message. Otherwise just put a message in the message log
            public readonly List<Planet> previousPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithTraderFactionBaseInfo-IndividualTraderData-previousPlanets" ); 

            public void DoPerSecondLogic( GameEntity_Squad traderEntity, bool debug, int StructureCost, ArcenHostOnlySimContext Context )
            {
                if ( Context == null )
                    return; //if client, don't do it

                Planet planet = traderEntity.Planet;

                while ( previousPlanets.Count > 10 )
                    previousPlanets.RemoveAt( 0 );

                if ( previousPlanets.Count == 0 || planet != previousPlanets[previousPlanets.Count - 1] )
                {
                    //we have just arrived at this planet. Check if a message needs to be displayed
                    Faction controllingFaction = planet.GetControllingFaction();
                    if ( controllingFaction.Type == FactionType.Player )
                    {
                        //We are on a player planet, so we will emit a message. Check if the most recent few were not Human planets
                        int indexOfLastHumanPlanet = -1;
                        for ( int i = this.previousPlanets.Count - 1; i >= 0; i-- )
                        {
                            Faction controllingPrevFaction = this.previousPlanets[i].GetControllingFaction();
                            if ( controllingPrevFaction.Type == FactionType.Player )
                            {
                                indexOfLastHumanPlanet = i;
                                break;
                            }
                        }
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( indexOfLastHumanPlanet == -1 || indexOfLastHumanPlanet < this.previousPlanets.Count - 2 )
                            {
                                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( traderEntity );

                                //Play long form message
                                World_AIW2.Instance.QueueChatMessageOrCommand( traderEntity.StartFactionColourForLog_Safe() + "Zenith Trader</color> is willing to trade on " + planet.Name, 
                                    ChatType.LogToCentralChat, "ArkChiefOfStaff_ZenithTraderArrivedOnHumanPlanet", chatHandlerOrNull );
                            }
                            else
                            {
                                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( traderEntity );

                                World_AIW2.Instance.QueueChatMessageOrCommand( traderEntity.StartFactionColourForLog_Safe() + "Zenith Trader</color> is willing to trade on " + planet.Name, 
                                    ChatType.LogToCentralChat, "ArkChiefOfStaff_ZenithTraderReadyToTrade", chatHandlerOrNull );
                                //play short for mmessage
                            }
                        }
                    }
                    this.previousPlanets.Add( planet );
                }
                if ( planet.GetControllingFaction().IsAllowedToBuyFromZenithTrader_Safe() && planet.GetControllingFactionType() != FactionType.Player )
                {
                    Faction otherFaction = planet.GetControllingFaction();
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "The Zenith Trader is visiting " + otherFaction.Type + " on " + planet.Name + " whose budget is " + otherFaction.ZenithTraderBudget + " cost for nasty pick " + StructureCost, Verbosity.DoNotShow );
                    if ( otherFaction.FactionIsDefeated )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\tFaction " + otherFaction.GetDisplayName() + " has been defeated, and can no longer buy from the Trader", Verbosity.DoNotShow );
                        return;
                    }
                    if ( otherFaction.ZenithTraderBudget > StructureCost )
                    {
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIZenithPurchase" );
                        ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( otherFaction );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "ZenithTraderPurchase-NPC" );
                        otherFaction.ZenithTraderBudget = 0;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "A " + entityData.InternalName + " has been purchased for " + planet.Name, Verbosity.DoNotShow );
                    }
                }
                if ( planet.GetFactionWithSpecialInfluenceHere().IsAllowedToBuyFromZenithTrader_Safe() )
                {
                    Faction otherFaction = planet.GetFactionWithSpecialInfluenceHere();
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "The Zenith Trader is visiting " + otherFaction.Type + " on " + planet.Name + " whose budget is " + otherFaction.ZenithTraderBudget + " cost for nasty pick " + StructureCost, Verbosity.DoNotShow );
                    if ( otherFaction.ZenithTraderBudget > StructureCost )
                    {
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NormalPlanetNastyPick" );
                        ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( otherFaction );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "ZenithTraderPurchase-NPC" );
                        otherFaction.ZenithTraderBudget = 0;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "A " + entityData.InternalName + " has been purchased for " + planet.Name, Verbosity.DoNotShow );

                    }
                }
            }
        }
    }
}
