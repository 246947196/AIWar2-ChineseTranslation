using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class SappersPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //Used for sappers, but also for things they can pick resources up from
        public int MetalStored;
        public int BloodstoneStored;
        public int MoonstoneStored;

        public int TotalResourcesSpent;

        //when this crystal will flower
        public int FloweringTime;

        //if this sapper (or constructor) is going to build something
        public GameEntityTypeData UnitToBuild;
        public int PlanetIdx;
        public ArcenPoint LocationToBuild;

        //For Beachheaders, track when we last sent off a turret
        public int TimeLastMadeConstructor;
        public bool IsBeachheadTurret;

        //For Watchtowers, the ships inside ready to go helper
        public readonly Dictionary<GameEntityTypeData, int> ShipsInside = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 60, "SappersPerUnitBaseInfo-ShipsInside" );
        public int HomeWatchtowerId;

        //For Sappers only. Every so often a sapper decides it must save up for something,
        //since otherwise they would unhealthily prioritize their cheap combat structures.
        //This basically says "The next non-crystal we build must be of this type";
        //the types will generally be "Save up for a Sapper Habitat (only used if we can build more)"
        //or "Save up for a powerful defensive/offesnive structure".
        //We can't set the UnitToBuild yet because this sapper probably needs to build crystals.
        public string TagForUnitToBuildNext;

        //if this sapper is going to collect resources
        public LazyLoadSquadWrapper DestinationForSappers;
        //Not Serialized
        public Planet PlanetToBuildOn;
        public GameEntity_Squad HomeWatchtower;
        public string ShipsInside_ForUI; //a string representation of the ShipsInside that's UI safe
        public Planet PlanetWatchtowerWantsToHelp;

        public SappersPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            MetalStored = 0;
            BloodstoneStored = 0;
            MoonstoneStored = 0;

            this.TotalResourcesSpent = 0;

            FloweringTime = 0;

            this.UnitToBuild = null;
            this.PlanetIdx = -1;
            this.LocationToBuild = ArcenPoint.ZeroZeroPoint;

            this.TimeLastMadeConstructor = -1;
            this.IsBeachheadTurret = false;

            this.ShipsInside.Clear();
            this.HomeWatchtowerId = 0;

            this.TagForUnitToBuildNext = string.Empty;
            this.DestinationForSappers.Clear();

            this.PlanetToBuildOn = null;
            this.HomeWatchtower = null;

            this.ShipsInside_ForUI = string.Empty;
            this.PlanetWatchtowerWantsToHelp = null;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            SappersPerUnitBaseInfo target = CopyTarget as SappersPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.MetalStored = this.MetalStored;
            target.BloodstoneStored = this.BloodstoneStored;
            target.MoonstoneStored = this.MoonstoneStored;

            target.TotalResourcesSpent = this.TotalResourcesSpent;

            target.FloweringTime = this.FloweringTime;

            target.UnitToBuild = this.UnitToBuild;
            target.PlanetIdx = this.PlanetIdx;
            target.LocationToBuild = this.LocationToBuild;

            target.TimeLastMadeConstructor = this.TimeLastMadeConstructor;
            target.IsBeachheadTurret = this.IsBeachheadTurret;

            target.ShipsInside.ClearAndCopyFrom( this.ShipsInside );
            target.HomeWatchtowerId = this.HomeWatchtowerId;

            target.TagForUnitToBuildNext = this.TagForUnitToBuildNext;
            target.DestinationForSappers = this.DestinationForSappers.CreateCopy();

            target.PlanetToBuildOn = this.PlanetToBuildOn;
            target.HomeWatchtower = this.HomeWatchtower;

            target.ShipsInside_ForUI = this.ShipsInside_ForUI;
            target.PlanetWatchtowerWantsToHelp = this.PlanetWatchtowerWantsToHelp;

        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            if ( OriginalStackCount <= 0 )
                return;

            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack

            //let each half only have part of the metal. We can use floats because this is only happening on the host anyway.
            float percentageMetalLeft = (float)MyPersonalNewStackCount / (float)OriginalStackCount;
            this.MetalStored = Mathf.RoundToInt( this.MetalStored * percentageMetalLeft );
            this.BloodstoneStored = Mathf.RoundToInt( this.BloodstoneStored * percentageMetalLeft );
            this.MoonstoneStored = Mathf.RoundToInt( this.MoonstoneStored * percentageMetalLeft );

            foreach ( KeyValuePair<GameEntityTypeData, int> kv in this.ShipsInside )
            {
                int newShipsInside = Mathf.RoundToInt( kv.Value * percentageMetalLeft );
                if ( newShipsInside < 1 )
                    newShipsInside = 1;
                this.ShipsInside[kv.Key] = newShipsInside;
            }
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Sapper PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, MetalStored, "MetalStored" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, BloodstoneStored, "BloodstoneStored" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, MoonstoneStored, "MoonstoneStored" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TotalResourcesSpent, "TotalResourcesSpent" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, FloweringTime, "FloweringTime" );

            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.UnitToBuild, Buffer, "UnitToBuild" );
            Buffer.AddArcenPointFromCombatSpace( MetaData, LocationToBuild, "LocationToBuild" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)PlanetIdx, "PlanetIdx" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeLastMadeConstructor, "TimeLastMadeConstructor" );
            Buffer.AddBool( MetaData, this.IsBeachheadTurret, "IsBeachheadTurret" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ShipsInside.Count, "ShipsInside.Count" );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, pair.Key, Buffer, "ShipInside.Type" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, ShipsInside[pair.Key], "ShipInside.Count" );
            }

            Buffer.AddString_Condensed( MetaData, this.TagForUnitToBuildNext, "TagForUnitToBuildNext" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, HomeWatchtowerId, "HomeWatchtowerId" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.DestinationForSappers.GetPrimaryKeyID(), "DestinationForSappers" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Sapper PerUnit Data" );
            MetalStored = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "MetalStored" );
            BloodstoneStored = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "BloodstoneStored" );
            MoonstoneStored = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "MoonstoneStored" );
            TotalResourcesSpent = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TotalResourcesSpent" );
            FloweringTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "FloweringTime" );

            UnitToBuild = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "UnitToBuild" );
            LocationToBuild = Buffer.ReadArcenPointFromCombatSpace( MetaData, "LocationToBuild" );
            PlanetIdx = (int)Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlanetIdx" );

            TimeLastMadeConstructor = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeLastMadeConstructor" );
            IsBeachheadTurret = Buffer.ReadBool( MetaData, "IsBeachheadTurret" );

            ShipsInside.Clear();
            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ShipsInside.Count" );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "ShipInside.Type" );
                ShipsInside[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "ShipInside.Count" );
            }

            this.TagForUnitToBuildNext = Buffer.ReadString_Condensed( MetaData, "TagForUnitToBuildNext" );
            HomeWatchtowerId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeWatchtowerId" );
            this.DestinationForSappers = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DestinationForSappers" ), true, "SapperPerUnitDeser" );
        }

        public int GetStrengthInside( GameEntity_Squad entity )
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
                {
                    GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( entity.CurrentMarkLevel );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * ShipsInside[pair.Key]);
                }
            }
            catch { }
            return strength;
        }
        public static ArcenDoubleCharacterBuffer Buffer_ForShipsInside = new ArcenDoubleCharacterBuffer( "SappersPerUnitBaseInfo-Buffer_ForShipsInside" );
        public void UpdateShipsInside_ForUI( GameEntity_Squad entity )
        {
            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[entity.CurrentMarkLevel];
            int shipsFound = 0;
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
            {
                shipsFound++;
                GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( entity.CurrentMarkLevel );
                int strengthForUnit = (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * this.ShipsInside[pair.Key]);
                if ( this.ShipsInside[pair.Key] > 0 )
                    Buffer_ForShipsInside.Add( pair.Key.GetDisplayName(), "909090" ).Add( " x" + this.ShipsInside[pair.Key].ToString(), markByOrdinal.ColorHex ).Add( ", " );
            }
            if ( shipsFound == 0 )
                Buffer_ForShipsInside.Add( "无舰船" );
            this.ShipsInside_ForUI = Buffer_ForShipsInside.GetStringAndResetForNextUpdate();
        }
    }
}
