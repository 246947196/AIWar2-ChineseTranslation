using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MacrophagePerSporeBaseInfo : ExternalSquadBaseInfo
    {
        public int TeliumID;
        public int SpawnTime;
        public MacrophagePerSporeBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.TeliumID = -1;
            this.SpawnTime = -1;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            MacrophagePerSporeBaseInfo target = CopyTarget as MacrophagePerSporeBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.TeliumID = this.TeliumID;
            target.SpawnTime = this.SpawnTime;
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
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TeliumID );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.SpawnTime );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TeliumID = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.SpawnTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }
    }
}
