using Arcen.AIW2.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class NeinzulCustodiansPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int Budget;

        public NeinzulCustodiansPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Budget = 0;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            NeinzulCustodiansPerUnitBaseInfo target = CopyTarget as NeinzulCustodiansPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.Budget = this.Budget;
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            if ( OriginalStackCount <= 0 )
                return;

            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack

            //let each half only have part of the budget. We can use floats because this is only  happening on the host anyway.
            float percentageMetalLeft = (float)MyPersonalNewStackCount / (float)OriginalStackCount;
            this.Budget = Mathf.RoundToInt( this.Budget * percentageMetalLeft );
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
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, Budget, "Budget" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Budget = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Budget" );
        }
    }
}
