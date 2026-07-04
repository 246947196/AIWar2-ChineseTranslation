using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireSidekickPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //serialized

        //This is for Relics
        public Int16 FactionThatFoundThisRelic; //so the relic remembers where to go
        public int TimeForNextRelicResponse; //to know when to send the next exo
        public Planet DestinationPlanet; //the player chooses where the relic goes (default is player king planet)
        public FInt RelicResponseMultiplier;
        public bool BuildsImmediately;
        public ArcenPoint DestinationPoint;
        public bool MustBuildOnStartPlanet;
        //for debris
        public int TimeUntilDebrisVanishes;
        public Int16 FactionIndexForDebris;

        //for relic trains
        public Int16 HopsLeftForTrain;

        //for spire sidekick specific stuff
        public int RangerMetal;
        public int DireRangerMetal;
        public LazyLoadSquadWrapper HomeCity;
        public SafeSquadWrapper HomeCitySafe;

        //Not Serialized
        public int RangerCap;
        public int DireRangerCap;

        public SpireSidekickPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            FactionThatFoundThisRelic = -1;
            TimeForNextRelicResponse = -1;
            DestinationPlanet = null;
            DestinationPoint = ArcenPoint.ZeroZeroPoint;
            BuildsImmediately = false;
            RelicResponseMultiplier = FInt.One;
            TimeUntilDebrisVanishes = -1;
            HopsLeftForTrain = -1;
            MustBuildOnStartPlanet = false;
            FactionIndexForDebris = -1;

            HomeCity.Clear();
            HomeCitySafe.Clear();

            RangerMetal = 0;
            DireRangerMetal = 0;

            RangerCap = -1;
            DireRangerCap = -1;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            SpireSidekickPerUnitBaseInfo target = CopyTarget as SpireSidekickPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.FactionThatFoundThisRelic = this.FactionThatFoundThisRelic;
            target.TimeForNextRelicResponse = this.TimeForNextRelicResponse;
            target.DestinationPlanet = this.DestinationPlanet;
            target.DestinationPoint = this.DestinationPoint;
            target.BuildsImmediately = this.BuildsImmediately;
            target.RelicResponseMultiplier = this.RelicResponseMultiplier;
            target.TimeUntilDebrisVanishes = this.TimeUntilDebrisVanishes;
            target.HopsLeftForTrain = this.HopsLeftForTrain;
            target.MustBuildOnStartPlanet = this.MustBuildOnStartPlanet;
            target.FactionIndexForDebris = this.FactionIndexForDebris;

            target.RangerMetal = this.RangerMetal;
            target.DireRangerMetal = this.DireRangerMetal;
            target.HomeCity = this.HomeCity.CreateCopy();
            target.HomeCitySafe = this.HomeCitySafe.CreateCopy();

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
            Buffer.AddFactionIndex_Neg1ToPos( MetaData, this.FactionThatFoundThisRelic, "FactionThatFoundThisRelic" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextRelicResponse, "TimeForNextRelicResponse" );
            Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.DestinationPlanet, "DestinationPlanet" );
            Buffer.AddArcenPointFromCombatSpace( MetaData, this.DestinationPoint, "DestinationPoint" );
            Buffer.AddFInt( MetaData, this.RelicResponseMultiplier, "RelicResponseMultiplier" );
            Buffer.AddBool( MetaData, this.BuildsImmediately, "BuildsImmediately" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeUntilDebrisVanishes, "TimeUntilDebrisVanishes" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.HopsLeftForTrain, "HopsLeftForTrain" );
            Buffer.AddBool( MetaData, this.MustBuildOnStartPlanet, "MustBuildOnStartPlanet" );
            Buffer.AddFactionIndex_Neg1ToPos( MetaData, this.FactionIndexForDebris, "FactionIndexForDebris" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RangerMetal, "RangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DireRangerMetal, "DireRangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeCity.GetPrimaryKeyID(), "HomeCity.PrimaryKeyID" );

        }
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.FactionThatFoundThisRelic = Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "FactionThatFoundThisRelic" );
            this.TimeForNextRelicResponse = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextRelicResponse" );
            this.DestinationPlanet = Buffer.ReadPlanetFromIndex_Neg1ToPos( MetaData, "DestinationPlanet" );
            Buffer.FillArcenPointFromCombatSpace( MetaData, out this.DestinationPoint, "DestinationPoint" );
            this.RelicResponseMultiplier = Buffer.ReadFInt( MetaData, "RelicResponseMultiplier" );
            this.BuildsImmediately = Buffer.ReadBool( MetaData, "BuildsImmediately" );
            this.TimeUntilDebrisVanishes = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeUntilDebrisVanishes" );
            this.HopsLeftForTrain = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "HopsLeftForTrain" );
            this.MustBuildOnStartPlanet = Buffer.ReadBool( MetaData, "MustBuildOnStartPlanet" );
            this.FactionIndexForDebris = Buffer.ReadFactionIndex_Neg1ToPos( MetaData, "FactionIndexForDebris" );

            RangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RangerMetal" );
            DireRangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DireRangerMetal" );
            this.HomeCity = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeCity.PrimaryKeyID" ), true, "SpireHomeStr" );

        }
    }
}
