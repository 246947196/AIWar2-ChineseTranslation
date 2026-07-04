using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class ZenithArchitravePerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //This is mostly for Spawners, which have
        //A. a "Strength of units can I hold"
        //B. a Max strength to build during War Footing
        //C. a set of ships inside it, ready to come out and fight

        //Nota Bene: Strength is calculated over the whole faction faction (add all the spawners)
        //and we won't keep track of which spawner spawns which ships

        public readonly Dictionary<GameEntityTypeData, int> ShipsInside = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 60, "ZenithArchitravePerUnitBaseInfo-ShipsInside" );
        public byte MarkLevelForShips;

        public string TagForShips; //the set of ships the ZA is allowed to make from a spawner (based on the mark level of the sapwner)
        public string TagForShipsIncludingSpire; //we use this tag if the ZA has gotten a Spire Relic

        public int TimeLastBuiltDefensiveStructure;

        //not serialized
        public string ShipsInside_ForUI; //a string representation of the ShipsInside that's UI safe
        public ZenithArchitravePerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            ShipsInside.Clear();
            MarkLevelForShips = 1;

            TagForShips = "ArchitraveTierZero";
            TagForShipsIncludingSpire = "ArchitraveTierZeroWithSpire";
            TimeLastBuiltDefensiveStructure = -1;
            ShipsInside_ForUI = string.Empty;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ZenithArchitravePerUnitBaseInfo target = CopyTarget as ZenithArchitravePerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }

            target.ShipsInside.Clear();
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in this.ShipsInside )
                target.ShipsInside[kv.Key] = kv.Value;

            target.MarkLevelForShips = this.MarkLevelForShips;

            target.TagForShips = this.TagForShips;
            target.TagForShipsIncludingSpire = this.TagForShipsIncludingSpire;

            target.ShipsInside_ForUI = this.ShipsInside_ForUI;
            target.TimeLastBuiltDefensiveStructure = this.TimeLastBuiltDefensiveStructure;
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            if ( OriginalStackCount <= 0 )
                return;

            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack

            //let each half only have part of the ships stored. We can use floats because this is only happening on the host anyway.
            float percentageMetalLeft = (float)MyPersonalNewStackCount / (float)OriginalStackCount;
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

        #region Ser / Deser
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ShipsInside.Count, "ShipsInside.Count" );
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in ShipsInside )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, kv.Key, Buffer, "ZenithArchitraveStorage" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value, "ZenithArchitraveStorage.Number" );
            }
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, MarkLevelForShips, "MarkLevelForShips" );
            Buffer.AddString_Condensed( MetaData, TagForShips, "TagForShips" );
            Buffer.AddString_Condensed( MetaData, TagForShipsIncludingSpire, "TagForShipsIncludingSpire" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeLastBuiltDefensiveStructure, "TimeLastBuiltDefensiveStructure" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            ShipsInside.Clear();
            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ShipsInside.Count" );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "ZenithArchitraveStorage" );
                ShipsInside[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "ZenithArchitraveStorage.Number" );
            }
            MarkLevelForShips = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "MarkLevelForShips" );
            TagForShips = Buffer.ReadString_Condensed( MetaData, "TagForShips" );
            TagForShipsIncludingSpire = Buffer.ReadString_Condensed( MetaData, "TagForShipsIncludingSpire" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 009 ) )
                TimeLastBuiltDefensiveStructure = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeLastBuiltDefensiveStructure" );
        }
        #endregion

        public int GetStrengthInside()
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> kv in ShipsInside )
                {
                    GameEntityTypeData.MarkLevelStats markLevelStats = kv.Key.MarkStatsFor( this.MarkLevelForShips );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * kv.Value);
                }
            }
            catch { } //this is used in the UI code and can race
            return strength;
        }
    }
}
