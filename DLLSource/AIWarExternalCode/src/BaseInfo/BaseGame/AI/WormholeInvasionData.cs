using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeInvasionData : ConcurrentPoolable<WormholeInvasionData>, IProtectedListable
    {
        public int TurretStrength;
        public int GuardStrength;

        public Planet InvasionStartPlanet;
        public Planet InvasionDestinationPlanet;

        public int ProjectorAppearanceTime; //whne the projector itself will appear
        public int PlanetLinkTime; //when the wormhole will link the planets

        //When the invasion is generated, we figure out all the waves that will hit immediately,
        //then track that. This lets us be more verbose and/or in our warnings to the players if we want
        //contains things like "time when wave will hit" and "here's the exact composition"
        public readonly ProtectedList<WormholeWaveData> WaveData = ProtectedList<WormholeWaveData>.Create_WillNeverBeGCed( 20, "WormholeInvasionData-WaveData" );

        public Int16 ResponsibleAIFactionIdx;
        public int WormholeProjectorId;

        public bool HasSpawnedProjector;

        //Not serialized
        public GameEntity_Squad WormholeProjector;
        public Faction ResponsibleAIFaction;

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "This invasion links " ).Add( InvasionStartPlanet.Name ).Add( " and " ).Add( InvasionDestinationPlanet.Name ).Add( ". HasSpawnedProjector " ).Add( HasSpawnedProjector );
            if ( this.ProjectorAppearanceTime > World_AIW2.Instance.GameSecond )
                buffer.Add( ". It will appear in " ).Add( (this.ProjectorAppearanceTime - World_AIW2.Instance.GameSecond ) ).Add( " seconds." );
            else if ( this.PlanetLinkTime > World_AIW2.Instance.GameSecond )
                buffer.Add( ". It will link the planets in " ).Add( (this.PlanetLinkTime - World_AIW2.Instance.GameSecond ) ).Add( " seconds." );
            else if ( this.WaveData.Count > 0 )
            {
                buffer.Add( ". It will launch a wave in " ).Add( (this.WaveData[0].TimeForWave - World_AIW2.Instance.GameSecond ) ).Add( " seconds." );
            }            
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private WormholeInvasionData()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "WormholeInvasionDatas" );
            RefTracker.IncrementObjectCount();

            Clear();
        }

        private static readonly ConcurrentPool<WormholeInvasionData> Pool = new ConcurrentPool<WormholeInvasionData>( "WormholeInvasionDatas", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new WormholeInvasionData(); } );

        public WormholeInvasionData CreateNewForPool()
        {
            return new WormholeInvasionData();
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
            Clear();
        }

        public static WormholeInvasionData GetFromPoolOrCreate()
        {
            WormholeInvasionData entry = Pool.GetFromPoolOrCreate();
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
            this.TurretStrength = 0;
            this.GuardStrength = 0;

            this.InvasionStartPlanet = null;
            this.InvasionDestinationPlanet = null;

            this.ProjectorAppearanceTime = 0;
            this.PlanetLinkTime = 0;

            this.WaveData.Clear( true );

            this.ResponsibleAIFactionIdx = 0;
            this.WormholeProjectorId = 0;

            this.HasSpawnedProjector = false;

            this.WormholeProjector = null;
            this.ResponsibleAIFaction = null;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TurretStrength, "TurretStrength" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GuardStrength, "GuardStrength" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ProjectorAppearanceTime, "ProjectorAppearanceTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, PlanetLinkTime, "PlanetLinkTime" );

            if ( InvasionStartPlanet != null )
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, InvasionStartPlanet.Index, "InvasionStartIndex" );
            else
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, -1, "InvasionStartIndex" );

            if ( InvasionDestinationPlanet != null )
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, InvasionDestinationPlanet.Index, "InvasionDestinationIndex" );
            else
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, -1, "InvasionDestinationIndex" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (Int16)WaveData.Count, "WaveDataCount" );
            for ( int i = 0; i < WaveData.Count; i++ )
                WaveData[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, ResponsibleAIFactionIdx, "ResponsibleAIFactionIdx" );
            if ( WormholeProjector != null )
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, WormholeProjector.PrimaryKeyID, "WormholeProjector" );
            else
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, -1, "WormholeProjector" );
            Buffer.AddBool( MetaData, HasSpawnedProjector, "HasSpawnedProjector" );
        }

        public void DeserializeFrom( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TurretStrength = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TurretStrength" );
            this.GuardStrength = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "GuardStrength" );
            this.ProjectorAppearanceTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ProjectorAppearanceTime" );
            this.PlanetLinkTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "PlanetLinkTime" );
            this.InvasionStartPlanet = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "InvasionStartIndex" ) );
            this.InvasionDestinationPlanet = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "InvasionDestinationIndex" ) );
            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "WaveDataCount" );
            WaveData.DeserializeUncertainNumberOfEntriesIntoExistingList( count,
                delegate { return WormholeWaveData.GetFromPoolOrCreate(); },
                delegate ( WormholeWaveData Data ) { Data.DeserializeFrom( MetaData, Buffer, SerializationCmdType ); } );

            this.ResponsibleAIFactionIdx  = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "ResponsibleAIFactionIdx" );
            this.WormholeProjectorId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "WormholeProjector" );
            this.WormholeProjector = null;
            this.ResponsibleAIFaction = World_AIW2.Instance.GetFactionByIndex( this.ResponsibleAIFactionIdx );
            this.HasSpawnedProjector = Buffer.ReadBool( MetaData );
        }
    }

    public class WormholeWaveData : ConcurrentPoolable<WormholeWaveData>, IProtectedListable
    {
        public int TimeForWave;
        public readonly Dictionary<GameEntityTypeData, int> ShipsInWave = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 10, "WormholeWaveData-ShipsInWave" );

        #region Pooling
        private static ReferenceTracker RefTracker;
        private WormholeWaveData()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "WormholeWaveDatas" );
            RefTracker.IncrementObjectCount();

            Clear();
        }

        private static readonly ConcurrentPool<WormholeWaveData> Pool = new ConcurrentPool<WormholeWaveData>( "WormholeWaveDatas", 9000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new WormholeWaveData(); } );

        public WormholeWaveData CreateNewForPool()
        {
            return new WormholeWaveData();
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
            Clear();
        }

        public static WormholeWaveData GetFromPoolOrCreate()
        {
            WormholeWaveData entry = Pool.GetFromPoolOrCreate();
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
            this.TimeForWave = 0;
            this.ShipsInWave.Clear();
        }

        #region CopyFrom
        public void CopyFrom( WormholeWaveData other )
        {
            TimeForWave = other.TimeForWave;
            ShipsInWave.Clear();
            foreach ( KeyValuePair<GameEntityTypeData,int> kv in other.ShipsInWave )
                this.ShipsInWave[kv.Key] = kv.Value;
        }
        #endregion

        #region Ser / Deser
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, TimeForWave, "TimeForWave" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ShipsInWave.Count, "ShipsInWave.Count" );
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in ShipsInWave )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, kv.Key, Buffer, "ShipsInWave" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value, "ShipsInWave.Number" );
            } ;
        }
        public void DeserializeFrom( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            TimeForWave = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimeForWave" );
            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ShipsInWave.Count" );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "ShipsInWave" );
                ShipsInWave[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "ShipsInWave.Number" );
            }
        }
        #endregion
    }
}
