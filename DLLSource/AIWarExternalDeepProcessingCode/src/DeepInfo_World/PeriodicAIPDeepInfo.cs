using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PeriodicAIP : ExternalWorldDeepInfo
    {
        public static PeriodicAIP Instance;

        private bool loaded = false;//Why not cache these values? They don't change mid-game anyway, slight but easy performance boost with no drawbacks
        private int periodicAIP;
        private int intervalSeconds;

        public PeriodicAIP()
        {
            Instance = this;
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            loaded = false;
        }

        public override string GetIdentifierForErrorMessages()
        {
            return GetType().Name;
        }

        public override bool GetShouldIBeInUse()
        {
            return true;
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override void DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context ) { }

        protected override void DoPerSecondLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( !loaded )
            {
                periodicAIP = World_AIW2.Instance.Setup.GetIntBySetting( "AIP_Periodic" );
                intervalSeconds = World_AIW2.Instance.Setup.GetIntBySetting( "AIP_PeriodicInterval" ) * 60;
            }
            if ( World_AIW2.Instance.GameSecond <= 1 )
            {
                FInt aip = FactionUtilityMethods.Instance.GetCurrentAIP();
                if ( aip < FInt.One )
                {
                    //In spectator mode, the game starts at 0 AIP. This means that (at least) AI factions in civil war
                    //won't do anything; there's not an obvious place to put this in the Spectator mode player code,
                    //so this is a reasonable place to put it
                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( FInt.One, AIPChangeReason.AutoIncrease, null, -1, -1, -1 );
                }
            }

            if ( World_AIW2.Instance.GameSecond > 0 && intervalSeconds > 0 &&
                 World_AIW2.Instance.GameSecond % intervalSeconds == 0 )
            {
                if (periodicAIP != FInt.Zero) {
                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt) periodicAIP, AIPChangeReason.AutoIncrease, null, -1, -1, -1 );
                }
            }
        }
    }
}
