using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
namespace Arcen.AIW2.External
{
    public class RenegadeSpirePerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //Serialized
        // FRACTURE: metal bank that accrues each second; spent to produce combat ships
        public int MetalStored;

        // FRACTURE: game-second when this fracture last marked up (-1 until first HandleFracturesSim tick)
        public int LastMarkupSecond;

        // FRACTURE/DEFILER: the next ship type to produce; null = pick a fresh random TierOne ship next tick
        public GameEntityTypeData NextShipToCreate;

        // DEFILER: metal budget for building guard posts while roaming;
        //          decremented by each structure's CostForAIToPurchase; defiler despawns when this hits 0
        public int DefensiveMetalToSpend;
        // DEFILER: navigation — two movement modes: fly to a relic, or fly to a planet location to build defenses
        public int DestinationId; //if we are going to a Relic, the Relic's ID is here
        public Int16 BuildPlanetIndex; //if we are going to a planet to build a structure
        public ArcenPoint DestinationPoint; //where on the planet to build a structure

        //non-serialized
        public GameEntity_Squad Destination; //from the DestinationId

        public RenegadeSpirePerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            MetalStored = 0;
            LastMarkupSecond = -1;
            NextShipToCreate = null;
            DefensiveMetalToSpend = 0;
            DestinationId = -1;
            Destination = null;
            BuildPlanetIndex = -1;
            DestinationPoint = ArcenPoint.ZeroZeroPoint;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            RenegadeSpirePerUnitBaseInfo target = CopyTarget as RenegadeSpirePerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.MetalStored = this.MetalStored;
            target.LastMarkupSecond = this.LastMarkupSecond;
            target.NextShipToCreate = this.NextShipToCreate;
            target.DefensiveMetalToSpend = this.DefensiveMetalToSpend;
            target.DestinationId = this.DestinationId;
            target.BuildPlanetIndex = this.BuildPlanetIndex;
            target.DestinationPoint = this.DestinationPoint;
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount ) { }

        public override void DoAfterSingleOtherShipMergedIntoOurStack( ExternalSquadBaseInfo OtherShipBeingDiscarded ) { }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, MetalStored );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, LastMarkupSecond );
            bool hasNextShip = NextShipToCreate != null;
            Buffer.AddBool( MetaData, hasNextShip, "HasNextShip" );
            if ( hasNextShip )
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, NextShipToCreate, Buffer, "NextShipToCreate" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, DefensiveMetalToSpend );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DestinationId );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, BuildPlanetIndex );
            Buffer.AddArcenPointFromCombatSpace( MetaData, DestinationPoint, "DestinationPoint" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            MetalStored = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            LastMarkupSecond = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            bool hasNextShip = Buffer.ReadBool( MetaData, "HasNextShip" );
            NextShipToCreate = hasNextShip ? GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "NextShipToCreate" ) : null;
            DefensiveMetalToSpend = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            DestinationId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            BuildPlanetIndex = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            Buffer.FillArcenPointFromCombatSpace( MetaData, out DestinationPoint, "DestinationPoint" );
            Destination = null; // re-resolved by LRP from DestinationId
        }
    }
}
