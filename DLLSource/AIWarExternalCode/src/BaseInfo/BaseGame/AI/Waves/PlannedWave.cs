using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlannedWave : AbstractWaveBase, IProtectedListable
    {
        #region Pooling
        private static ReferenceTracker RefTracker;
        private PlannedWave()
        {            
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PlannedWave" );
            RefTracker.IncrementObjectCount();
            SetDefaults();
        }

        private static readonly ConcurrentPool<AbstractWaveBase> Pool = new ConcurrentPool<AbstractWaveBase>( "PlannedWave", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlannedWave(); } );

        public static PlannedWave GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate() as PlannedWave;
        }

        public PlannedWave CreateNewForPool()
        {
            return new PlannedWave();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            SetDefaults();
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        public void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer buffer, SerializationCommandType SerializationCmdType )
        {
            this.planetWithWarpGateIdx = buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            this.targetPlanetIdx = buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            this.disableWaveWarnings = buffer.ReadBool( MetaData );
            this.gameTimeInSecondsForLaunchWave = buffer.ReadInt32( MetaData, ReadStyle.NonNeg );

            Int16 numCompositionElements = buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.FinalComposition.Clear();
            for ( int i = 0; i < numCompositionElements; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, buffer, "TypeInWave" );
                this.FinalComposition[data] = buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            }

            this.spawnWaveDirectlyOnTarget = buffer.ReadBool( MetaData );
            this.secondsAdvanceWarningToGive = buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.isActuallyACrossPlanetAttack = buffer.ReadBool( MetaData );
            this.aiCostBudgetForWave = buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            this.playerBeingAlerted = buffer.ReadBool( MetaData );
            this.deQueueWave = buffer.ReadBool( MetaData );
            this.sendWaveThisSimStep = buffer.ReadBool( MetaData );
            this.cancelRefundRatioForNextWave = buffer.ReadFInt( MetaData );
            this.SendingFactionIndex = buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            this.TargetFactionIndex = buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            this.isReconquestWave = buffer.ReadBool( MetaData );
            this.cancelRefundRatioForNextWormholeInvasion = buffer.ReadFInt( MetaData );
            this.isExogalacticWormholeWave = buffer.ReadBool( MetaData );
            this.IsAstroTrainWave = buffer.ReadBool( MetaData );
            this.DebugString = buffer.ReadString_Condensed( MetaData );
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.planetWithWarpGateIdx );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.targetPlanetIdx );
            Buffer.AddBool( MetaData, this.disableWaveWarnings );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.gameTimeInSecondsForLaunchWave );
            if ( this.FinalComposition == null )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0 );
            else
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.FinalComposition.Count );
                foreach ( KeyValuePair<GameEntityTypeData, int> kv in this.FinalComposition )
                {
                    GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, kv.Key, Buffer, "TypeInWave" );
                    Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value );
                }
            }
            Buffer.AddBool( MetaData, this.spawnWaveDirectlyOnTarget );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.secondsAdvanceWarningToGive );
            Buffer.AddBool( MetaData, this.isActuallyACrossPlanetAttack );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.aiCostBudgetForWave );
            Buffer.AddBool( MetaData, this.playerBeingAlerted );
            Buffer.AddBool( MetaData, this.deQueueWave );
            Buffer.AddBool( MetaData, this.sendWaveThisSimStep );
            Buffer.AddFInt( MetaData, this.cancelRefundRatioForNextWave );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.SendingFactionIndex );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.TargetFactionIndex );
            Buffer.AddBool( MetaData, this.isReconquestWave );
            Buffer.AddFInt( MetaData, this.cancelRefundRatioForNextWormholeInvasion );
            Buffer.AddBool( MetaData, this.isExogalacticWormholeWave );
            Buffer.AddBool( MetaData, this.IsAstroTrainWave );
            Buffer.AddString_Condensed( MetaData, this.DebugString );
        }

        #region Temporary Lists
        private static RapidAntiLeakPool<List<PlannedWave>> InnerPoolFor_PlannedWaveList = RapidAntiLeakPool<List<PlannedWave>>.Create_WillNeverBeGCed(
            "PoolFor_TempPlannedWaveList", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return List<PlannedWave>.Create_WillNeverBeGCed( 30, "TempPlannedWaveList" ); } );

        public static List<PlannedWave> GetTemporaryPlannedWaveList( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            List<PlannedWave> list = InnerPoolFor_PlannedWaveList.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( list == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            list.Clear();
            return list;
        }

        public static void ReleaseTemporaryPlannedWaveList( List<PlannedWave> list )
        {
            InnerPoolFor_PlannedWaveList.ReturnToPool( list );
        }
        #endregion
    }
}
