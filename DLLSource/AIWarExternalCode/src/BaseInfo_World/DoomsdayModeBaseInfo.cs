using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DoomsdayModeWorldBaseInfo : ExternalWorldBaseInfo
    {
        public int TimeForNextPlanetDeath;
        public static DoomsdayModeWorldBaseInfo Instance;
        public DoomsdayModeWorldBaseInfo()
        {
            Instance = this;
            SetDefaults();
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            TimeForNextPlanetDeath = -1;
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "DoomsdayModeBaseInfo";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        #region SerializeTo
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DoomsdayModeBaseInfo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextPlanetDeath, "TimeForNextPlanetDeath" );
        }
        #endregion

        #region DeserializeIntoSelf
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) 
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DoomsdayModeBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "DoomsdayModeBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.TimeForNextPlanetDeath = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextPlanetDeath" );
        }
        #endregion

        #region DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        #endregion

        protected override void DoPerSecondLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context )
        {
        }
    }
}
