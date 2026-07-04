using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class InstigatorPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int TimeForNextEffect;
        public int InstigatorEffectIndex;
        public bool UnitGoesForHumanKing;
        public int CumulativeEffectSoFar;
        public int NumTimesEffectHappened;
        public InstigatorPerUnitBaseInfo()
        {
        }

        protected override void Cleanup()
        {
            TimeForNextEffect = 0;
            InstigatorEffectIndex = -1;
            CumulativeEffectSoFar = -1;
            UnitGoesForHumanKing = false;
            NumTimesEffectHappened = 0;
        }
        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            InstigatorPerUnitBaseInfo target = CopyTarget as InstigatorPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.TimeForNextEffect = this.TimeForNextEffect;
            target.InstigatorEffectIndex = this.InstigatorEffectIndex;
            target.CumulativeEffectSoFar = this.CumulativeEffectSoFar;
            target.UnitGoesForHumanKing = this.UnitGoesForHumanKing;
            target.NumTimesEffectHappened = this.NumTimesEffectHappened;
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
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimeForNextEffect, "TimeForNextEffect" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.InstigatorEffectIndex, "InstigatorEffectIndex" );
            Buffer.AddBool( MetaData, this.UnitGoesForHumanKing, "UnitGoesForHumanKing" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.CumulativeEffectSoFar, "CumulativeEffectSoFar" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NumTimesEffectHappened, "NumTimesEffectHappened" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TimeForNextEffect = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimeForNextEffect" );
            this.InstigatorEffectIndex = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "InstigatorEffectIndex" );
            this.UnitGoesForHumanKing = Buffer.ReadBool( MetaData, "UnitGoesForHumanKing" );
            this.CumulativeEffectSoFar = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "CumulativeEffectSoFar" );
            this.NumTimesEffectHappened = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "NumTimesEffectHappened" );
        }
    }
}
