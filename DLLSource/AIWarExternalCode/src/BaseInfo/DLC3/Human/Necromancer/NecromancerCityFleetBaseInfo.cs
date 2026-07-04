using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerCityFleetBaseInfo : ExternalFleetBaseInfo, IBolsteringFleet
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public bool CanChangeBolsteredFleet = false;

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public NecromancerCityFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.CanChangeBolsteredFleet = false;
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NecromancerCityFleetBaseInfo" );

            Buffer.AddBool( MetaData, this.CanChangeBolsteredFleet, "CanChangeBolsteredFleet" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NecromancerCityFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "NecromancerCityFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );

            this.CanChangeBolsteredFleet = Buffer.ReadBool( MetaData, "CanChangeBolsteredFleet" );
        }

        public override int AddToGetBaseSquadCapWithAdditions( int CapSoFar, FleetMembership FMem )
        {
            int debugCode = 100;
            try{
                debugCode = 200;
                if ( FMem == null )
                    return CapSoFar;
                debugCode = 300;
                Fleet fleet = this.AttachedFleet;
                if ( fleet == null )
                    return CapSoFar;
                debugCode = 400;
                Faction fac = fleet.Faction;
                debugCode = 500;
                NecromancerEmpireFactionBaseInfo necroFac = null;
                if ( fac != null )
                {
                    debugCode = 600;
                    necroFac = fac.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                }
                debugCode = 700;
                if ( necroFac != null && necroFac.NecromancerCompletedUpgrades != null &&
                     necroFac.NecromancerCompletedUpgrades.Count > 0 )
                {
                    debugCode = 800;
                    //process faction wide updates
                    for ( int i = 0; i < necroFac.NecromancerCompletedUpgrades.Count; i++ )
                    {
                        debugCode = 900;
                        NecromancerUpgrade upgrade = necroFac.NecromancerCompletedUpgrades[i];
                        if ( upgrade == null || upgrade.ShipForCapIncrease == null )
                            continue;
                        //ArcenDebugging.ArcenDebugLogSingleLine("processing faction upgrade " + upgrade.ToString() + " in fleet code", Verbosity.DoNotShow );
                        debugCode = 1000;
                        if ( upgrade.ShipForCapIncrease == FMem.TypeData )
                        {
                            debugCode = 1100;
                            CapSoFar += upgrade.CapIncrease;
                            //ArcenDebugging.ArcenDebugLogSingleLine("\tupdated base to " + newBase, Verbosity.DoNotShow );
                        }
                    }
                }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Necromancer Fleet AddToGetBaseSquadCapWithAdditions debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return CapSoFar;
        }

        public List<SafeSquadWrapper> FleetsThatCanBeBolstered {
            get {
                if ( !this.CanChangeBolsteredFleet ) {
                    return null;
                }
                NecromancerEmpireFactionBaseInfo necroFac = this.AttachedFleet?.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                return necroFac?.Flagships.GetDisplayList();
            }
        }

        public override void PerFrame_UpdateFleetData(Faction LocalPlayerFactionForUICalculations)
        {
            RecalculateNecromancerNecropolisBuildingContents();
        }

        public void RecalculateNecromancerNecropolisBuildingContents()
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //this is a host-only thing!

            CityManager.RecalculateCityBuildingContents(this.AttachedFleet, "NecromancerBuildMenu", delegate (GameEntity_Squad city, GameEntityTypeData buildingInfo) {
                if ( !city.TypeData.GetHasTag("DefensiveNecropolis") &&
                        buildingInfo.GetHasTag( "NecromancerDefensiveBuildMenuOnly" ) )
                    return false;
                if ( city.TypeData.GetHasTag("DefensiveNecropolis") &&
                        !buildingInfo.GetHasTag( "NecromancerDefensiveBuildMenu" ) )
                    return false; //if this is a defensive necropolis, only show the 'allowed' structures (ie nothing that might be built by a fleet)
                return true;
            });
        }
    }
}
