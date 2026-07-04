using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ApkalluPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        // Serialized
        public int MetalAccumulated;   // metal saved up at this unit for ship production
        public Int16 HomePlanetIdx;    // for ships that track a home planet
        public int LamassuRespawnReadyAt = -1;  // game second when Ziggurat should respawn its Lamassu; -1 = not scheduled
        public int LamassuEntityPrimaryKeyID = -1; // PrimaryKeyID of the Lamassu this Ziggurat currently owns; -1 = none
        public int FlagshipEntityPrimaryKeyID = -1; // PrimaryKeyID of the flagship this MajorDuru owns; -1 = not yet granted

        public int ResourcePoints;    // for Pilgrims
        public readonly List<Int16> PlanetsVisitedIdx = List<Int16>.Create_WillNeverBeGCed( 16, "ApkalluPerUnit-PlanetsVisitedIdx" );

        // Not Serialized — rebuilt from PlanetsVisitedIdx on deserialize
        public readonly List<Planet> PlanetsVisited = List<Planet>.Create_WillNeverBeGCed( 16, "ApkalluPerUnit-PlanetsVisited" );
        public Planet HomePlanet;
        public Planet LocustDestination;

        public int RangerMetal;
        public int DireRangerMetal;
        public LazyLoadSquadWrapper HomeDuru;
        public SafeSquadWrapper HomeDuruSafe;

        //Not Serialized
        public int RangerCap;
        public int DireRangerCap;

        public ApkalluPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            MetalAccumulated = 0;
            HomePlanetIdx = -1;
            HomePlanet = null;
            LocustDestination = null;
            LamassuRespawnReadyAt = -1;
            LamassuEntityPrimaryKeyID = -1;
            FlagshipEntityPrimaryKeyID = -1;
            ResourcePoints = 0;
            PlanetsVisitedIdx.Clear();
            PlanetsVisited.Clear();

            RangerMetal = 0;
            DireRangerMetal = 0;

            RangerCap = -1;
            DireRangerCap = -1;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ApkalluPerUnitBaseInfo target = CopyTarget as ApkalluPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.MetalAccumulated = this.MetalAccumulated;
            target.HomePlanetIdx = this.HomePlanetIdx;
            target.HomePlanet = this.HomePlanet;
            target.LocustDestination = this.LocustDestination;
            target.LamassuRespawnReadyAt = this.LamassuRespawnReadyAt;
            target.LamassuEntityPrimaryKeyID = this.LamassuEntityPrimaryKeyID;
            target.FlagshipEntityPrimaryKeyID = this.FlagshipEntityPrimaryKeyID;
            target.ResourcePoints = this.ResourcePoints;
            target.PlanetsVisitedIdx.Clear();
            target.PlanetsVisited.Clear();
            for ( int i = 0; i < this.PlanetsVisitedIdx.Count; i++ )
            {
                target.PlanetsVisitedIdx.Add( this.PlanetsVisitedIdx[i] );
                target.PlanetsVisited.Add( this.PlanetsVisited[i] );
            }
            target.RangerMetal = this.RangerMetal;
            target.DireRangerMetal = this.DireRangerMetal;
            target.HomeDuru = this.HomeDuru.CreateCopy();
            target.HomeDuruSafe = this.HomeDuruSafe.CreateCopy();
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
        }

        public override void DoAfterSingleOtherShipMergedIntoOurStack( ExternalSquadBaseInfo OtherShipBeingDiscarded )
        {
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.MetalAccumulated, "MetalAccumulated" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.HomePlanetIdx, "HomePlanetIdx" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LamassuRespawnReadyAt, "LamassuRespawnReadyAt" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LamassuEntityPrimaryKeyID, "LamassuEntityPrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.ResourcePoints, "ResourcePoints" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.PlanetsVisitedIdx.Count, "PlanetsVisited.Count" );
            for ( int i = 0; i < this.PlanetsVisitedIdx.Count; i++ )
            {
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.PlanetsVisitedIdx[i], "PlanetsVisited[" + i + "]" );
            }

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, RangerMetal, "RangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DireRangerMetal, "DireRangerMetal" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeDuru.GetPrimaryKeyID(), "HomeDuru.PrimaryKeyID" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.FlagshipEntityPrimaryKeyID, "FlagshipEntityPrimaryKeyID" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.MetalAccumulated = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "MetalAccumulated" );
            this.HomePlanetIdx = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "HomePlanetIdx" );
            this.HomePlanet = World_AIW2.Instance.GetPlanetByIndex( this.HomePlanetIdx );
            this.LamassuRespawnReadyAt = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "LamassuRespawnReadyAt" );
            this.LamassuEntityPrimaryKeyID = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "LamassuEntityPrimaryKeyID" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 805 ) )
                this.ResourcePoints = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ResourcePoints" );
            this.PlanetsVisitedIdx.Clear();
            this.PlanetsVisited.Clear();
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 805 ) )
            {
                int count = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "PlanetsVisited.Count" );
                for ( int i = 0; i < count; i++ )
                {
                    Int16 idx = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlanetsVisited[" + i + "]" );
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( idx );
                    this.PlanetsVisitedIdx.Add( idx );
                    if ( planet != null )
                        this.PlanetsVisited.Add( planet );
                }
            }
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 508 ) )
            {
                RangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "RangerMetal" );
                DireRangerMetal = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DireRangerMetal" );
            }
            this.HomeDuru = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeDuru.PrimaryKeyID" ), true, "ApkalluHomeStr" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 809 ) )
                this.FlagshipEntityPrimaryKeyID = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "FlagshipEntityPrimaryKeyID" );

        }
    }
}
