using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AIReservesPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int CurrentIncomePerSecond;
        public int StoredMetalGeneral;
        public int StoredMetalUniqueShips; //the ai reserves hsave some unique structures
        public int TimeToSpawnShipsNext;
        public int TimeWormholeSpawned;
        public bool UnitsToAttackPlayerPlanets;
        public bool ThisUnitAttackingPlayerPlanet;

        public AIReservesPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.CurrentIncomePerSecond = 0;
            this.StoredMetalGeneral = 0;
            this.StoredMetalUniqueShips = 0;
            this.TimeToSpawnShipsNext = 0;
            this.TimeWormholeSpawned = 0;
            this.UnitsToAttackPlayerPlanets = false;
            this.ThisUnitAttackingPlayerPlanet = false;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            AIReservesPerUnitBaseInfo target = CopyTarget as AIReservesPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.CurrentIncomePerSecond = this.CurrentIncomePerSecond;
            target.StoredMetalGeneral = this.StoredMetalGeneral;
            target.StoredMetalUniqueShips = this.StoredMetalUniqueShips;
            target.TimeToSpawnShipsNext = this.TimeToSpawnShipsNext;
            target.TimeWormholeSpawned = this.TimeWormholeSpawned;
            target.UnitsToAttackPlayerPlanets = this.UnitsToAttackPlayerPlanets;
            target.ThisUnitAttackingPlayerPlanet = this.ThisUnitAttackingPlayerPlanet;
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            if ( OriginalStackCount <= 0 )
                return;

            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack

            //let each half only have part of the metal. We can use floats because this is only happening on the host anyway.
            float percentageMetalLeft = (float)MyPersonalNewStackCount / ( float)OriginalStackCount;
            this.StoredMetalGeneral = Mathf.RoundToInt( this.StoredMetalGeneral * percentageMetalLeft );
            this.StoredMetalUniqueShips = Mathf.RoundToInt( this.StoredMetalUniqueShips * percentageMetalLeft );
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
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, CurrentIncomePerSecond, "CurrentIncomePerSecond" );
            Buffer.AddInt32( MetaData, ReadStyle.Signed, StoredMetalGeneral, "StoredMetalGeneral" );
            Buffer.AddInt32( MetaData, ReadStyle.Signed, StoredMetalUniqueShips, "StoredMetalUniqueShips" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeToSpawnShipsNext, "TimeToSpawnShipsNext" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeWormholeSpawned, "TimeWormholeSpawned" );
            Buffer.AddBool( MetaData, UnitsToAttackPlayerPlanets, "UnitsToAttackPlayerPlanets" );
            Buffer.AddBool( MetaData, ThisUnitAttackingPlayerPlanet, "ThisUnitAttackingPlayerPlanet" );
        }
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            CurrentIncomePerSecond = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "CurrentIncomePerSecond" );
            StoredMetalGeneral = Buffer.ReadInt32( MetaData, ReadStyle.Signed, "StoredMetalGeneral" );
            StoredMetalUniqueShips = Buffer.ReadInt32( MetaData, ReadStyle.Signed, "StoredMetalUniqueShips" );
            TimeToSpawnShipsNext = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeToSpawnShipsNext" );
            TimeWormholeSpawned = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeWormholeSpawned" );
            UnitsToAttackPlayerPlanets = Buffer.ReadBool( MetaData, "UnitsToAttackPlayerPlanets" );
            ThisUnitAttackingPlayerPlanet = Buffer.ReadBool( MetaData, "ThisUnitAttackingPlayerPlanet" );
        }
    }
}
