using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DoomsdayMode : ExternalWorldDeepInfo
    {
        public static DoomsdayMode Instance;
        private DrawBag<Planet> WorkingPlanetBag = DrawBag<Planet>.Create_WillNeverBeGCed( 30, "DoomsdayModeDeepInfo-WorkingPlanetBag" );

        private List<Planet> DoomsdayList = List<Planet>.Create_WillNeverBeGCed( 500, "DoomsdayModeDeepInfo-DoomsdayedList" );
        public DoomsdayMode()
        {
            Instance = this;
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            WorkingPlanetBag.Clear();
            DoomsdayList.Clear();
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "DoomsdayMode";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        #region SerializeTo
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            
        }
        #endregion

        #region DeserializeIntoSelf
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) 
        {
            
        }
        #endregion
        
        
        protected override void DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            //nothing to do!
        }
        protected override void DoPerSecondLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DoomsdayMode" ) )
            {
                return;
            }

            int baseInterval = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "DoomsdayBaseInterval" );
            int increaseRate = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "DoomsdayIncreaseRate" );
            bool debug = false;
            if ( ( World_AIW2.Instance.GameSecond % 50 == 0 ||
                   ( DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath == World_AIW2.Instance.GameSecond + 2 ) )
                 && debug )
                ArcenDebugging.ArcenDebugLogSingleLine("Current Time: " + World_AIW2.Instance.GameSecond + " and next destroy: " + DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath, Verbosity.DoNotShow );
            if ( DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath == -1 )
            {
                int baseTime = baseInterval * 60;
                int variance = baseTime / 10;
                int nextTime = Context.RandomToUse.Next( baseTime - variance, baseTime + variance );
                DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath = World_AIW2.Instance.GameSecond + nextTime; //initialize for first doomsday
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Initializing our first planet destruction time to be " + DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath + ". baseTime " + baseTime + " baseInterval " + baseInterval + " variance " + variance + " nextTime " + nextTime, Verbosity.DoNotShow );
            }
            if ( World_AIW2.Instance.GameSecond > DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath )
            {
                //destroy a random planet
                UpdateDoomsdayVictimPlanets();
                Planet planet = GetDoomsdayPlanet ( Context );
                planet.IsPlanetToBeDestroyed = true;
                planet.IsDoomsdayVictim = true;
                planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;


                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    if ( neighbor == null || neighbor.HasPlanetBeenDestroyed )
                        continue;
                    neighbor.AdjacentDoomsdayVictims++;
                }

                GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                if ( commandStation != null )
                {
                    foreach ( GameEntity_Squad otherEntity in planet.Squads( EntityRollupType.AIPOnDeath ) )
                    {
                        otherEntity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                    commandStation.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                }

                for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
                {
                    PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction(World_AIW2.Instance.Factions[i]);
                    if ( localPlanetFaction == null )
                        continue;
                    localPlanetFaction.AIPLeftFromCommandStation = 0;
                    localPlanetFaction.AIPLeftFromWarpGate = 0;
                }


                int baseTime = baseInterval * 60;
                int increase = DoomsdayList.Count * increaseRate;
                int nextTime = baseTime + increase;
                int variance = nextTime / 10;
                nextTime = Context.RandomToUse.Next( nextTime - variance, nextTime + variance );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("base time: " + baseTime + " increase " + increase + " variance " + variance + " next time: " + nextTime + ". We chose to destroy " + planet.Name +", which had " + planet.AdjacentDoomsdayVictims + " previously adjacent victims" , Verbosity.DoNotShow );

                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = planet;

                World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#ff0000>Doomsday Mode</color>: Planet " + planet.Name + " has been destroyed.", 
                    ChatType.LogToCentralChat, chatHandlerOrNull );
                DoomsdayModeWorldBaseInfo.Instance.TimeForNextPlanetDeath = World_AIW2.Instance.GameSecond + nextTime;
            }
       }
       private void UpdateDoomsdayVictimPlanets( )
       {
           DoomsdayList.Clear();
           foreach ( Planet planet in World_AIW2.Instance.Planets( true ) )
           {
               if ( planet.IsDoomsdayVictim )
               {
                   DoomsdayList.Add( planet );
               }
           }
       }
       private Planet GetDoomsdayPlanet( ArcenHostOnlySimContext Context )
       {
           WorkingPlanetBag.Clear();
           foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
           {
               if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                   continue; //no planets currently owned by the player

               if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld ||
                    planet.PopulationType == PlanetPopulationType.AIHomeworld ||
                    planet.PopulationType == PlanetPopulationType.DarkZenith ||
                    planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                   continue;

               bool playerFlagshipFound = false;
               foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.CenterpieceMustLive ) )
               {
                   GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                   if ( centerpiece == null )
                       continue;
                   if ( centerpiece.PlanetFaction.Faction.Type != FactionType.Player )
                       continue;
                   if ( centerpiece.Planet == planet )
                   {
                       playerFlagshipFound = true;
                       break;
                   }
               }
               if ( playerFlagshipFound )
               {
                   continue;
               }
               int numEntries = 1 + planet.AdjacentDoomsdayVictims * 10;

               WorkingPlanetBag.AddItem( planet, numEntries );
           }
           if ( !WorkingPlanetBag.GetHasItems() )
               return null;
           return WorkingPlanetBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
       }
    }
}
