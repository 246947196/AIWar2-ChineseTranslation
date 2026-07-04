using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainsPerTrainBaseInfo : ExternalSquadBaseInfo
    {
        //serialized
        public Int16 TargetDepotPlanetID;
        public int stationsRemainingBeforeDepot;
        public bool hasGoneNearPlayer;
        public bool hasGottenToStation;
        public int DepotDataTableIndex;//this is the AstroTrainBehaviorType for the depot in question. We need it for notifications
        public int GuardMetal; //at Stations, a train can get/spend metal for more guards

        //nonserialized
        public readonly DoubleBufferedList<SafeSquadWrapper> DeployedGuardsOfThisTrain = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "SingleAstroTrain-DeployedGuardsOfThisTrain" ); //filled from the faction BaseInfo

        //TEACHING_MOMENT: This code, as previously written, was an Dictionary.  To my knowledge, it had never had an exception,
        //but it was only a matter of time.  At some point, one thread would be writing to StoredGuardsInsideThisTrain at the same time as
        //another was reading from it or iterating over it, and we'd get a bug report about that cross-threading exception.
        //However, this isn't some sort of data that we can double-buffer.  This isn't collected-data-over-time.  This is... "real data."
        //Because of the way it is accessed by the UI and LRP and the main sim thread (wow), we REALLY need a threadsafe way to store and access that data.
        //Therefor, this has been converted to the ConcurrentDictionary, from the System.Collections.Concurrent namespace.  Inevitable future problem solved.
        public readonly ConcurrentDictionary<GameEntityTypeData, int> StoredGuardsInsideThisTrain = 
            ConcurrentDictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( Engine_Universal.DEFAULT_CONCURRENCY_LEVEL, 90, "AstroTrainsPerTrainBaseInfo-StoredGuardsInsideThisTrain" ); //these are guards that don't actually exist!

        public AstroTrainsPerTrainBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.TargetDepotPlanetID = -1;
            this.stationsRemainingBeforeDepot = -1;
            this.hasGoneNearPlayer = false;
            this.hasGottenToStation = false;
            this.DepotDataTableIndex = -1;
            this.GuardMetal = 0;

            DeployedGuardsOfThisTrain.Clear();
            StoredGuardsInsideThisTrain.Clear();
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            AstroTrainsPerTrainBaseInfo target = CopyTarget as AstroTrainsPerTrainBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.TargetDepotPlanetID = this.TargetDepotPlanetID;
            target.stationsRemainingBeforeDepot = this.stationsRemainingBeforeDepot;
            target.hasGoneNearPlayer = this.hasGoneNearPlayer;
            target.hasGottenToStation = this.hasGottenToStation;
            target.DepotDataTableIndex = this.DepotDataTableIndex;
            target.GuardMetal = this.GuardMetal;

            //DeployedGuardsOfThisTrain is specific to the original

            //StoredGuardsInsideThisTrain also seems like we can leave it, we don't want double of it for sure
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
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.TargetDepotPlanetID, "TargetDepotPlanetID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.stationsRemainingBeforeDepot, "stationsRemainingBeforeDepot" );
            Buffer.AddBool( MetaData, this.hasGoneNearPlayer, "hasGoneNearPlayer" );
            Buffer.AddBool( MetaData, this.hasGottenToStation, "hasGottenToStation" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.DepotDataTableIndex, "DepotDataTableIndex" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.GuardMetal );


            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.D_0_To_63_Def0, this.StoredGuardsInsideThisTrain.Count, "StoredGuardsInsideThisTrain.Count" );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in this.StoredGuardsInsideThisTrain )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, pair.Key, Buffer, "StoredGuardsInsideThisTrain" );
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511_Def0, pair.Value, "NumberOfStoredGuardsOfThisType" );
            }
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TargetDepotPlanetID = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "TargetDepotPlanetID" );
            this.stationsRemainingBeforeDepot = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "stationsRemainingBeforeDepot" );
            this.hasGoneNearPlayer = Buffer.ReadBool( MetaData, "hasGoneNearPlayer" );
            this.hasGottenToStation = Buffer.ReadBool( MetaData, "hasGottenToStation" );
            this.DepotDataTableIndex = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DepotDataTableIndex" );
            this.GuardMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );

            this.StoredGuardsInsideThisTrain.Clear();
            int numGuardTypesStoredInside = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.D_0_To_63_Def0, "StoredGuardsInsideThisTrain.Count" );
            for ( int i = 0; i < numGuardTypesStoredInside; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "StoredGuardsInsideThisTrain" );
                this.StoredGuardsInsideThisTrain[data] = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511_Def0, "NumberOfStoredGuardsOfThisType" );
            }
        }
    }
}
