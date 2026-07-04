using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public partial class AISentinelsCoreData : AICoreDataRoot, IAISentinelsCoreData
    {
        protected override string TracingNameRoot => "SetinelsCore";

        public AITypeData AIType;
        public bool WasRandomAIType;
        public TypeDifficulty AdaptiveAIDifficulty;
        public AIDifficulty AIDifficulty;
        public readonly AIBudgetCurrentConfiguration CurrentBudgetConfiguration = new AIBudgetCurrentConfiguration();
        public readonly Dictionary<AIBudgetType, int> NextEventTime = Dictionary<AIBudgetType, int>.Create_WillNeverBeGCed( 20, "AISentinelsCoreData-NextEventTime" );
        public int PreviousWaveLength;
        public bool WavesTemporarilyDisabled;
        public readonly EnumIndexedArray<AIBudgetType,FInt> StoredAIPurchaseCostByBudget = EnumIndexedArray<AIBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AISentinelsCoreData-StoredAIPurchaseCostByBudget" );
        public readonly ProtectedList<ExtragalacticBudget> ExtragalacticBudgets = ProtectedList<ExtragalacticBudget>.Create_WillNeverBeGCed( 80, "AISentinelsCoreData-ExtragalacticBudgets" );
        public FInt WormholeBorerBudget; //kept separate since it uses a different mechanism than the regular budgets

        public AISentinelsCoreData( AISentinelsFactionBaseInfo BaseInfo ) : base( BaseInfo ) { }

        public override void Cleanup()
        {
            AIType = null;
            WasRandomAIType = false;
            AdaptiveAIDifficulty = TypeDifficulty.Unset;
            AIDifficulty = null;
            CurrentBudgetConfiguration.Clear();
            NextEventTime.Clear();
            PreviousWaveLength = -1;
            WavesTemporarilyDisabled = false;
            StoredAIPurchaseCostByBudget.Clear();
            ExtragalacticBudgets.Clear( true );
            WormholeBorerBudget = FInt.Zero;
        }

        // write to buffer this ai's type, if visible to the player
        // returns true if anything was written
        public bool WriteAITypeDisplayString(ArcenCharacterBufferBase buffer)
        {
            bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && 
                                        World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
            var showRandomAiType = GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" );

            if (secretFactionDetails)
                return false;
            
            if ( this.WasRandomAIType )
            {
                if ( showRandomAiType )
                {
                    buffer.Add( this.AIType.DisplayName );
                } 
                else
                {
                    buffer.Add( "随机" );
                }
            } 
            else 
            if ( this.AdaptiveAIDifficulty != TypeDifficulty.Unset )
            {
                if ( showRandomAiType )
                {
                    buffer.Add( this.AIType.DisplayName );
                } 
                else
                {
                    buffer.Add( "自适应" );
                }
            } 
            else
            {
                buffer.Add( this.AIType.DisplayName );
            }
                
            return true;
        }
        
        #region Ser / Deser
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "AISentinelData" );
            AITypeDataTable.Instance.SerializeByInternalName( MetaData, this.AIType, Buffer, "AIType" );
            AIDifficultyTable.Instance.SerializeByInternalName( MetaData, this.AIDifficulty, Buffer, "AIDifficulty" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.CurrentBudgetConfiguration.SerializeTo( MetaData, Buffer, SerializationCmdType );
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)AIBudgetType.Length, "AIBudgetTypeLength");
                for (AIBudgetType i = 0; i < AIBudgetType.Length; i++)
                    Buffer.AddFInt(MetaData, this.StoredAIPurchaseCostByBudget[i], "BudgetAmount");

                byte pairCount = (byte)this.NextEventTime.Count;
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, pairCount, "NextEventTimeCount" );
                foreach ( KeyValuePair<AIBudgetType, int> kv in this.NextEventTime )
                {
                    Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)kv.Key, "AIBudgetTypeForEvent" );
                    Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, kv.Value, "TimeForEvent" );
                }
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.PreviousWaveLength, "PreviousWaveLength" );
                Buffer.AddBool( MetaData, this.WavesTemporarilyDisabled, "WavesTemporarilyDisabled" );
            }
            Buffer.AddBool( MetaData, this.WasRandomAIType, "WasRandomAIType" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.AdaptiveAIDifficulty, "AdaptiveAIDifficulty" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ExtragalacticBudgets.Count, "ExtragalacticBudgetsCount" );
            for ( int i = 0; i < this.ExtragalacticBudgets.Count; i++ )
            {
                this.ExtragalacticBudgets[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddFInt( MetaData, this.WormholeBorerBudget, "WormholeBorerBudget" );
        }

        public void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "AISentinelData" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "AISentinelData Ext", TrackerStyle.ByTypeOnly );
            this.AIType = AITypeDataTable.Instance.DeserializeByInternalName( MetaData, Buffer, "AIType" );
            this.AIDifficulty = AIDifficultyTable.Instance.DeserializeByInternalName( MetaData, Buffer, "AIDifficulty" );
            int countToExpect;
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.CurrentBudgetConfiguration.DeserializeFrom(MetaData, Buffer, SerializationCmdType);

                countToExpect = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "AIBudgetTypeLength");
                for (int i = 0; i < countToExpect; i++)
                {
                    this.StoredAIPurchaseCostByBudget[(AIBudgetType)i] = Buffer.ReadFInt(MetaData, "BudgetAmount");
                }
                this.NextEventTime.Clear();
                byte pairCount = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "NextEventTimeCount");
                for (int i = 0; i < pairCount; i++)
                {
                    AIBudgetType type = (AIBudgetType)Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "AIBudgetTypeForEvent");
                    this.NextEventTime[type] = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "TimeForEvent");
                }
                this.PreviousWaveLength = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "PreviousWaveLength");
                this.WavesTemporarilyDisabled = Buffer.ReadBool(MetaData, "WavesTemporarilyDisabled");
            }
            this.WasRandomAIType = Buffer.ReadBool( MetaData, "WasRandomAIType" );
            this.AdaptiveAIDifficulty = (TypeDifficulty)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "AdaptiveAIDifficulty" );

            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ExtragalacticBudgetsCount" );
            this.ExtragalacticBudgets.DeserializeUncertainNumberOfEntriesIntoExistingList( count,
                delegate { return ExtragalacticBudget.GetFromPoolOrCreate(); },
                delegate ( ExtragalacticBudget item ) { item.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); } );

            this.WormholeBorerBudget = Buffer.ReadFInt( MetaData, "WormholeBorerBudget" );
            Buffer.StopTrackerByName( "AISentinelData Ext" );
        }
        #endregion
    }
}
