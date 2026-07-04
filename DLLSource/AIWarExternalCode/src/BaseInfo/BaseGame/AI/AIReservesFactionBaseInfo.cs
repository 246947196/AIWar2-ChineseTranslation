using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIReservesFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {      
        //serialized
        public bool ReservesActive;
        public bool AbsorbShipsMode;
        public int TimeReservesActivated;
        public int TimePlayerIncursionStarted;
        public int TimeForNextWormhole;

        //Not serialized to disk
        public static AIReservesFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public bool DespawnOldRegimeShips; //old-style ai reserves before the rework just despawn
        public bool DropExistingOrders; //when we flip to absorb mode, drop existing orders
        public List<Planet> PlanetsTriggeringAttackHostOnly = List<Planet>.Create_WillNeverBeGCed(5, "AIReserves-PlanetsTriggeringAttackHostOnly");

        public readonly DoubleBufferedList<SafeSquadWrapper> Wormholes = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "AIReserves-Wormholes" );

        public AIReservesFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            ReservesActive = false;
            AbsorbShipsMode = false;
            TimeReservesActivated = -1;
            TimeForNextWormhole = -1;

            DespawnOldRegimeShips = false;
            DropExistingOrders = false;

            Wormholes.Clear();
            PlanetsTriggeringAttackHostOnly.Clear();

            Instance = null;
            hasInitializedConstants = false; //force reload of xml
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are always here, don't tell us about this
            return 0;
        }

        #region Xml Constants
        private bool hasInitializedConstants = false;
        public int MinDepthForDeepstrike = 0;
        public int MinStrengthToTrigger = 0;
        public int TimeRemainsOnAlert = 0;

        private void InitializeConstantsIfNecessary()
        {
            if ( hasInitializedConstants )
                return;
            hasInitializedConstants = true;
            MinStrengthToTrigger = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AIReserves_MinStrengthToTrigger" );
            MinDepthForDeepstrike = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AIReserves_MinDepthForDeepstrike" );
            TimeRemainsOnAlert = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_AIReserves_TimeRemainsOnAlert" );

            if ( MinDepthForDeepstrike == 0 )
                throw new Exception( "Error parsing AI Reserves XML path 2" );
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddBool( MetaData, ReservesActive, "ReservesActive" );
            Buffer.AddBool( MetaData, AbsorbShipsMode, "AbsorbShipsMode" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeReservesActivated, "TimeReservesActivated" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimePlayerIncursionStarted, "TimePlayerIncursionStarted" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextWormhole, "TimeForNextWormhole" );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.ReservesActive = Buffer.ReadBool( MetaData, "ReservesActive" );
            this.AbsorbShipsMode = Buffer.ReadBool( MetaData, "AbsorbShipsMode" );
            this.TimeReservesActivated = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeReservesActivated" );
            this.TimePlayerIncursionStarted = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimePlayerIncursionStarted" );
            this.TimeForNextWormhole = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextWormhole" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //this is based only on the AI this is part of
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            InitializeConstantsIfNecessary();
        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
        }
        #endregion

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            Wormholes.ClearConstructionListForStartingConstruction();
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "AIReservesSpawnPoint" ) )
            {
                Wormholes.AddToConstructionList( entity );
            }
            Wormholes.SwitchConstructionToDisplay();
        }

        #region GetEffectiveGracePeriod
        public int GetEffectiveGracePeriod()
        {
            int gracePeriodAmount = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "AIReservesGracePeriod" );
            int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
            if ( highestDifficulty >= 9 )
                return 0;
            if ( highestDifficulty >= 8 )
                return gracePeriodAmount / 2;
            return gracePeriodAmount;
        }
        #endregion

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            int debugStage = 1;
            try
            {
                #region Fill NonSim Notifications List Relating To AI Reserves
                {
                    AIReservesFactionBaseInfo reservesData = FactionUtilityMethods.Instance.GetAIReservesFactionBaseInfo();
                    if ( reservesData != null && reservesData.ReservesActive )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Faction = FactionUtilityMethods.Instance.GetAIReservesFaction();
                        try{
                            for ( int i = 0; i < reservesData.PlanetsTriggeringAttackHostOnly.Count; i++  )
                                fillData.PlanetList.Add( reservesData.PlanetsTriggeringAttackHostOnly[i] );
                        } catch {} //this can race with the LRP code that fills the list
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicAIReservesNotifier.Instance, fillData, "", 0, "Reserves", SortedNotificationPriorityLevel.Major );
                    }
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "AI Reserves.Notifications Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
