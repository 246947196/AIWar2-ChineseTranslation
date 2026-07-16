using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class TemplarPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //Serialized
        //this goes on units so they know if they should retreat to a castle
        public bool DefenseMode;
        public LazyLoadSquadWrapper HomeCastle;
        //these go on Castles
        public int TimeTillmarkUp;
        public int TimeTillSpawnNextConstructor;
        public readonly Dictionary<GameEntityTypeData, int> ShipsInside = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 60, "TemplarPerUnitBaseInfo-ShipsInside" );
        public int MetalStored;

        //These go on wave leaders
        public readonly List<Planet> PlanetsVisited = List<Planet>.Create_WillNeverBeGCed( 30, "TemplarPerUnitBaseInfo-PlanetsVisited" );
        public int StrengthRalliedToWave;

        //These go on constructors
        public GameEntityTypeData UnitToBuild;
        public int PlanetIdx;
        public ArcenPoint LocationToBuild;

        //Things that go on Rifts
        public readonly List<NecromancerUpgrade> AvailableUpgrades = List<NecromancerUpgrade>.Create_WillNeverBeGCed( 60, "TemplarPerUnitBaseInfo-AvailableUpgrades" );
        //Not Serialized
        public Planet PlanetToBuildOn;
        public string ShipsInside_ForUI; //a string representation of the ShipsInside that's UI safe
        public Planet PlanetCastleWantsToHelp;

        public TemplarPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.DefenseMode = false;
            this.HomeCastle.Clear();

            this.TimeTillmarkUp = -1;
            this.TimeTillSpawnNextConstructor = -1;
            this.ShipsInside.Clear();
            this.MetalStored = 0;
            this.StrengthRalliedToWave = -1;

            this.PlanetsVisited.Clear();
            this.UnitToBuild = null;
            this.PlanetIdx = -1;
            this.LocationToBuild = ArcenPoint.ZeroZeroPoint;

            this.AvailableUpgrades.Clear();
            this.PlanetToBuildOn = null;
            this.ShipsInside_ForUI = string.Empty;
            this.PlanetCastleWantsToHelp = null;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            TemplarPerUnitBaseInfo target = CopyTarget as TemplarPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.DefenseMode = this.DefenseMode;
            target.HomeCastle = this.HomeCastle.CreateCopy();

            target.TimeTillmarkUp = this.TimeTillmarkUp;
            target.TimeTillSpawnNextConstructor = this.TimeTillSpawnNextConstructor;
            target.ShipsInside.ClearAndCopyFrom( this.ShipsInside );
            target.MetalStored = this.MetalStored;
            target.StrengthRalliedToWave = this.StrengthRalliedToWave;

            target.PlanetsVisited.CopyFrom( this.PlanetsVisited );
            target.UnitToBuild = this.UnitToBuild;
            target.PlanetIdx = this.PlanetIdx;
            target.LocationToBuild = this.LocationToBuild;

            target.AvailableUpgrades.CopyFrom( this.AvailableUpgrades );
            target.PlanetToBuildOn = this.PlanetToBuildOn;
            target.ShipsInside_ForUI = this.ShipsInside_ForUI;
            target.PlanetCastleWantsToHelp = this.PlanetCastleWantsToHelp;
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "TemplarPerUnitBaseInfo" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddBool(MetaData, this.DefenseMode);
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeCastle.GetPrimaryKeyID(), "HomeCastle.PrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeTillmarkUp );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeTillSpawnNextConstructor );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddInt32( MetaData, ReadStyle.Signed, this.MetalStored );
                Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.StrengthRalliedToWave);
            }
            GameEntityTypeDataTable.Instance.SerializeByIndex(MetaData, this.UnitToBuild, Buffer, "UnitToBuild");
            if (!SerializationCmdType.GetIsNetworkType())
                Buffer.AddArcenPointFromCombatSpace(MetaData, LocationToBuild, "LocationToBuild");
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)PlanetIdx, "PlanetIdx" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ShipsInside.Count, "ShipsInside.Count" );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, pair.Key, Buffer, "ShipInside.Type" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, ShipsInside[pair.Key], "ShipInside.Count" );
            }

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.AvailableUpgrades.Count, "AvailableUpgradeCount" );
            for ( int i = 0; i < this.AvailableUpgrades.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.AvailableUpgrades[i].Index, "AvailableUpgradeIndex" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddInt16(MetaData, ReadStyle.NonNeg, (Int16)this.PlanetsVisited.Count, "PlanetsVisited.Count");
                for (int i = 0; i < this.PlanetsVisited.Count; i++)
                    Buffer.AddPlanetIndex_Neg1ToPos(MetaData, this.PlanetsVisited[i].Index, "PlanetsVisited.Value");
            }
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "TemplarPerUnitBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "TemplarPerUnitBaseInfo Ext", TrackerStyle.ByTypeOnly );
            if (!SerializationCmdType.GetIsNetworkType())
                this.DefenseMode = Buffer.ReadBool( MetaData );
            this.HomeCastle = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeCastle.PrimaryKeyID" ), true, "TemplarCastDeser" );
            this.TimeTillmarkUp = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.TimeTillSpawnNextConstructor = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.MetalStored = Buffer.ReadInt32( MetaData, ReadStyle.Signed );
                if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 004))
                    this.StrengthRalliedToWave = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1);
            }

            UnitToBuild = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "UnitToBuild" );
            if (!SerializationCmdType.GetIsNetworkType())
                Buffer.FillArcenPointFromCombatSpace( MetaData, out this.LocationToBuild, "LocationToBuild");
            PlanetIdx = (int)Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlanetIdx" );

            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ShipsInside.Count" );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData data = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "ShipInside.Type" );
                ShipsInside[data] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "ShipInside.Count" );
            }
            this.AvailableUpgrades.Clear();
            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "AvailableUpgradeCount" );
            for ( int i = 0; i < count; i++ )
                this.AvailableUpgrades.Add( NecromancerUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "AvailableUpgradeIndex" ) ) );

            this.PlanetsVisited.Clear();
            if (!SerializationCmdType.GetIsNetworkType())
            {
                count = Buffer.ReadInt16(MetaData, ReadStyle.NonNeg, "PlanetsVisited.Count");
                for (int i = 0; i < count; i++)
                    this.PlanetsVisited.Add(World_AIW2.Instance.GetPlanetByIndex(Buffer.ReadPlanetIndex_Neg1ToPos(MetaData, "PlanetsVisited.Value")));
            }
            Buffer.StopTrackerByName( "TemplarPerUnitBaseInfo Ext" );
        }

        public void CopyFrom( TemplarPerUnitBaseInfo otherData )
        {
            this.DefenseMode = otherData.DefenseMode;
            this.TimeTillmarkUp = otherData.TimeTillmarkUp;
            this.TimeTillSpawnNextConstructor = otherData.TimeTillSpawnNextConstructor;
        }
        public int GetTotalStrengthInside( GameEntity_Squad entity )
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
                {
                    byte markLevel = GetMarkLevelForShipType( entity, pair.Key );

                    GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( markLevel );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * ShipsInside[pair.Key]);
                }
            }
            catch { }
            return strength;
        }
        public int GetStrikecraftStrengthInside( GameEntity_Squad entity )
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
                {
                    if ( !pair.Key.GetHasTag( "TemplarStrikecraft" ) )
                        continue;
                    byte markLevel = GetMarkLevelForShipType( entity, pair.Key );

                    GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( markLevel );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * ShipsInside[pair.Key]);
                }
            }
            catch { }
            return strength;
        }
        public int GetGuardianStrengthInside( GameEntity_Squad entity )
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
                {
                    if ( !pair.Key.GetHasTag( "TemplarGuardian" ) )
                        continue;
                    byte markLevel = GetMarkLevelForShipType( entity, pair.Key );

                    GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( markLevel );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * ShipsInside[pair.Key]);
                }
            }
            catch { }
            return strength;
        }
        public int GetDireStrengthInside( GameEntity_Squad entity )
        {
            //calculate the strength of ships in here
            int strength = 0;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
                {
                    if ( !pair.Key.GetHasTag( "TemplarDire" ) )
                        continue;
                    byte markLevel = GetMarkLevelForShipType( entity, pair.Key );

                    GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( markLevel );
                    strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * ShipsInside[pair.Key]);
                }
            }
            catch { }
            return strength;
        }
        public byte GetMarkLevelForShipType( GameEntity_Squad entity, GameEntityTypeData shipType )
        {
            byte baseStrikecraftMark = 0;
            byte baseGuardianMark = 0;
            byte baseDireMark = 0;
            byte result = 1;
            if ( entity.TypeData.GetHasTag( "TemplarMidTierStructure" ) )
                baseStrikecraftMark = 2;
            if ( entity.TypeData.GetHasTag( "TemplarHighTierStructure" ) )
            {
                baseStrikecraftMark = 4;
                baseGuardianMark = 2;
            }
            if ( shipType.GetHasTag( "TemplarStrikecraft" ) )
                result = (byte)(entity.CurrentMarkLevel + baseStrikecraftMark);
            if ( shipType.GetHasTag( "TemplarGuardian" ) )
                result = (byte)(entity.CurrentMarkLevel + baseGuardianMark);
            if ( shipType.GetHasTag( "TemplarDire" ) )
                result = (byte)(entity.CurrentMarkLevel + baseDireMark);
            if ( result > (byte)7 )
                result = 7;
            return result;
        }
        public static ArcenDoubleCharacterBuffer Buffer_ForShipsInside = new ArcenDoubleCharacterBuffer( "TemplarPerUnitBaseInfo-Buffer_ForShipsInside" );
        public void UpdateShipsInside_ForUI( GameEntity_Squad entity )
        {
            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[entity.CurrentMarkLevel];
            int shipsFound = 0;
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in ShipsInside )
            {
                shipsFound++;
                byte markLevel = GetMarkLevelForShipType( entity, pair.Key );
                GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( markLevel );
                int strengthForUnit = (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * this.ShipsInside[pair.Key]);
                if ( shipsFound > 1 )
                    Buffer_ForShipsInside.Add( ", " );

                if ( this.ShipsInside[pair.Key] > 0 )
                    Buffer_ForShipsInside.Add( pair.Key.GetDisplayName(), "909090" ).Add( " x" + this.ShipsInside[pair.Key].ToString(), markByOrdinal.ColorHex );
            }
            if ( shipsFound == 0 )
                Buffer_ForShipsInside.Add( "无单位。\n" );
            else
            {
                int strengthInside = GetTotalStrengthInside( entity ) / 1000;
                if ( strengthInside == 0 )
                    strengthInside = 1;
                Buffer_ForShipsInside.Add( "。约 " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( strengthInside, "ffa1a1" ).Add( "。\n" );
            }
            this.ShipsInside_ForUI = Buffer_ForShipsInside.GetStringAndResetForNextUpdate();
        }
    }
}
