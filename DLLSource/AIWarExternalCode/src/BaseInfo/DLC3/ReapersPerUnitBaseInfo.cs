using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ReapersPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int SecondsTillRavage;
        public int SecondsTillTroopSpawn;
        public int CuendillarRemaining;
        public int ChrysalisHatchTime;
        public int GatewayNextReinforcementTime;
        public int GatewayNextMarkupTime;
        public int GatewayNextLarvaTime;
        //Not Serialized

        public ReapersPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            SecondsTillRavage = -1;
            SecondsTillTroopSpawn = -1;
            CuendillarRemaining = -1;
            ChrysalisHatchTime = -1;
            GatewayNextReinforcementTime = -1;
            GatewayNextMarkupTime = -1;
            GatewayNextLarvaTime = -1;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ReapersPerUnitBaseInfo target = CopyTarget as ReapersPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.SecondsTillRavage = this.SecondsTillRavage;
            target.SecondsTillTroopSpawn = this.SecondsTillTroopSpawn;
            target.CuendillarRemaining = this.CuendillarRemaining;
            target.ChrysalisHatchTime = this.ChrysalisHatchTime;
            target.GatewayNextReinforcementTime = this.GatewayNextReinforcementTime;
            target.GatewayNextMarkupTime = this.GatewayNextMarkupTime;
            target.GatewayNextLarvaTime = this.GatewayNextLarvaTime;
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Reapers PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, SecondsTillRavage, "SecondsTillRavage" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, SecondsTillTroopSpawn, "SecondsTillTroopSpawn" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, CuendillarRemaining, "CuendillarRemaining" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ChrysalisHatchTime, "ChrysalisHatchTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GatewayNextReinforcementTime, "GatewayNextReinforcementTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GatewayNextMarkupTime, "GatewayNextMarkupTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, GatewayNextLarvaTime, "GatewayNextLarvaTime" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Reapers PerUnit Data" );
            SecondsTillRavage = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SecondsTillRavage" );
            SecondsTillTroopSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SecondsTillTroopSpawn" );
            CuendillarRemaining = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "CuendillarRemaining" );
            ChrysalisHatchTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ChrysalisHatchTime" );
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 546))
            {
                GatewayNextReinforcementTime = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "GatewayNextReinforcementTime");
                GatewayNextMarkupTime = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "GatewayNextMarkupTime");
                GatewayNextLarvaTime = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "GatewayNextLarvaTime");
            }
        }
    }
}
