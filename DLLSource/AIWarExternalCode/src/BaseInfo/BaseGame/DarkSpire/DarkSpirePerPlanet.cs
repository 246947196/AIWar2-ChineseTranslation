using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkSpirePerPlanet : ConcurrentPoolable<DarkSpirePerPlanet>, IProtectedListable
    {
        public FInt NetEnergy; //the amount of energy this planet has right now
        public FInt TotalEnergy; //the total amount of energy earned so far
        public int ConversionRatio; //how efficiently this planet turns energy into ships.
                                    //If the ratio is > 100 then we start generating more ships than there was energy
        public FInt EnergyThresholdForAttack = FInt.Zero; //energy must be above this threshold to attack
        public int TimeToAwaken; //Sometimes we might want enforced idleness
        public bool hasBeenInitialized;
        public bool NextAttackMustSpawnUnits;
        public bool VGGeneratesEnergy;
        public readonly List<Int16> FactionsWhichHaveDownloadedShipDesign = List<Int16>.Create_WillNeverBeGCed( 20, "DarkSpirePerPlanet-FactionsWhichHaveDownloadedShipDesign" );
        public int numShipDesignsDownloaded;
        public int lastTimeAttemptedLocus;

        public void Cleanup()
        {
            this.NetEnergy = FInt.Zero;
            this.TotalEnergy = FInt.Zero;
            this.ConversionRatio = 0;
            this.EnergyThresholdForAttack = FInt.Zero;
            this.TimeToAwaken = 0;
            this.hasBeenInitialized = false;
            this.NextAttackMustSpawnUnits = false;
            this.VGGeneratesEnergy = false;
            this.numShipDesignsDownloaded = 0;
            this.lastTimeAttemptedLocus = -1;

            if ( this.FactionsWhichHaveDownloadedShipDesign.Count > 0 )
                this.FactionsWhichHaveDownloadedShipDesign.Clear();
        }

        public void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.NetEnergy = Buffer.ReadFInt( MetaData, "NetEnergy" );
            this.TotalEnergy = Buffer.ReadFInt( MetaData, "TotalEnergy" );
            this.ConversionRatio = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "ConversionRatio" );
            this.EnergyThresholdForAttack = Buffer.ReadFInt( MetaData, "EnergyThresholdForAttack" );
            this.TimeToAwaken = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimeToAwaken" );
            this.hasBeenInitialized = Buffer.ReadBool( MetaData, "hasBeenInitialized" );
            this.NextAttackMustSpawnUnits = Buffer.ReadBool( MetaData, "NextAttackMustSpawnUnits" );
            this.VGGeneratesEnergy = Buffer.ReadBool( MetaData, "VGGeneratesEnergy" );
            this.FactionsWhichHaveDownloadedShipDesign.Clear();
            Int16 numFactions = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "FactionsWhichHaveDownloadedShipDesign.Count" );
            for ( int i = 0; i < numFactions; i++ )
                this.FactionsWhichHaveDownloadedShipDesign.Add( Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "FactionsWhichHaveDownloadedShipDesign_Item" ) );
            this.numShipDesignsDownloaded = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "numShipDesignsDownloaded" );
            this.lastTimeAttemptedLocus = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "lastTimeAttemptedLocus" );
        }
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFInt( MetaData, this.NetEnergy, "NetEnergy" );
            Buffer.AddFInt( MetaData, this.TotalEnergy, "TotalEnergy" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.ConversionRatio, "ConversionRatio" );
            Buffer.AddFInt( MetaData, this.EnergyThresholdForAttack, "EnergyThresholdForAttack" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimeToAwaken, "TimeToAwaken" );
            Buffer.AddBool( MetaData, this.hasBeenInitialized, "hasBeenInitialized" );
            Buffer.AddBool( MetaData, this.NextAttackMustSpawnUnits, "NextAttackMustSpawnUnits" );
            Buffer.AddBool( MetaData, this.VGGeneratesEnergy, "VGGeneratesEnergy" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.FactionsWhichHaveDownloadedShipDesign.Count, "FactionsWhichHaveDownloadedShipDesign.Count" );
            for ( int i = 0; i < this.FactionsWhichHaveDownloadedShipDesign.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.FactionsWhichHaveDownloadedShipDesign[i], "FactionsWhichHaveDownloadedShipDesign_Item" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.numShipDesignsDownloaded, "numShipDesignsDownloaded" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.lastTimeAttemptedLocus, "lastTimeAttemptedLocus" );
        }

        #region Pooling
        public static DarkSpirePerPlanet GetFromPoolOrCreate()
        {
            DarkSpirePerPlanet data = Pool.GetFromPoolOrCreate();
            return data;
        }

        private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "DarkSpirePerPlanets" );
        private DarkSpirePerPlanet()
        {
            if ( RefTracker != null ) //it will be null for the two above in the static definitions
                RefTracker.IncrementObjectCount();
            Cleanup();
        }
        private static readonly ConcurrentPool<DarkSpirePerPlanet> Pool = new ConcurrentPool<DarkSpirePerPlanet>( "DarkSpirePerPlanet", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DarkSpirePerPlanet(); } );

        public DarkSpirePerPlanet CreateNewForPool()
        {
            return new DarkSpirePerPlanet();
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            Cleanup();
        }

        //IProtectedDictionaryable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion
    }
}
