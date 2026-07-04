using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This information goes on any units that are set up to guard an astro train as its escort.
    /// They could be any normal ships.
    /// </summary>
    public class AstroTrainsPerTrainGuardUnitBaseInfo : ExternalSquadBaseInfo
    {
        public LazyLoadSquadWrapper TrainIGuard;
        public AstroTrainsPerTrainGuardUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.TrainIGuard.Clear();
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            AstroTrainsPerTrainGuardUnitBaseInfo target = CopyTarget as AstroTrainsPerTrainGuardUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.TrainIGuard = this.TrainIGuard.CreateCopy();
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
            Buffer.AddSquadPrimaryKeyID_PosDef0( MetaData, this.TrainIGuard.GetPrimaryKeyID(), "TrainIGuard" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TrainIGuard = LazyLoadSquadWrapper.Create( Buffer.ReadSquadPrimaryKeyID_PosDef0( MetaData, "TrainIGuard" ), true, "TrainDeser" );
        }
    }
}
