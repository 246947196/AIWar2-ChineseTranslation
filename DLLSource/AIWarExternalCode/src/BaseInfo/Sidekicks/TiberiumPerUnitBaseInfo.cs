using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public enum TiberiumUpgrade
    {
        None,
        MarkUp,
        NewVein,
        BoostNextCPA,
        BuildDefenses,
        SpawnTierOneShip,
        SpawnTierTwoShip,
        SpawnDireShip,
        SpawnDefense,
        SpawnSummoner
    }
    public static class TiberiumUpgradeExtensions
    {
        public static string ToFriendlyString(this TiberiumUpgrade me)
        {
            switch(me)
            {
                case TiberiumUpgrade.None:
                    return "Unset";
                case TiberiumUpgrade.MarkUp:
                    return "Mark Up";
                case TiberiumUpgrade.NewVein:
                    return "Spawn New Vein";
                case TiberiumUpgrade.SpawnTierOneShip:
                    return "Spawn Corrupted AI Ship";
                case TiberiumUpgrade.SpawnTierTwoShip:
                    return "Spawn Powerful Corrupted AI Ship";
                case TiberiumUpgrade.SpawnDefense:
                    return "Spawn Turret";
                case TiberiumUpgrade.SpawnSummoner:
                    return "Spawn Summoner";
                case TiberiumUpgrade.BoostNextCPA:
                    return "Boost Next CPA";

                default:
                    return "Confusion";
            }
        }
    }
    public class TiberiumPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //For Veins
        public int Points;
        public TiberiumUpgrade NextUpgrade;
        public TiberiumUpgrade PreferredUpgrade;
        public int AutoDefenseBuildPoints;
        //For Transports
        public LazyLoadSquadWrapper HomeVein;
        public SafeSquadWrapper HomeVeinSafe;

        //For Summoners
        public int TimeForNextSpawn;
        //Not Serialized
        public TiberiumPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Points = -1;
            AutoDefenseBuildPoints = -1;
            TimeForNextSpawn = -1;
            NextUpgrade = TiberiumUpgrade.None;
            PreferredUpgrade = TiberiumUpgrade.None;
            HomeVein.Clear();
            HomeVeinSafe.Clear();

        }
        public override void CopyTo(ExternalSquadBaseInfo CopyTarget)
        {
            if ( CopyTarget == null )
                return;
            TiberiumPerUnitBaseInfo target = CopyTarget as TiberiumPerUnitBaseInfo;
            target.Points = this.Points;
            target.AutoDefenseBuildPoints = this.AutoDefenseBuildPoints;
            target.TimeForNextSpawn = this.TimeForNextSpawn;
            target.NextUpgrade = this.NextUpgrade;
            target.PreferredUpgrade = this.PreferredUpgrade;
            target.HomeVein = this.HomeVein.CreateCopy();
            target.HomeVeinSafe = this.HomeVeinSafe.CreateCopy();
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Tiberium PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, Points, "Points" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, AutoDefenseBuildPoints, "AutoDefenseBuildPoints" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, (int)NextUpgrade, "NextUpgrade" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, (int)PreferredUpgrade, "PreferredUpgrade" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeVein.GetPrimaryKeyID(), "HomeVein.PrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForNextSpawn, "TimeForNextSpawn" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Tiberium PerUnit Data" );
            Points = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Points" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 718 ) )
                AutoDefenseBuildPoints = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "AutoDefenseBuildPoints" );
            NextUpgrade = (TiberiumUpgrade)Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "NextUpgrade" );
            //if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 548 ) )
            {
                PreferredUpgrade = (TiberiumUpgrade)Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "PreferredUpgrade" );
            }
            this.HomeVein = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeVein.PrimaryKeyID" ), true, "TiberiumHomeStr" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 506 ) )
            {
                TimeForNextSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextSpawn" );
            }
        }
    }
}
