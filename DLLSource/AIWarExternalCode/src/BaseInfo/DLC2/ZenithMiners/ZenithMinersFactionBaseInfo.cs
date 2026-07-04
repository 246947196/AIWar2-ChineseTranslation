using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithMinersFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        public int TimeToSpawnNextProbe;
        public int NumProbesSpawned;
        public int NumMinersSpawned;
        public int NumSuccessfulMinings;
        //the ZM is not allowed to go to these planets
        public readonly List<Planet> BlockedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithMinersFactionBaseInfo-BlockedPlanets" );
        public readonly List<bool> NextMinerRavage = List<bool>.Create_WillNeverBeGCed( 30, "ZenithMinersFactionBaseInfo-NextMinerRavage" );

        //not serialized
        public static ZenithMinersFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public int Intensity = 0;
        public ZenithMinersDifficulty Difficulty = null;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ZenithMinersFactionBaseInfo-Teams" );
        //the Probes/Miners lists are kept here for ease of use with Notifications.
        //they aren't serialized since they are updated each sim step
        public readonly DoubleBufferedList<SafeSquadWrapper> Probes = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZMiners-Probes" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Miners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "ZMiners-Miners" );

        public ZenithMinersFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeToSpawnNextProbe = -1;
            NumProbesSpawned = 0;
            NumMinersSpawned = 0;
            NumSuccessfulMinings = 0;
            BlockedPlanets.Clear();
            NextMinerRavage.Clear();

            Teams.Clear();
            Probes.Clear();
            Miners.Clear();

            Intensity = 0;
            Instance = null;

            HaveLoadedData = false; //force xml reload
        }

        #region Serialization And Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeToSpawnNextProbe );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NumProbesSpawned );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NumMinersSpawned );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NumSuccessfulMinings );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.BlockedPlanets.Count );
            for ( int i = 0; i < this.BlockedPlanets.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.BlockedPlanets[i].Index );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.NextMinerRavage.Count );
            for ( int i = 0; i < this.NextMinerRavage.Count; i++ )
                Buffer.AddBool( MetaData, this.NextMinerRavage[i] );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            BlockedPlanets.Clear();
            NextMinerRavage.Clear();
            TimeToSpawnNextProbe = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            NumProbesSpawned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            NumMinersSpawned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            NumSuccessfulMinings = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );

            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < count; i++ )
                this.BlockedPlanets.Add( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.NonNeg ) ) );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < count; i++ )
                this.NextMinerRavage.Add( Buffer.ReadBool( MetaData ) );
        }
        #endregion end Serialization And Deserialization

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "10 Load From Zenith Miner" );
            return 10;
        }

        #region Xml Data
        public bool HaveLoadedData = false;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;

        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            this.LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            Difficulty = ZenithMinersDifficultyTable.Instance.GetRowByIntensity( this.Intensity );
        }
        #endregion

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 200;
                if ( Difficulty == null )
                    throw new Exception( "Unable to read Zenith Miners Difficulty in stage2" );
                debugCode = 300;

                this.Miners.ClearConstructionListForStartingConstruction();
                this.Probes.ClearConstructionListForStartingConstruction();
                debugCode = 500;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ZenithMinerMobile" ) )
                {
                    this.Miners.AddToConstructionList( entity );
                }
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ZenithMinerStationary" ) )
                {
                    this.Miners.AddToConstructionList( entity );
                }

                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ZenithMinerProbe" ) )
                {
                    this.Probes.AddToConstructionList( entity );
                }
                this.Miners.SwitchConstructionToDisplay();
                this.Probes.SwitchConstructionToDisplay();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception debugCode " + debugCode + " in zenith miners stage 2 " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            base.WriteFactionSlotStatus( buffer );
            
            buffer.Add( "Om nom nom", "ff2020" );
        }

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) ||
                Target.TypeData.GetHasTag( "WarpGate" ) ||
                Target.TypeData.IsCommandStation )
                return true;
            return false;
        }

        #region GetZenithMinersStateForDisplay
        public void GetZenithMinersStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //For debug, this goes in the Threat menu
            buffer.Add( "\n" );
            buffer.Add( "Time for next Zenith Miner probe: " + Engine_Universal.ToHoursAndMinutesString( this.TimeToSpawnNextProbe ) ).Add( " (" + (this.TimeToSpawnNextProbe - World_AIW2.Instance.GameSecond) + ")" ).Add( "\n" );
            buffer.Add( "Number of probes spawned: " + this.NumProbesSpawned ).Add( "\n" );
            buffer.Add( "Num Planets Eaten: " + this.NumSuccessfulMinings ).Add( "\n" );
            List<SafeSquadWrapper> probes = this.Probes.GetDisplayList();
            buffer.Add( "Current probes: " + probes.Count ).Add( ".\n" );
            for ( int i = 0; i < probes.Count; i++ )
                buffer.Add( "\t" + probes[i].ToStringWithPlanet() + "\n" );
            List<SafeSquadWrapper> miners = this.Miners.GetDisplayList();
            buffer.Add( "Current miners: " + miners.Count ).Add( ".\n" );
            for ( int i = 0; i < miners.Count; i++ )
                buffer.Add( "\t" + miners[i].ToStringWithPlanet() + "\n" );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region EffectToString
        public static string EffectToString( ZenithMinerEffect effect )
        {
            if ( effect == ZenithMinerEffect.DestroyPlanet )
                return "destroy the planet";
            if ( effect == ZenithMinerEffect.RavagePlanet )
                return "ravage the planet's resource production";
            if ( effect == ZenithMinerEffect.SlowShipsOnPlanet )
                return "permanently slow all ships when on this planet";
            if ( effect == ZenithMinerEffect.SpeedupShipsOnPlanet )
                return "permanently speed up all ships when on this planet";
            if ( effect == ZenithMinerEffect.MakePlanetNomadic )
                return "make this planet nomadic";
            if ( effect == ZenithMinerEffect.DestroyDysonSphere )
                return "destroy the Dyson Sphere on this planet";
            if ( effect == ZenithMinerEffect.DiminishZenithArchitrave )
                return "destroy part of the Zenith Architrave's territory. If this planet has the Portal then the Architrave can be permanently defeated by destroying its remaining spawners";

            return "Unknown effect. This is a bug";
        }
        #endregion

        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;
            int debugCode = 0;
            try
            {
                debugCode = 100;

                NotifierFillData fillData = null;
                debugCode = 260020;
                //We have a notification for both Probes and/or Miners
                if ( this.Probes.Count > 0 )
                {
                    debugCode = 260030;
                    if ( fillData == null )
                        fillData = NotifierFillData.GetFromPoolOrCreate();
                    debugCode = 260040;
                    List<SafeSquadWrapper> probes = this.Probes.GetDisplayList();
                    for ( int j = 0; j < probes.Count; j++ )
                    {
                        debugCode = 260040;
                        fillData.EntityList2.Add( probes[j] ); //probes
                    }
                    fillData.Faction = AttachedFaction;
                }
                debugCode = 260050;
                if ( this.Miners.Count > 0 )
                {
                    debugCode = 260060;
                    if ( fillData == null )
                        fillData = NotifierFillData.GetFromPoolOrCreate();
                    debugCode = 260070;
                    List<SafeSquadWrapper> miners = this.Miners.GetDisplayList();
                    for ( int j = 0; j < miners.Count; j++ )
                        fillData.EntityList.Add( miners[j] ); //miners
                    debugCode = 260080;
                    fillData.Faction = AttachedFaction;
                }
                if ( fillData != null )
                {
                    debugCode = 260090;
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicZenithMinerNotifier.Instance, fillData, "", 0, "Zenith Miners",
                        fillData.EntityList.Count > 0 ? SortedNotificationPriorityLevel.Major : SortedNotificationPriorityLevel.Medium );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Zenith Miners notification code, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
    }
}
