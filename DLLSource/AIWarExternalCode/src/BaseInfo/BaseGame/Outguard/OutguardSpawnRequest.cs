using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class OutguardSpawnRequest : TimeBasedPoolable<OutguardSpawnRequest>, IProtectedListable
    {
        //Note these aren't being saved to disk so if you manage to
        //enqueue a request and then save in the same frame then you might have issues
        public OutguardGroupData Group;
        public Planet Planet; //You can't store a Planet object here because you can't look it up during deserialization (since the planet lookup table doesn't exist yet).
                                  //Instead always store the index and look up the Planet when you want to use it
        public ArcenPoint SpawnPoint; //can be unset
        public int SpawnSecond; //filled out in Enqueue based on the SpawnDelay in the table
        public Faction ChargeFaction; //who is paying for these outguardenaries?

        public const int REQUIRED_FIELD_COUNT = 5;

        public void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "请求在星球 " ).Add( Planet?.Name ).Add( " 上部署 " ).Add( this.Group.GetDisplayName() )
                .Add( "，坐标 " ).Add( this.SpawnPoint.X ).Add( "," ).Add( this.SpawnPoint.Y ).Add( "，时间 " ).Add( this.SpawnSecond );
        }

        public string ToString_DebugOnly()
        {
            ArcenCharacterBuffer adcb = ArcenCharacterBuffer.GetFromPoolOrCreate( "OutguardSpawnRequest-ToString_DebugOnly" );
            AppendStateForInterfaceDisplay( adcb );
            return adcb.ToStringAndReturnToPool();
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private OutguardSpawnRequest()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "OutguardSpawnRequests" );
            RefTracker.IncrementObjectCount();

            Clear();
        }

        private static readonly TimeBasedPool<OutguardSpawnRequest> Pool = TimeBasedPool<OutguardSpawnRequest>.Create_WillNeverBeGCed( "OutguardSpawnRequests", 
            2, 2, //we really only need one pool interval beyond the first, but we want to make sure there's time, so rather than doing 1, 1, it's safer to do 2, 2
            9000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new OutguardSpawnRequest(); } );

        public OutguardSpawnRequest CreateNewForPool()
        {
            return new OutguardSpawnRequest();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOugoingReferences()
        {
        }

        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
            Clear();
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleToDo()
        {
        }

        public static OutguardSpawnRequest GetFromPoolOrCreate()
        {
            OutguardSpawnRequest entry = Pool.GetFromPoolOrCreate();
            return entry;
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        public void Clear()
        {
            Group = null;
            Planet = null;
            SpawnPoint = Engine_AIW2.Instance.CombatCenter;
            SpawnSecond = -1;
            ChargeFaction = null;
        }

        public static OutguardSpawnRequest GetFromPoolOrCreate( GameCommand Command )
        {
            OutguardSpawnRequest req = GetFromPoolOrCreate();
            req.Group = OutguardGroupDataTable.Instance.GetRowByName( Command.RelatedString );
            req.Planet = World_AIW2.Instance.GetPlanetByIndex( Command.PlanetOrderWasIssuedFrom );
            if ( Command.RelatedPoints.Count < 1 )
                throw new Exception( "Error: No related points in game command!" );
            req.SpawnPoint = Command.RelatedPoints.First;
            if ( Command.RelatedIntegers.Count < 1 )
                throw new Exception( "Error: No related integers in game command!" );
            req.SpawnSecond = Command.RelatedIntegers.First;
            req.ChargeFaction = Command.GetRelatedFaction();
            return req;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddString_Condensed( MetaData, this.Group.InternalName, "Group.InternalName" );
            Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.Planet.Index, "Planet.Index" );
            Buffer.AddArcenPointFromCombatSpace( MetaData, this.SpawnPoint, "SpawnPoint" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.SpawnSecond, "SpawnSecond" );
            Buffer.AddFactionIndex_PosOnly( MetaData, this.ChargeFaction.FactionIndex, "ChargeFaction.FactionIndex" );
        }

        public void DeserializedIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Group = OutguardGroupDataTable.Instance.GetRowByName( Buffer.ReadString_Condensed( MetaData, "Group.InternalName" ) );
            this.Planet = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "Planet.Index" ) );
            Buffer.FillArcenPointFromCombatSpace( MetaData, out SpawnPoint, "SpawnPoint" );
            this.SpawnSecond = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SpawnSecond" );
            this.ChargeFaction = World_AIW2.Instance.GetFactionByIndex( Buffer.ReadFactionIndex_PosOnly( MetaData, "ChargeFaction.FactionIndex" ) );
        }
    }
}
