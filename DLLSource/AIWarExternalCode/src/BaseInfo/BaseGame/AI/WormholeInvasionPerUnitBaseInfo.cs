using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeInvasionPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //This is for the Exogalactic Wormhole style wormhole invasions (aka old-style)
        public int RemainingTimeForWormholeInvasion;
        public int WormholeInvasionRemainingAttacksToLaunch;
        public int WormholeInvasionAttackInterval;
        public int AIPurchaseCostPerAttack;
        public int TimeUntilNextAttack;

        //For planet linking wormhole invasions, these go on the projectors
        public int LinkedPlanetIdx;
        public int TimeToRemoveUnit;

        public WormholeInvasionPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            //This is for the Exogalactic Wormhole style wormhole invasions
            this.RemainingTimeForWormholeInvasion = -1;
            this.WormholeInvasionRemainingAttacksToLaunch = -1;
            this.WormholeInvasionAttackInterval = -1;
            this.AIPurchaseCostPerAttack = -1;
            this.TimeUntilNextAttack = -1;
            this.LinkedPlanetIdx = -1;
            this.TimeToRemoveUnit = -1;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            WormholeInvasionPerUnitBaseInfo target = CopyTarget as WormholeInvasionPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.RemainingTimeForWormholeInvasion = this.RemainingTimeForWormholeInvasion;
            target.WormholeInvasionRemainingAttacksToLaunch = this.WormholeInvasionRemainingAttacksToLaunch;
            target.WormholeInvasionAttackInterval = this.WormholeInvasionAttackInterval;
            target.AIPurchaseCostPerAttack = this.AIPurchaseCostPerAttack;
            target.TimeUntilNextAttack = this.TimeUntilNextAttack;
            target.LinkedPlanetIdx = this.LinkedPlanetIdx;
            target.TimeToRemoveUnit = this.TimeToRemoveUnit;
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
            //This is for the Exogalactic Wormhole style wormhole invasions
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RemainingTimeForWormholeInvasion, "RemainingTimeForWormholeInvasion" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, WormholeInvasionRemainingAttacksToLaunch, "WormholeInvasionRemainingAttacksToLaunch" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, WormholeInvasionAttackInterval );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, AIPurchaseCostPerAttack );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeUntilNextAttack );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, LinkedPlanetIdx );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeToRemoveUnit );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            //This is for the Exogalactic Wormhole style wormhole invasions
            RemainingTimeForWormholeInvasion = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RemainingTimeForWormholeInvasion" );
            WormholeInvasionRemainingAttacksToLaunch = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "WormholeInvasionRemainingAttacksToLaunch" );
            WormholeInvasionAttackInterval = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            AIPurchaseCostPerAttack = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            TimeUntilNextAttack = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            LinkedPlanetIdx = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            TimeToRemoveUnit = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }
    }
}
