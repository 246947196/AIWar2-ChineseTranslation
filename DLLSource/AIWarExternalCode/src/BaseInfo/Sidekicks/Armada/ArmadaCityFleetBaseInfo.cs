using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaCityFleetBaseInfo : ExternalFleetBaseInfo, IBolsteringFleet
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public bool CanChangeBolsteredFleet = false;

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public ArmadaCityFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.CanChangeBolsteredFleet = false;
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaCityFleetBaseInfo" );

            Buffer.AddBool( MetaData, this.CanChangeBolsteredFleet, "CanChangeBolsteredFleet" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaCityFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ArmadaCityFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );

            this.CanChangeBolsteredFleet = Buffer.ReadBool( MetaData, "CanChangeBolsteredFleet" );
        }

        public override int AddToGetBaseSquadCapWithAdditions( int CapSoFar, FleetMembership FMem )
        {
            int debugCode = 100;
            try{
                // debugCode = 200;
                // if ( FMem == null )
                //     return CapSoFar;
                // debugCode = 300;
                // Fleet fleet = this.AttachedFleet;
                // if ( fleet == null )
                //     return CapSoFar;
                // debugCode = 400;
                // Faction fac = fleet.Faction;
                // debugCode = 500;
                // ArmadaFactionBaseInfo dysonFac = null;
                // if ( fac != null )
                // {
                //     debugCode = 600;
                //     dysonFac = fac.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                // }
                // debugCode = 700;
                // if ( dysonFac != null && dysonFac.ArmadaCompletedUpgrades != null &&
                //      dysonFac.ArmadaCompletedUpgrades.Count > 0 )
                // {
                //     debugCode = 800;
                //     //process faction wide updates
                //     for ( int i = 0; i < dysonFac.ArmadaCompletedUpgrades.Count; i++ )
                //     {
                //         debugCode = 900;
                //         ArmadaUpgrade upgrade = dysonFac.ArmadaCompletedUpgrades[i];
                //         if ( upgrade == null || upgrade.ShipForCapIncrease == null )
                //             continue;
                //         //ArcenDebugging.ArcenDebugLogSingleLine("processing faction upgrade " + upgrade.ToString() + " in fleet code", Verbosity.DoNotShow );
                //         debugCode = 1000;
                //         if ( upgrade.ShipForCapIncrease == FMem.TypeData )
                //         {
                //             debugCode = 1100;
                //             CapSoFar += upgrade.CapIncrease;
                //             //ArcenDebugging.ArcenDebugLogSingleLine("\tupdated base to " + newBase, Verbosity.DoNotShow );
                //         }
                //     }
                // }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Armada Fleet AddToGetBaseSquadCapWithAdditions debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return CapSoFar;
        }

        public List<SafeSquadWrapper> FleetsThatCanBeBolstered {
            get {
                if ( !this.CanChangeBolsteredFleet ) {
                    return null;
                }
                ArmadaFactionBaseInfo dysonFac = this.AttachedFleet?.Faction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                return dysonFac?.Flagships.GetDisplayList();
            }
        }
    }
}
