using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainsPerDepotBaseInfo : ExternalSquadBaseInfo
    {
        //On initialization, we look up
        //the AstroTrainBehaviorType in the AstroTrainBehaviorTypeTable
        //using the depotName
        public int LastTrainSpawnTime;
        public int TrainsThatArrivedSafely;
        public int TrainsSpawned;
        public int DepotTrainBehaviorID;
        public AstroTrainBehaviorType data;
        public AstroTrainsPerDepotBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.LastTrainSpawnTime = -1;
            this.TrainsThatArrivedSafely = 0;
            this.TrainsSpawned = 0;
            this.DepotTrainBehaviorID = -1;
            this.data = null;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            AstroTrainsPerDepotBaseInfo target = CopyTarget as AstroTrainsPerDepotBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.LastTrainSpawnTime = this.LastTrainSpawnTime;
            target.TrainsThatArrivedSafely = this.TrainsThatArrivedSafely;
            target.TrainsSpawned = this.TrainsSpawned;
            target.DepotTrainBehaviorID = this.DepotTrainBehaviorID;
            target.data = this.data;
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            //nothing seems to be needed here
            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack
        }

        public override void DoAfterSingleOtherShipMergedIntoOurStack( ExternalSquadBaseInfo OtherShipBeingDiscarded )
        {
            //the OtherShipBeingDiscarded is being discarded.  If there's any data we want to merge into this
            //(this being a part of the stack that is kept), then we can do it now.
            //if there are 10 squads being merged into one stack, this would be called 9 times on the
            //single squad that is remaining, with the parameter being the other 9 that are disappearing.
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TrainsSpawned, "TrainsSpawned" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LastTrainSpawnTime, "LastTrainSpawnTime" );
            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.F_ˉ1_To_255, this.DepotTrainBehaviorID, "DepotTrainBehaviorID" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TrainsThatArrivedSafely, "TrainsThatArrivedSafely" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TrainsSpawned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TrainsSpawned" );
            this.LastTrainSpawnTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "LastTrainSpawnTime" );
            this.DepotTrainBehaviorID = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.F_ˉ1_To_255, "DepotTrainBehaviorID" );
            this.TrainsThatArrivedSafely = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TrainsThatArrivedSafely" );

            if ( this.data != null && this.data.id == this.DepotTrainBehaviorID )
            { } //hooray, we had a match!
            else //otherwise load if we can
            {
                this.data = null;
                if ( this.DepotTrainBehaviorID != -1 )
                    this.data = AstroTrainBehaviorTypeTable.Instance.GetRowById( DepotTrainBehaviorID );
            }
        }

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "Depot: " ).Add( this.data.name ).Add( " TrainsNeededBeforeFiring <" ).Add( this.data.TrainsNeededBeforeFiring )
                .Add( "> TrainsToSendBeforeFiring <" ).Add( this.data.TrainsToSendBeforeFiring ).Add( "> TrainsSpawned <" ).Add( this.TrainsSpawned )
                .Add( "> FiresOnEveryTrain <" ).Add( this.data.FiresOnEveryTrain ).Add( ">" );
        }
    }
}
