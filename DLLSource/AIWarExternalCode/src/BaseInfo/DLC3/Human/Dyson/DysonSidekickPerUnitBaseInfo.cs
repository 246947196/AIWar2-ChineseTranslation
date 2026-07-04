using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class DysonSidekickPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //Note that drilling and overloading are tracked by this variable
        public int TimeTillPlanetDrilled;
        public int TimeTillPlanetOverloaded;
        public int AIPAlreadyGranted;

        public int TimeTillDysonSphereWin;
        public int TimeForNextTransport;

        public bool DrillingChrysalis;

        public int CuendillarTransported; //for the transports
        public int TransportsSent; //for 
        public int TotalCuendillarDrilled;

        public int CuendillarRemaining;
        public int UnitsKilled;

        public int GuardianMetal;
        public int DireGuardianMetal;
        public LazyLoadSquadWrapper HomeStronghold;
        public SafeSquadWrapper HomeStrongholdSafe;

        //Not Serialized
        public int GuardianCap;
        public int DireGuardianCap;
        public DysonSidekickPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeTillPlanetDrilled = -1;
            TimeTillPlanetOverloaded = -1;
            AIPAlreadyGranted = 0;
            TimeTillDysonSphereWin = -1;
            TimeForNextTransport = -1;
            DrillingChrysalis = false;
            CuendillarTransported = -1;
            TransportsSent = 0;
            TotalCuendillarDrilled = -1;
            UnitsKilled = 0;
            HomeStronghold.Clear();
            HomeStrongholdSafe.Clear();

            GuardianMetal = 0;
            DireGuardianMetal = 0;

            GuardianCap = -1;
            DireGuardianCap = -1;
        }
        public override void CopyTo(ExternalSquadBaseInfo CopyTarget)
        {
            if ( CopyTarget == null )
                return;
            DysonSidekickPerUnitBaseInfo target = CopyTarget as DysonSidekickPerUnitBaseInfo;
            target.TimeTillPlanetDrilled = this.TimeTillPlanetDrilled;
            target.TimeTillPlanetOverloaded = this.TimeTillPlanetOverloaded;
            target.AIPAlreadyGranted = this.AIPAlreadyGranted;
            target.TimeTillDysonSphereWin = this.TimeTillDysonSphereWin;
            target.TimeForNextTransport = this.TimeForNextTransport;
            target.DrillingChrysalis = this.DrillingChrysalis;
            target.CuendillarTransported = this.CuendillarTransported;
            target.TransportsSent = this.TransportsSent;
            target.CuendillarRemaining = this.CuendillarRemaining;
            target.UnitsKilled = this.UnitsKilled;
            target.GuardianMetal = this.GuardianMetal;
            target.DireGuardianMetal = this.DireGuardianMetal;
            target.HomeStronghold = this.HomeStronghold.CreateCopy();
            target.HomeStrongholdSafe = this.HomeStrongholdSafe.CreateCopy();
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekick PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeTillPlanetDrilled, "TimeTillPlanetDrilled" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeTillPlanetOverloaded, "TimeTillPlanetOverloaded" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeTillDysonSphereWin, "TimeTillDysonSphereWin" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextTransport, "TimeForNextTransport" );
            Buffer.AddBool( MetaData, DrillingChrysalis, "DrillingChrysalis" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, CuendillarTransported, "CuendillarTransported" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TransportsSent, "TransportsSent" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TotalCuendillarDrilled, "TotalCuendillarDrilled" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, CuendillarRemaining, "CuendillarRemaining" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, UnitsKilled, "UnitsKilled" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GuardianMetal, "GuardianMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DireGuardianMetal, "DireGuardianMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeStronghold.GetPrimaryKeyID(), "HomeStronghold.PrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, AIPAlreadyGranted, "AIPAlreadyGranted" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekick PerUnit Data" );
            TimeTillPlanetDrilled = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeTillPlanetDrilled" );
            TimeTillPlanetOverloaded = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeTillPlanetOverloaded" );
            TimeTillDysonSphereWin = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeTillDysonSphereWin" );
            TimeForNextTransport = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextTransport" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 520 ) )
                DrillingChrysalis = Buffer.ReadBool( MetaData, "DrillingChrysalis" );
            CuendillarTransported = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "CuendillarTransported" );
            TransportsSent = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TransportsSent" );
            TotalCuendillarDrilled = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TotalCuendillarDrilled" );
            CuendillarRemaining = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "CuendillarRemaining" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 528 ) )
                UnitsKilled = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "UnitsKilled" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 548 ) )
            {
                GuardianMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "GuardianMetal" );
                DireGuardianMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DireGuardianMetal" );
            }
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 548 ) )
                this.HomeStronghold = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeStronghold.PrimaryKeyID" ), true, "DysonSidekickHomeStr" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 810 ) )
                AIPAlreadyGranted = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "AIPAlreadyGranted" );
        }
    }
}
