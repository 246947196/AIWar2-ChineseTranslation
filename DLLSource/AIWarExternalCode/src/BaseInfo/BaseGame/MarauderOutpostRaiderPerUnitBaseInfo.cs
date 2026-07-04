using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //this is set on both Marauder Raiders or Marauder Outposts
    public class MarauderOutpostRaiderPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //These three values are available if isRaider == false
        public int LastRaiderSummonTime = 0;
        public int MaxRaiders = 0;

        public int OutpostId = -1; //this value is only set if isRaider is true
        public bool isHumanAligned = false;

        public MarauderOutpostRaiderPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            LastRaiderSummonTime = 0;
            MaxRaiders = 0;
            OutpostId = -1;
            isHumanAligned = false;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            MarauderOutpostRaiderPerUnitBaseInfo target = CopyTarget as MarauderOutpostRaiderPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.LastRaiderSummonTime = this.LastRaiderSummonTime;
            target.MaxRaiders = this.MaxRaiders;
            target.OutpostId = this.OutpostId;
            target.isHumanAligned = this.isHumanAligned;
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
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, LastRaiderSummonTime, "LastRaiderSummonTime" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, MaxRaiders, "MaxRaiders" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, OutpostId, "OutpostId" );
            Buffer.AddBool( MetaData, isHumanAligned, "isHumanAligned" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            LastRaiderSummonTime = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "LastRaiderSummonTime" );
            MaxRaiders = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "MaxRaiders" );
            OutpostId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "OutpostId" );
            isHumanAligned = Buffer.ReadBool( MetaData, "isHumanAligned" );
        }
    }
}
