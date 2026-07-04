using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ElderlingsPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        public int HatchTime;
        public int NextEggLayingTime;
        public int NumberOfTimesLeveledUp;
        public int ExperienceRequired;
        public int SanityRemaining = -1;
        public bool FullyUpgraded; //this unit will not attempt to upgrade further

        public bool SuicideMode;
        public bool TrackedByPlayer;

        public List<Planet> Territory = List<Planet>.Create_WillNeverBeGCed( 20, "ElderlingsPerUnitBaseInfo-Territory" );
        public bool GetsAFreeTerritoryIfPossible; //try to increase our territory next iteration. Serialized in case the elderling is having trouble expanding (so it gets a free chance later)

        public Planet PlanetForEgg;
        public ArcenPoint LocationToBuild;

        //Not Serialized
        public Planet LurePlanet;
        public int LastTerritoryUpdateTime; //Last time we tried to update an Elderling's territory. Not worth serializing to disk, a minor perf improvement

        public ElderlingsPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            HatchTime = -1;
            NextEggLayingTime = -1;
            NumberOfTimesLeveledUp = -1;
            ExperienceRequired = -1;
            SanityRemaining = -1;
            Territory.Clear();
            PlanetForEgg = null;
            SuicideMode = false;
            TrackedByPlayer = false;
            LurePlanet = null;
            GetsAFreeTerritoryIfPossible = false;
            this.LocationToBuild = ArcenPoint.ZeroZeroPoint;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ElderlingsPerUnitBaseInfo target = CopyTarget as ElderlingsPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.HatchTime = this.HatchTime;
            target.NextEggLayingTime = this.NextEggLayingTime;
            target.NumberOfTimesLeveledUp = this.NumberOfTimesLeveledUp;
            target.ExperienceRequired = this.ExperienceRequired;
            target.SanityRemaining = this.SanityRemaining;
            target.Territory.CopyFrom( this.Territory );
            target.PlanetForEgg = this.PlanetForEgg;
            target.SuicideMode = this.SuicideMode;
            target.TrackedByPlayer = this.TrackedByPlayer;
            target.LurePlanet = this.LurePlanet;
            target.GetsAFreeTerritoryIfPossible = this.GetsAFreeTerritoryIfPossible;
            target.LocationToBuild = this.LocationToBuild;
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
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Elderling PerUnit Data" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, HatchTime, "HatchTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, NextEggLayingTime, "NextEggLayingTime" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, NumberOfTimesLeveledUp, "NumberOfTimesLeveledUp" );
            if ( ExperienceRequired < -1 )
                ExperienceRequired = -1;
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, ExperienceRequired, "ExperienceRequired" );
            if ( SanityRemaining < -1 )
                SanityRemaining = -1;
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, SanityRemaining, "SanityRemaining" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.Territory.Count, "Territory.Count" );
            for ( int i = 0; i < this.Territory.Count; i++ )
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.Territory[i].Index, "Territory.Value" );
            Buffer.AddBool( MetaData, this.GetsAFreeTerritoryIfPossible, "GetsAFreeTerritoryIfPossible" );
            if ( PlanetForEgg == null )
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, -1, "PlanetForEgg");
            else
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.PlanetForEgg.Index, "PlanetForEgg" );

            Buffer.AddArcenPointFromCombatSpace( MetaData, LocationToBuild, "LocationToBuild" );
            Buffer.AddBool( MetaData, this.SuicideMode, "SuicideMode" );
            Buffer.AddBool( MetaData, this.TrackedByPlayer, "TrackedByPlayer" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Elderling PerUnit Data" );
            HatchTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HatchTime" );
            NextEggLayingTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "NextEggLayingTime" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 009 ) ) {
                NumberOfTimesLeveledUp = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "NumberOfTimesLeveledUp" );
            } else {
                Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "NecromancerUpgradeIndex" );
                if (this.AttachedSquad.TypeData.GetHasTag("Elderling")) {
                    NumberOfTimesLeveledUp = this.AttachedSquad.CurrentMarkLevel - 1;
                }
            }
            ExperienceRequired = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "ExperienceRequired" );
            SanityRemaining = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SanityRemaining" );
            this.Territory.Clear();
            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "Territory.Count" );
            for ( int i = 0; i < count; i++ )
                this.Territory.AddButRejectIfNull( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "Territory.Value" ) ) );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 902 ) )
                this.SuicideMode = Buffer.ReadBool( MetaData, "GetsAFreeTerritoryIfPossible" );
            this.PlanetForEgg = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "PlanetForEgg" ) );
            LocationToBuild = Buffer.ReadArcenPointFromCombatSpace( MetaData, "LocationToBuild" );

            this.SuicideMode = Buffer.ReadBool( MetaData, "SuicideMode" );
            this.TrackedByPlayer = Buffer.ReadBool( MetaData, "TrackedByPlayer" );
        }

        public void UpdateForTransformedElderling(ElderlingsPerUnitBaseInfo oldInfo)
        {
            this.Territory.AddRange(oldInfo.Territory);
            this.TrackedByPlayer = oldInfo.TrackedByPlayer;
            // We don't want the hacked elderling to upgraded on us.
            this.FullyUpgraded = true;
            this.ExperienceRequired = 0;
            // TODO: Should we copy the sanity, maybe with a penalty?
        }
    }
}
