using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIBudgetItem : IClearableDuringXmlReload
    {
        public AIBudgetType Type;
        public int SecondsBetweenAttemptsToSpend;
        public bool SpendOnThreshold;
        public bool DoubleWaveIntervalEachTime = false;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory NormalAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory GuardPostAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory DireGuardPostAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory UnarmedGuardPostAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory TurretAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory NonTurretDefenseAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory GuardianAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory WormholeSentinelAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory ForcefieldGuardianAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory DecloakerAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory DireGuardianAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory RegularSingularFreakySurprisesAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory DireSingularFreakySurprisesAIShipGroup;
        [DumpInternalNameOnlyForArcenDynamicTableRow]
        public AIShipGroupCategory ExoLeaderAIShipGroup;

        public static string GetAIShipGroupDetails( AIShipGroup menus )
        {
            string retVal = "\nship group size: " + menus.DrawBag.InternalListSize;
            //for ( int i = 0; i < menus.Count; i++ )
            //{
            //    AIShipGroup menu = menus[i];
            //    retVal += "  menu:" + menu.InternalName;
            //    retVal += " drawbag." + menu.DrawBag.InternalListSize;
            //    retVal += " availitems." + menu.DrawBag.InternalListSize;
            //}
            return retVal;
        }

        private static ReferenceTracker RefTracker;
        private AIBudgetItem()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIBudgetItems" );
            RefTracker.IncrementObjectCount();
        }

        public static AIBudgetItem CreateNotFromPool()
        {
            return new AIBudgetItem();
        }

        public void Clear()
        {
            this.Type = AIBudgetType.None;
            this.SecondsBetweenAttemptsToSpend = 0;
            this.SpendOnThreshold = false;
            this.DoubleWaveIntervalEachTime = false;

            this.NormalAIShipGroup = null;
            this.GuardPostAIShipGroup = null;
            this.DireGuardPostAIShipGroup = null;
            this.UnarmedGuardPostAIShipGroup = null;
            this.TurretAIShipGroup = null;
            this.NonTurretDefenseAIShipGroup = null;
            this.GuardianAIShipGroup = null;
            this.WormholeSentinelAIShipGroup = null;
            this.ForcefieldGuardianAIShipGroup = null;
            this.DecloakerAIShipGroup = null;
            this.DireGuardianAIShipGroup = null;
            this.RegularSingularFreakySurprisesAIShipGroup = null;
            this.DireSingularFreakySurprisesAIShipGroup = null;
            this.ExoLeaderAIShipGroup = null;
        }

        public void WipeDuringXmlReload()
        {
            this.Clear();
        }

        #region Temporary BudgetRatioArray
        private static RapidAntiLeakPool<EnumIndexedArray<AIBudgetType, FInt>> InnerPoolFor_BudgetRatioArray = RapidAntiLeakPool<EnumIndexedArray<AIBudgetType, FInt>>.Create_WillNeverBeGCed(
            "PoolFor_TempBudgetRatioArray", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return EnumIndexedArray<AIBudgetType, FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "TempBudgetRatioArray" ); } );

        public static EnumIndexedArray<AIBudgetType, FInt> GetTemporaryBudgetRatioArray( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            EnumIndexedArray<AIBudgetType, FInt> list = InnerPoolFor_BudgetRatioArray.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( list == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            list.Clear();
            return list;
        }

        public static void ReleaseTemporaryBudgetRatioArray( EnumIndexedArray<AIBudgetType, FInt> list )
        {
            InnerPoolFor_BudgetRatioArray.ReturnToPool( list );
        }
        #endregion        
    }

    public class AIBudgetCurrentConfiguration : IArcenSerializable
    {
        private readonly EnumIndexedArray<AIBudgetType,AIBudgetItemCurrentConfiguration> Configurations = EnumIndexedArray<AIBudgetType,AIBudgetItemCurrentConfiguration>.Create_WillNeverBeGCed( false, null, "AIBudgetCurrentConfiguration-Configurations" );

        public AIBudgetCurrentConfiguration()
        {
            for ( AIBudgetType i = 0; i < AIBudgetType.Length; i++ )
                this.Configurations[i] = new AIBudgetItemCurrentConfiguration();
        }

        public void Clear()
        {
            for ( AIBudgetType i = 0; i < AIBudgetType.Length; i++ )
                this.Configurations[i].Clear();
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)AIBudgetType.Length, "AIBudgetCurrentConfiguration-AIBudgetTypeCount" );
            for ( AIBudgetType i = 0; i < AIBudgetType.Length; i++ )
                this.Configurations[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
        }

        public void DeserializeFrom( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            byte countToExpect = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "AIBudgetCurrentConfiguration-AIBudgetTypeCount" );
            for ( byte i = 0; i < countToExpect; i++ )
                this.Configurations[(AIBudgetType)i].DeserializeFrom( MetaData, Buffer, SerializationCmdType );
        }

        public AIBudgetItemCurrentConfiguration this[AIBudgetType Item] => this.Configurations[Item];
    }

    public class AIBudgetItemCurrentConfiguration : IArcenSerializable, IClearableDuringXmlReload
    {
        public FInt BudgetPortion;
        public FInt IntervalMultiplier = FInt.One;

        private static ReferenceTracker RefTracker;
        public AIBudgetItemCurrentConfiguration()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIBudgetItemCurrentConfigurations" );
            RefTracker.IncrementObjectCount();
        }

        public void Clear()
        {
            BudgetPortion = FInt.Zero;
            IntervalMultiplier = FInt.One;
        }

        public void WipeDuringXmlReload()
        {
            this.Clear();
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFInt( MetaData, this.BudgetPortion, "AIBudgetCurrentConfigurationItem-BudgetPortion" );
            Buffer.AddFInt( MetaData, this.IntervalMultiplier, "AIBudgetCurrentConfigurationItem-IntervalMultiplier" );
        }

        public void DeserializeFrom( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.Fill( MetaData, out this.BudgetPortion, "AIBudgetCurrentConfigurationItem-BudgetPortion" );
            Buffer.Fill( MetaData, out this.IntervalMultiplier, "AIBudgetCurrentConfigurationItem-IntervalMultiplier" );
        }
    }

    public enum AIBudgetType : byte
    {
        None,
        Reinforcement,
        Wave,
        CPA,
        Warden,
        Reconquest,
        HunterFleet,
        PraetorianGuard,
        WormholeInvasion,
        BorderAggression,
        Length
    }
}
