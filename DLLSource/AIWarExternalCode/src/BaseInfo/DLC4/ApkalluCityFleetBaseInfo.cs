using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ApkalluCityFleetBaseInfo : ExternalFleetBaseInfo, IBolsteringFleet
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public bool CanChangeBolsteredFleet = false;

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public ApkalluCityFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            this.CanChangeBolsteredFleet = false;
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluCityFleetBaseInfo" );
            Buffer.AddBool( MetaData, this.CanChangeBolsteredFleet, "CanChangeBolsteredFleet" );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluCityFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ApkalluCityFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.CanChangeBolsteredFleet = Buffer.ReadBool( MetaData, "CanChangeBolsteredFleet" );
        }

        public override int AddToGetBaseSquadCapWithAdditions( int CapSoFar, FleetMembership FMem )
        {
            int debugCode = 100;
            try
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ApkalluCityFleetBaseInfo AddToGetBaseSquadCapWithAdditions debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return CapSoFar;
        }

        public List<SafeSquadWrapper> FleetsThatCanBeBolstered {
            get {
                if ( !this.CanChangeBolsteredFleet )
                    return null;
                ApkalluFactionBaseInfo apkalluFac = this.AttachedFleet?.Faction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                return apkalluFac?.Flagships.GetDisplayList();
            }
        }
    }
}
