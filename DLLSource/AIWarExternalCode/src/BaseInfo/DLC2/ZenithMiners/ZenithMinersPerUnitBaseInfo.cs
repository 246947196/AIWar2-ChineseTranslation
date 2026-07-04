using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum ZenithMinerEffect : byte
    {
        DestroyPlanet,
        SlowShipsOnPlanet,
        SpeedupShipsOnPlanet,
        MakePlanetNomadic,
        DestroyDysonSphere,
        DiminishZenithArchitrave,
        RavagePlanet,
    };

    public class ZenithMinersPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int RemainingDuration; //for both probe and miner
        public ZenithMinerEffect Effect;
        public bool InMiningMode;

        //Not Serialized
        public bool WasEffectDone; //for miner only; checked in the DoOnAnyDeath code. Not serialized because it should be set and checked in the same sim-step
        public ZenithMinersPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            RemainingDuration = -1;
            Effect = ZenithMinerEffect.DestroyPlanet;
            InMiningMode = false;

            WasEffectDone = false;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ZenithMinersPerUnitBaseInfo target = CopyTarget as ZenithMinersPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.RemainingDuration = this.RemainingDuration;
            target.Effect = this.Effect;
            target.InMiningMode = this.InMiningMode;

            target.WasEffectDone = this.WasEffectDone;
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
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RemainingDuration );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.Effect );
            Buffer.AddBool( MetaData, this.InMiningMode );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            RemainingDuration = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            Effect = (ZenithMinerEffect)Buffer.ReadByte( MetaData, ReadStyleByte.Normal );
            InMiningMode = Buffer.ReadBool( MetaData );
        }
    }
}
