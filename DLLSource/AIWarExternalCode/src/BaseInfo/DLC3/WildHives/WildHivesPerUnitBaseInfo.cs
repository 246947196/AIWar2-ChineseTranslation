using Arcen.AIW2.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class WildHivesPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        /// <summary>
        /// Used by Workers to claim generators.
        /// Used by Hives to create Workers.
        /// </summary>
        public int PrimaryBuildPoints;
        public int SoldierBuildPoints;

        // Unique to generators to allow them to spit out the correct unit on death.
        public string BaseEntityName;

        // Holds extra information about capturables eaten by a Wild Hive.
        public Dictionary<string, short> FleetLines = Dictionary<string, short>.Create_WillNeverBeGCed( 5, "WildHivesPerUnitBaseInfo-fleetLines", 2 );

        public WildHivesPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            PrimaryBuildPoints = 0;
            SoldierBuildPoints = 0;
            BaseEntityName = "MetalHarvester";
            FleetLines.Clear();
        }

        public void CopyFrom( WildHivesPerUnitBaseInfo HiveToCopyFrom ) //convenience method
        {
            HiveToCopyFrom.CopyTo( this );
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            WildHivesPerUnitBaseInfo target = CopyTarget as WildHivesPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.PrimaryBuildPoints = this.PrimaryBuildPoints;
            target.SoldierBuildPoints = this.SoldierBuildPoints;
            target.BaseEntityName = this.BaseEntityName;

            FleetLines.Clear();
            foreach ( KeyValuePair<string, short> pair in this.FleetLines )
            {
                target.FleetLines.Add( pair.Key, pair.Value );
            }
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            if ( OriginalStackCount <= 0 )
                return;

            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack

            //let each half only have part of the metal. We can use floats because this is only  happening on the host anyway.
            float percentageMetalLeft = (float)MyPersonalNewStackCount / (float)OriginalStackCount;
            this.PrimaryBuildPoints = Mathf.RoundToInt( this.PrimaryBuildPoints * percentageMetalLeft );
            this.SoldierBuildPoints = Mathf.RoundToInt( this.SoldierBuildPoints * percentageMetalLeft );
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
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, PrimaryBuildPoints, "Wild Hives Worker Build Points" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, SoldierBuildPoints, "Wild Hives Soldier Build Points" );
            Buffer.AddString_Condensed( MetaData, BaseEntityName, "Wild Hives Base Generator Name" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)FleetLines.GetPairCount(), "Wild Hives Fleet Lines Pair Count" );
            foreach ( KeyValuePair<string, short> pair in FleetLines )
            {
                Buffer.AddString_Condensed( MetaData, pair.Key, "Wild Hives Fleet Line Internal Name" );
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)pair.Value, "Wild Hives Fleet Line Count" );
            }
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            PrimaryBuildPoints = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Wild Hives Worker Build Points" );
            SoldierBuildPoints = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Wild Hives Soldier Build Points" );
            BaseEntityName = Buffer.ReadString_Condensed( MetaData, "Wild Hives Base Generator Name" );
            int count = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Wild Hives Fleet Lines Pair Count" );
            FleetLines.Clear();
            for ( int x = 0; x < count; x++ )
                FleetLines.Add( Buffer.ReadString_Condensed( MetaData, "Wild Hives Fleet Line Internal Name" ), Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "Wild Hives Fleet Line Count" ) );
        }
    }
}
