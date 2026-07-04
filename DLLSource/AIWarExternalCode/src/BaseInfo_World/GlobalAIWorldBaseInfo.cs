using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class GlobalAIWorldBaseInfo : ExternalWorldBaseInfo
    {
        /// <summary>
        /// This is a thing we usse only sparingly, but since so much here is needed for the MP client to see their top bar correctly, this is a case where it is worth it.
        /// </summary>
        public override bool IsUltraFrequentSyncedInMultiplayer { get { return true; } }

        public FInt AIProgress_Total;
        public FInt AIProgress_Reduction;
        public int AIPFloorMultiplierPercent = -1;
        public int AIPAbsoluteFloor = -1;
        public int AIPNeverReducesBelow = -1;
        public readonly ProtectedList<AIPChange> AIPChangeHistory = ProtectedList<AIPChange>.Create_WillNeverBeGCed( 3000, "GlobalAIWorldBaseInfo-AIPChangeHistory" );

        public FInt AIProgress_Floor 
        {
            get
            {
                if (AIWar2GalaxySettingQuickAccess.AIPFloorRebalance)
                {
                    if (AIProgress_Total < AIPNeverReducesBelow)
                        return (FInt)AIPNeverReducesBelow;

                    var amt = AIProgress_Total - AIPNeverReducesBelow;
                    var res = (FInt)(amt * AIPFloorMultiplierPercent / 100);
                    res += AIPNeverReducesBelow;

                    return res;
                }

                return Mat.Max( (FInt)this.AIPAbsoluteFloor, (AIPFloorMultiplierPercent * AIProgress_Total) / 100 );
            }
        }

        /* For Showdown Devices */

        //If the player owns >= N-1 showdown devices (so if 5 are seeded, the player needs to own 4)
        //then they can hack any of them to trigger the showdown device crisis countdown.
        //If they player survives some minutes of non-stop assaults, the AI Overlords transform and all the other AI ships
        //in the galaxy will join the wave faction
        public int TimeForNextExo;
        public int TimeForNextWormholeInvasion;
        public int StrengthForNextExo;
        public int StrengthForNextWormholeInvasion;
        public int SecondsUntilCrisis; //countdown till crisis

        public bool HasSpawnedDevices; //for initial setup
        public bool CrisisCountdownTriggered; //Triggered when the player starts the showdown devices
        public bool CrisisFailed; //this happens when SecondsUntilCrisis == 0; the AI overlords transform and all that
        public bool CrisisTriggered; //this happens when SecondsUntilCrisis == 0; the AI overlords transform and all that

        public int DevicesToSpawn = 4;
        public static GlobalAIWorldBaseInfo Instance;

        private static ReferenceTracker RefTracker;
        public GlobalAIWorldBaseInfo()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "GlobalAIWorldBaseInfos" );
            RefTracker.IncrementObjectCount();

            Instance = this;
            SetDefaults();
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            AIProgress_Total = FInt.Zero;
            AIProgress_Reduction = FInt.Zero;
            AIPFloorMultiplierPercent = -1;
            AIPAbsoluteFloor = -1;
            AIPNeverReducesBelow = -1;
            AIPChangeHistory.Clear( true );

            TimeForNextExo = -1;
            TimeForNextWormholeInvasion = -1;
            StrengthForNextExo = -1;
            StrengthForNextWormholeInvasion = -1;
            SecondsUntilCrisis = -1;
            HasSpawnedDevices = false;
            CrisisCountdownTriggered = false;
            CrisisTriggered = false;
            CrisisFailed = false;
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "GlobalAIWorldBaseInfo";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        #region SerializeTo
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "GlobalAIWorldBaseInfo" );
            Buffer.AddFInt( MetaData, this.AIProgress_Total, "AIProgress_Total" );
            Buffer.AddFInt( MetaData, this.AIProgress_Reduction, "AIProgress_Reduction" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (Int16)this.AIPFloorMultiplierPercent, "AIPFloorMultiplierPercent" );
            for ( int i = 0; i < this.AIPChangeHistory.Count; i++ )
            {
                Buffer.AddBool( MetaData, true, "HasAnotherAIPHistoryElement" );
                this.AIPChangeHistory[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddBool( MetaData, false, "HasAnotherAIPHistoryElement" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextExo, "TimeForNextExo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextWormholeInvasion, "TimeForNextWormholeInvasion" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.StrengthForNextExo, "StrengthForNextExo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.StrengthForNextWormholeInvasion, "StrengthForNextWormholeInvasion" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.SecondsUntilCrisis, "SecondsUntilCrisis" );
            Buffer.AddBool( MetaData, this.HasSpawnedDevices, "HasSpawnedDevices" );
            Buffer.AddBool( MetaData, this.CrisisCountdownTriggered, "CrisisCountdownTriggered" );
            Buffer.AddBool( MetaData, this.CrisisFailed, "CrisisFailed" );
            Buffer.AddBool( MetaData, this.CrisisTriggered, "CrisisTriggered" );
        }
        #endregion

        #region DeserializeIntoSelf
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) 
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "GlobalAIWorldBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "GlobalAIWorldBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.AIProgress_Total = Buffer.ReadFInt( MetaData, "AIProgress_Total" );
            this.AIProgress_Reduction = Buffer.ReadFInt( MetaData, "AIProgress_Reduction" );
            this.AIPFloorMultiplierPercent = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "AIPFloorMultiplierPercent" );
            AIPChangeHistory.Clear( true );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 508 ) )
            {
                while ( Buffer.ReadBool( MetaData, "HasAnotherAIPHistoryElement" ) )
                {
                    AIPChange C = AIPChange.GetFromPoolOrCreate();
                    C.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); 
                    AIPChangeHistory.Add( C );
                }
            }
            else
            {
                int countToExpect = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "AIPChangeHistory.Count" );
                AIPChangeHistory.DeserializeUncertainNumberOfEntriesIntoExistingList( countToExpect,
                   delegate { return AIPChange.GetFromPoolOrCreate(); },
                   delegate ( AIPChange C ) { C.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); } );
            }
            this.TimeForNextExo = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextExo" );
            this.TimeForNextWormholeInvasion = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextWormholeInvasion" );
            this.StrengthForNextExo = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "StrengthForNextExo" );
            this.StrengthForNextWormholeInvasion = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "StrengthForNextWormholeInvasion" );
            this.SecondsUntilCrisis = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SecondsUntilCrisis" );
            this.HasSpawnedDevices = Buffer.ReadBool( MetaData, "HasSpawnedDevices" );
            this.CrisisCountdownTriggered = Buffer.ReadBool( MetaData, "CrisisCountdownTriggered" );
            this.CrisisFailed = Buffer.ReadBool( MetaData, "CrisisFailed" );
            this.CrisisTriggered = Buffer.ReadBool( MetaData, "CrisisTriggered" );
        }
        #endregion

        #region DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( World_AIW2.Instance.AIFactions.Count == 0 )
                return;

            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.AIFactions[i];
                AISentinelsCoreData sentinelsCore = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( sentinelsCore == null )
                    continue;
                if ( this.AIPFloorMultiplierPercent < sentinelsCore.AIDifficulty.AIPFloorMultiplierPercent )
                {
                    //set the floor multiplier if it's not set to the highest of any current AI
                    this.AIPFloorMultiplierPercent = sentinelsCore.AIDifficulty.AIPFloorMultiplierPercent;
                }
                if ( this.AIPAbsoluteFloor < sentinelsCore.AIDifficulty.AIPAbsoluteFloor )
                {
                    //set the absolute floor if it's not set to the highest of any current AI
                    this.AIPAbsoluteFloor = sentinelsCore.AIDifficulty.AIPAbsoluteFloor;
                }
                if ( this.AIPNeverReducesBelow < sentinelsCore.AIDifficulty.AIPNeverReducesBelow )
                {
                    //set the never-reduces-below if it's not set to the highest of any current AI
                    this.AIPNeverReducesBelow = sentinelsCore.AIDifficulty.AIPNeverReducesBelow;
                }
            }
        }
        #endregion

        protected override void DoPerSecondLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        
        #region ReductionLessThanTotal
        public FInt ReductionLessThanTotal()
        {
            if ( AIProgress_Reduction > AIProgress_Total )
                return AIProgress_Total;
            
            return AIProgress_Reduction;
        }
        #endregion

        #region AIProgress_Effective
        public FInt AIProgress_Effective
        {
            get
            {
                FInt val = AIProgress_Total;
                
                if (val < AIPNeverReducesBelow)
                    return val;
                
                val = val - ReductionLessThanTotal();
                if ( AIProgress_Floor > val )
                    val = AIProgress_Floor;
                
                if ( (FInt)AIPNeverReducesBelow > val )
                    val = (FInt)AIPNeverReducesBelow;

                return val;
            }
        }
        #endregion
        
        #region ChangeAIP

        public void ChangeAIP( FInt Change, AIPChangeReason Reason, GameEntityTypeData RelatedEntityTypeData, Int16 FactionIndex,
            Int16 PlanetIndex, Int16 secondaryFactionIndex )
        {
            ChangeAIP( Change, Reason, RelatedEntityTypeData, null, FactionIndex, PlanetIndex, secondaryFactionIndex );
        }

        public void ChangeAIP( FInt Change, AIPChangeReason Reason, GameEntityTypeData RelatedEntityTypeData, GameEntityTypeData SecondaryRelatedEntityTypeData, Int16 FactionIndex,
            Int16 PlanetIndex, Int16 secondaryFactionIndex )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            Change *= (AIWar2GalaxySettingQuickAccess.AIPMultiplier.ToFInt() / 100.ToFInt());
            
            FInt startingNet = this.AIProgress_Effective;
            if ( Change >= 0 )
                this.AIProgress_Total += Change;
            else
                this.AIProgress_Reduction += -Change;

            AIPChange record = AIPChange.Create( Change, Reason, RelatedEntityTypeData, SecondaryRelatedEntityTypeData, FactionIndex, PlanetIndex, this.AIProgress_Floor, secondaryFactionIndex );
            this.AIPChangeHistory.Add( record );
            
            if ( Reason == AIPChangeReason.InitialValue)
                goto done;
            
            if ( !World_AIW2.Instance.IsOutsideOfNormalGameplay && 
                 ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
            {
                FInt endingNet = this.AIProgress_Effective;
                int aipFloor = this.AIProgress_Floor.IntValue;
                int aipNeverReducesBelow = -1;
                AIDifficulty highestDifficulty = null;

                FInt highestAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    if ( fac.Type != FactionType.AI )
                        continue;

                    AISentinelsCoreData sentinelInfo = fac.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( this == null || sentinelInfo.AIDifficulty == null )
                        continue;

                    if ( highestDifficulty == null || sentinelInfo.AIDifficulty.Difficulty > highestDifficulty.Difficulty )
                        highestDifficulty = sentinelInfo.AIDifficulty;
                    aipFloor = this.AIProgress_Floor.IntValue;
                    aipNeverReducesBelow = this.AIPNeverReducesBelow;
                }

                if ( aipFloor < aipNeverReducesBelow )
                    aipFloor = aipNeverReducesBelow;
                if ( highestDifficulty != null )
                {
                    if ( aipFloor < highestDifficulty.AIPAbsoluteFloor )
                        aipFloor = highestDifficulty.AIPAbsoluteFloor;
                }
                if ( aipFloor > endingNet.IntValue )
                    aipFloor = endingNet.IntValue;

                if (record.Change != 0)
                {
                    AIPHistoryChatHandler chatHandlerOrNull = ChatClickHandler.CreateNewAs<AIPHistoryChatHandler>( "AIPHistory" );
                    World_AIW2.Instance.QueueChatMessageOrCommand( record.GetDescription(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                }

                if ( startingNet.IntValue == aipFloor )
                {
                    if ( Change < 0 )
                    {
                        AIPHistoryChatHandler chatHandlerOrNull = ChatClickHandler.CreateNewAs<AIPHistoryChatHandler>( "AIPHistory" );
                        World_AIW2.Instance.QueueChatMessageOrCommand( "AIP Floor was hit!  Reduction will apply more later as AIP rises.  Current floor is: " +
                            aipFloor, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                }
            }

            if ( Change > 0 && 
                 ( AIWar2GalaxySettingQuickAccess.SciencePoints_PerAIP > 0 || 
                   AIWar2GalaxySettingQuickAccess.HackingPoints_PerAIP > 0 ) )
            {
                Faction causedBy = World_AIW2.Instance.GetFactionByIndex( FactionIndex );

                if ( AIWar2GalaxySettingQuickAccess.SciencePoints_PerAIP > 0 )
                {
                    FInt amount = ( AIWar2GalaxySettingQuickAccess.SciencePoints_PerAIP * Change ) / 100;
                    if ( AIWar2GalaxySettingQuickAccess.SciencePointsScaling.Equals( "Individual Harvest" ) )
                    {
                        if ( causedBy != null && causedBy.Type == FactionType.Player )
                            causedBy.StoredScience += amount;
                        else
                        {
                            amount /= World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            for ( int i = 0; i < World_AIW2.Instance.EmpireStylePlayerFactions.Count; i++ )
                                World_AIW2.Instance.EmpireStylePlayerFactions[i].StoredScience += amount;
                        }
                    }
                    switch ( AIWar2GalaxySettingQuickAccess.SciencePointsScaling )
                    {
                        case "Soft Decline":
                            int humanEmpireCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            if ( humanEmpireCount >= 3 )
                                amount /= 2;
                            else if ( humanEmpireCount >= 2 )
                                amount = (amount * 2) / 3;
                            break;
                        case "Linear Decline":
                            amount /= World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            break;
                    }
                    for ( int i = 0; i < World_AIW2.Instance.EmpireStylePlayerFactions.Count; i++ )
                        World_AIW2.Instance.EmpireStylePlayerFactions[i].StoredScience += amount;
                }

                if ( AIWar2GalaxySettingQuickAccess.HackingPoints_PerAIP > 0 )
                {
                    FInt amount = ( AIWar2GalaxySettingQuickAccess.HackingPoints_PerAIP * Change ) / 100;
                    if ( AIWar2GalaxySettingQuickAccess.HackingPointsScaling.Equals( "Individual Harvest" ) )
                    {
                        if ( causedBy != null && causedBy.Type == FactionType.Player )
                            causedBy.StoredHacking += amount;
                        else
                        {
                            amount /= World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            for ( int i = 0; i < World_AIW2.Instance.EmpireStylePlayerFactions.Count; i++ )
                                World_AIW2.Instance.EmpireStylePlayerFactions[i].StoredHacking += amount;
                        }
                    }
                    switch ( AIWar2GalaxySettingQuickAccess.HackingPointsScaling )
                    {
                        case "Soft Decline":
                            int humanEmpireCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            if ( humanEmpireCount >= 3 )
                                amount /= 2;
                            else if ( humanEmpireCount >= 2 )
                                amount = (amount * 2) / 3;
                            break;
                        case "Linear Decline":
                            amount /= World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1;
                            break;
                    }
                    for ( int i = 0; i < World_AIW2.Instance.EmpireStylePlayerFactions.Count; i++ )
                        World_AIW2.Instance.EmpireStylePlayerFactions[i].StoredHacking += amount;
                }
            }

            done:
            
            ArcenExternalCodeHook.Invoke("OnAIPChange", Change, Reason, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost);
        }
        #endregion

        /// <summary>
        /// Calculate how much AIP can be earned before reaching the target AIP.
        /// This takes into account the effect of unused AIP reduction and AIP floor increases.
        /// </summary>
        public int CalculateAIPRemainingUntil(int targetAIP)
        {
            if (targetAIP <= AIPNeverReducesBelow) {
                return (targetAIP - AIProgress_Total).IntValue;
            }
            // Calculate the remaining AIP until the target AIP, assuming the floor is not relevant
            FInt remainingAIP = targetAIP - AIProgress_Total + AIProgress_Reduction;
            // Calculate the AIP Floor we would have after earning that much more AIP
            FInt aipFloorAtTarget = (AIProgress_Total + remainingAIP) * AIPFloorMultiplierPercent / 100;
            // If the floor would be greater than our target, calculate the
            // total AIP that would result in the floor being our target AIP.
            // The remaining AIP is that minus our current total.
            if (aipFloorAtTarget > targetAIP) {
                remainingAIP = targetAIP * 100 / AIPFloorMultiplierPercent - AIProgress_Total;
            }
            return remainingAIP.IntValue;
        }
    }
}
