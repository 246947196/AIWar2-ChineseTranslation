using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgePerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //This data structure is applied to all Scourge units.
        //Some things (Experience) are used by everyone. Others are not
        public byte TierLevel; //mostly for display/just in case

        public FInt Experience; //everyone
        public FInt ExperienceForNextLevel; //everyone

        public FInt StoredMetal; //for Builders and spawners
        public bool NextMustBeAnUpgrade; //builders will sometimes need to Upgrade instead of build new
        public Int16 MustBuildNextOnFarFlungPlanetIdx; //sometimes we want to order a structure to be built on a far-off planet
        public Int16 ScourgeTypeId; //this is for Spawners to know which type of unit to create; this is an index into the ScourgeTypeDataTable

        public Int16 UnitsKilled; //this is for scourge infused empire

        public bool IsEvolved; //for Warriors
        public bool IsHybrid; //for Warriors
        public bool IsSubjugator;
        public bool IsNemesis; //for Nemeses
        public bool IsOffToUpgrade; //its nice to be able to tell the player what a unit is doing

        //Fireteams are groups of warriors. Nota Bene: we also like to set the MinorFactionStackingID to be the fireteamID for stacking purposes
        public int FireteamId; //needed more for logging purposes, since the actual fireteamid is on the unit now
        public bool MustEvolveBeforeJoiningFireteam; //every so often a warrior will just wait at home until it evolves
        public bool FullyInitialized; //we mess with this structure in both the Sim and LongRangePlanning code, so this is a "just in case"

        public bool IsAssignedToMission; //for vassal scourge

        public LazyLoadSquadWrapper HomeDefensiveStructure;
        public SafeSquadWrapper HomeDefensiveStructureSafe;
        //Not serialized
        public FInt MetalIncomeLastSecond_ForUI;
        public VassalMission MyMission;

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "Tier Level: " ).Add( this.TierLevel ).Add( " Experience " ).Add( this.Experience ).Add( " stored metal " ).Add( this.StoredMetal )
                .Add( " scourge type id " ).Add( this.ScourgeTypeId ).Add( " fireteam id " ).Add( this.FireteamId )
                .Add( " initialized " ).Add( this.FullyInitialized ).Add( " NextMustBeAnUpgrade " ).Add( this.NextMustBeAnUpgrade ).Add( ".\n" );
        }

        public ScourgePerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.TierLevel = 0;
            this.Experience = FInt.Zero;
            this.ExperienceForNextLevel = FInt.Zero;
            this.StoredMetal = FInt.Zero;
            this.NextMustBeAnUpgrade = false;
            this.ScourgeTypeId = -1;
            this.UnitsKilled = 0;
            this.IsEvolved = false;
            this.IsHybrid = false;
            this.IsSubjugator = false;
            this.IsNemesis = false;
            this.IsOffToUpgrade = false;
            this.FireteamId = -1;
            this.MustEvolveBeforeJoiningFireteam = false;
            this.FullyInitialized = false;
            this.MetalIncomeLastSecond_ForUI = FInt.Zero;
            this.MustBuildNextOnFarFlungPlanetIdx = -1;

            this.IsAssignedToMission = false;
            this.HomeDefensiveStructure.Clear();
            this.HomeDefensiveStructureSafe.Clear();
            this.MyMission = null;
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            ScourgePerUnitBaseInfo target = CopyTarget as ScourgePerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.TierLevel = this.TierLevel;
            target.Experience = this.Experience;
            target.StoredMetal = this.StoredMetal;
            target.ScourgeTypeId = this.ScourgeTypeId;
            target.UnitsKilled = this.UnitsKilled;
            target.IsEvolved = this.IsEvolved;
            target.IsHybrid = this.IsHybrid;
            target.IsSubjugator = this.IsSubjugator;
            target.IsNemesis = this.IsNemesis;
            target.IsOffToUpgrade = this.IsOffToUpgrade;
            target.MustEvolveBeforeJoiningFireteam = this.MustEvolveBeforeJoiningFireteam;
            target.NextMustBeAnUpgrade = this.NextMustBeAnUpgrade;
            target.FullyInitialized = this.FullyInitialized;
            target.MustBuildNextOnFarFlungPlanetIdx = this.MustBuildNextOnFarFlungPlanetIdx;
            target.IsAssignedToMission = this.IsAssignedToMission;
            target.MyMission = this.MyMission;
            target.HomeDefensiveStructure = this.HomeDefensiveStructure.CreateCopy();
            target.HomeDefensiveStructureSafe = this.HomeDefensiveStructureSafe.CreateCopy();

        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            //we could divide out experience or something, but it seems like that's not actually required
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
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, this.TierLevel);
            }
            Buffer.AddFInt( MetaData, this.Experience );
            Buffer.AddFInt( MetaData, this.ExperienceForNextLevel );
            Buffer.AddFInt( MetaData, this.StoredMetal );
            Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.UnitsKilled);
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.ScourgeTypeId);
                Buffer.AddBool(MetaData, this.IsEvolved);
                Buffer.AddBool(MetaData, this.IsHybrid);
                Buffer.AddBool(MetaData, this.IsNemesis);
                Buffer.AddBool(MetaData, this.IsOffToUpgrade);
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.FireteamId );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddBool(MetaData, MustEvolveBeforeJoiningFireteam);
                Buffer.AddBool(MetaData, NextMustBeAnUpgrade);
                Buffer.AddBool(MetaData, this.FullyInitialized);
                Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.MustBuildNextOnFarFlungPlanetIdx);
                Buffer.AddBool(MetaData, this.IsSubjugator);
                Buffer.AddBool(MetaData, this.IsAssignedToMission);
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeDefensiveStructure.GetPrimaryKeyID(), "HomeDefensiveStructure.PrimaryKeyID" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.TierLevel = Buffer.ReadByte(MetaData, ReadStyleByte.Normal);
            }
            this.Experience = Buffer.ReadFInt( MetaData );
            this.ExperienceForNextLevel = Buffer.ReadFInt( MetaData );
            this.StoredMetal = Buffer.ReadFInt( MetaData );
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 609))
                this.UnitsKilled = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1);
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.ScourgeTypeId = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1);
                this.IsEvolved = Buffer.ReadBool(MetaData);
                this.IsHybrid = Buffer.ReadBool(MetaData);
                this.IsNemesis = Buffer.ReadBool(MetaData);
                this.IsOffToUpgrade = Buffer.ReadBool(MetaData);
            }
            Buffer.SetIfNextIntErrorsThenCorrect( -1, true ); //older versions of the game sometimes used -5 to -1, but just -1 is fine
            this.FireteamId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.MustEvolveBeforeJoiningFireteam = Buffer.ReadBool(MetaData);
                this.NextMustBeAnUpgrade = Buffer.ReadBool(MetaData);
                this.FullyInitialized = Buffer.ReadBool(MetaData);
                this.MustBuildNextOnFarFlungPlanetIdx = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1);
                this.IsSubjugator = Buffer.ReadBool(MetaData);
                if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(3, 778))
                    this.IsAssignedToMission = Buffer.ReadBool(MetaData);
            }
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 617))
                this.HomeDefensiveStructure = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeDefensiveStructure.PrimaryKeyID" ), true, "ScourgeHomeStr" );
        }
    }
}
