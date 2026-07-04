using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ArmadaPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //For Mines
        public int MineFinishTime;
        //For Producers
        public int ProducerNextTransportTime;
        //For Transports
        public int MetalTransported;
        public int HackingTransported;
        public int ScienceTransported;
        public int TiberiumTransported;

        public int UnitsKilled;

        public int TimeForNextSwarmSummon;
        public Planet LocustDestination;

        public int RangerMetal;
        public int DireRangerMetal;
        public LazyLoadSquadWrapper HomeStarbase;
        public SafeSquadWrapper HomeStarbaseSafe;

        //Not Serialized
        public int RangerCap;
        public int DireRangerCap;
        public ArmadaPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            MineFinishTime = -1;
            ProducerNextTransportTime = -1;
            MetalTransported = -1;
            HackingTransported = -1;
            ScienceTransported = -1;
            TiberiumTransported = -1;

            UnitsKilled = 0;

            TimeForNextSwarmSummon = 0;
            LocustDestination = null;

            HomeStarbase.Clear();
            HomeStarbaseSafe.Clear();

            RangerMetal = 0;
            DireRangerMetal = 0;

            RangerCap = -1;
            DireRangerCap = -1;
        }
        public override void CopyTo(ExternalSquadBaseInfo CopyTarget)
        {
            if ( CopyTarget == null )
                return;
            ArmadaPerUnitBaseInfo target = CopyTarget as ArmadaPerUnitBaseInfo;
            target.MineFinishTime = this.MineFinishTime;
            target.ProducerNextTransportTime = this.ProducerNextTransportTime;
            target.MetalTransported = this.MetalTransported;
            target.HackingTransported = this.HackingTransported;
            target.ScienceTransported = this.ScienceTransported;
            target.TiberiumTransported = this.TiberiumTransported;

            target.UnitsKilled = this.UnitsKilled;

            target.TimeForNextSwarmSummon = this.TimeForNextSwarmSummon;
            target.LocustDestination = this.LocustDestination;
            
            target.RangerMetal = this.RangerMetal;
            target.DireRangerMetal = this.DireRangerMetal;
            target.HomeStarbase = this.HomeStarbase.CreateCopy();
            target.HomeStarbaseSafe = this.HomeStarbaseSafe.CreateCopy();
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Armada PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, MineFinishTime, "MineFinishTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, MetalTransported, "MetalTransported" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, HackingTransported, "HapTransported" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ScienceTransported, "ScienceTransported" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TiberiumTransported, "TiberiumTransported" );

            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, UnitsKilled, "UnitsKilled" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, TimeForNextSwarmSummon, "TimeForNextSwarmSummon" );
            if ( LocustDestination == null )
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, -1, "LocustDestination");
            else
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.LocustDestination.Index, "LocustDestination" );


            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RangerMetal, "RangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DireRangerMetal, "DireRangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeStarbase.GetPrimaryKeyID(), "HomeStarbase.PrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ProducerNextTransportTime, "ProducerNextTransportTime" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Armada PerUnit Data" );
            MineFinishTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "MineFinishTime" );
            MetalTransported = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "MetalTransported" );
            HackingTransported = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HackingTransported" );
            ScienceTransported = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ScienceTransported" );
            TiberiumTransported = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TiberiumTransported" );
            // if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 520 ) )
            //     DrillingChrysalis = Buffer.ReadBool( MetaData, "DrillingChrysalis" );

            UnitsKilled = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "UnitsKilled" );
            TimeForNextSwarmSummon = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimeForNextSwarmSummon" );
            this.LocustDestination = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "LocustDestination" ) );

//            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 548 ) )
            {
                RangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RangerMetal" );
                DireRangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DireRangerMetal" );
            }
            this.HomeStarbase = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeStarbase.PrimaryKeyID" ), true, "ArmadaHomeStr" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 720 ) )
                this.ProducerNextTransportTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ProducerNextTransportTime" );
        }
    }
}
