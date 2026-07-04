using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeInvasionFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        public readonly List<WormholeInvasionData> IncomingInvasionList = List<WormholeInvasionData>.Create_WillNeverBeGCed( 5, "WormholeInvasionFactionBaseInfo-IncomingInvasionList" );
        public int BlockedInvasionStreak;
        public int TotalBlockedInvasions;
        public int TotalInvasions;
        //active invasion data is tracked on the wormhole projector per-unit data


        //Not serialized to disk
        public static WormholeInvasionFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public readonly DoubleBufferedList<SafeSquadWrapper> WormholeProjectors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "WormholeInvasion-WormholeProjectors" );
        public bool DebugSpawnInvasion;
        public int DebugInvasionStrength;
        public WormholeInvasionFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            IncomingInvasionList.Clear();
            BlockedInvasionStreak = -1;
            TotalBlockedInvasions = -1;
            TotalInvasions = 0;

            WormholeProjectors.Clear();
            Instance = null;
            hasInitializedConstants = false; //force reload of xml
            DebugSpawnInvasion = false;
            DebugInvasionStrength = -1;
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

        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            for ( int i = 0; i < this.IncomingInvasionList.Count; i++ )
            {
                Buffer.AddBool( MetaData, true, "HasAnotherIncomingInvasion" );
                this.IncomingInvasionList[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddBool( MetaData, false, "HasAnotherIncomingInvasion" );

            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (Int16)BlockedInvasionStreak, "BlockedInvasionStreak" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (Int16)TotalBlockedInvasions, "TotalBlockedInvasions" );
            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.K_0_To_8ˌ191, TotalInvasions, "TotalInvasions" );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.IncomingInvasionList.Clear();
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 508 ) )
            {
                while ( Buffer.ReadBool( MetaData, "HasAnotherIncomingInvasion" ) )
                {
                    WormholeInvasionData Data = WormholeInvasionData.GetFromPoolOrCreate();
                    Data.DeserializeFrom( MetaData, Buffer, SerializationCmdType );
                    IncomingInvasionList.Add( Data );
                }
            }
            else
            {
                Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "IncomingInvasionListCount" );
                IncomingInvasionList.DeserializeUncertainNumberOfEntriesIntoExistingList( count,
                    delegate { return WormholeInvasionData.GetFromPoolOrCreate(); },
                    delegate ( WormholeInvasionData Data ) { Data.DeserializeFrom( MetaData, Buffer, SerializationCmdType ); } );
            }
            this.BlockedInvasionStreak = (int)Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "BlockedInvasionStreak" );
            this.TotalBlockedInvasions = (int)Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "TotalBlockedInvasions" );
            this.TotalInvasions = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.K_0_To_8ˌ191, "TotalInvasions" );
        }
        #endregion


        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //might need to be a real number? unsure what this does
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                Instance = this;
                InitializeConstantsIfNecessary();
                for ( int i = this.IncomingInvasionList.Count - 1; i >= 0; i-- )
                {
                    debugCode = 200;
                    WormholeInvasionData data = this.IncomingInvasionList[i];
                    if ( data == null )
                        continue;
                    debugCode = 300;
                    if ( data.WormholeProjectorId != -1 && data.WormholeProjector == null )
                        data.WormholeProjector = World_AIW2.Instance.GetEntityByID_Squad( data.WormholeProjectorId );
                    debugCode = 400;
                    if ( data.HasSpawnedProjector &&
                         data.WormholeProjector.GetHasBeenDestroyed() )
                    {
                        this.IncomingInvasionList.RemoveAt( i ); //the projector has died,
                        continue;
                    }
                    debugCode = 500;
                    if ( data.ResponsibleAIFaction == null )
                        data.ResponsibleAIFaction = World_AIW2.Instance.GetFactionByIndex( data.ResponsibleAIFactionIdx );
                    this.IncomingInvasionList[i] = data;
                    debugCode = 600;
                    if ( data.ProjectorAppearanceTime < World_AIW2.Instance.GameSecond ) //we've definitely spawned the projector
                    {
                        debugCode = 700;
                        if ( ( data.WormholeProjector == null ) ) //the wormhole projector is dead
                            this.IncomingInvasionList.RemoveAt( i ); //the projector has died, no more notification
                        if ( data.WaveData.Count == 0  ) //we've sent all our waves, so this is now just a Projector linking things
                            this.IncomingInvasionList.RemoveAt( i ); //we're done with this one, since the projector is dead and it was definitely created
                    }
                }
            }catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in WormholeInvasion DoFactionGeneralAggregationsPausedOrUnpaused debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {

        }
        #endregion

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            WormholeProjectors.ClearConstructionListForStartingConstruction();
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "WormholeProjector" ) )
            {
                WormholeProjectors.AddToConstructionList( entity );
            }
            WormholeProjectors.SwitchConstructionToDisplay();
        }
    }
}
