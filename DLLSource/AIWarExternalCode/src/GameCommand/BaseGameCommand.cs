using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class BaseGameCommand : IGameCommandTypeImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseGameCommands" );
        private static readonly object planetGateDropLogLock = new object();
        private static float lastLoggedPlanetGateDropRealtime = -1f;
        private static int planetGateDropsSinceLastLog = 0;
        public BaseGameCommand()
        {
            RefTracker.IncrementObjectCount();
        }

        public static readonly EnumIndexedArray<Code, GameCommandType> CommandsByCode = EnumIndexedArray<Code, GameCommandType>.Create_WillNeverBeGCed( false, null, "BaseGameCommand-CommandsByCode" );

        public virtual void LoadCustomData( ArcenDynamicTableRow Row )
        {
            Code code;
            try
            {
                code = (Code)Enum.Parse( typeof( Code ), Row.InternalName );
            }
            catch
            {
                return;
            }
            CommandsByCode[code] = (GameCommandType)Row;
            CommandsByCode[code].ExternalCode = (int)code;
        }

        public virtual void ExamineOnHostForPKIDRequests( GameCommand command )
        {
            //optional thing, run on the host before the host gives the command to itself and clients,
            //to find out if we need to add any PKID requests
        }

        public abstract void Execute( GameCommand command, ArcenClientOrHostSimContextCore context );

        public enum Code
        {
            None,
            MoveManyToOnePoint_Player,
            MoveManyToOnePoint_PlayerRemoteShipsToHere,
            MoveManyToOnePoint_Formation,
            MoveManyToOnePoint_BaseAICamping,
            MoveManyToOnePoint_NPCChaseAKing,
            MoveManyToOnePoint_NPCWander,
            MoveManyToOnePoint_BaseAIHunkerDown,
            MoveManyToOnePoint_NPCNearestWormhole,
            MoveManyToOnePoint_NPCVisitTargetOnPlanet,
            MoveManyToOnePoint_NPCFollowGuardedUnit,
            MoveManyToOnePoint_FireteamCamp,
            MoveManyToOnePoint_FireteamEscort,
            MoveManyToOnePoint_PlayerForcefieldReturns,
            MoveManyToOnePoint_NPCPatrol,
            MoveManyToOnePoint_NPCRandomWormhole,
            MoveManyToOnePoint_NPCRandomMetalSpot,
            MoveManyToOnePoint_NPCBulkPathfind,
            MoveManyToOnePoint_NPCRandomPlanetPoint,
            MoveManyToManyPoints,
            Attack,
            SetWormholePath_Player,
            SetHoldFireMode,
            PlaceSelfBuildingUnit,
            UnlockTech,
            SetBehavior_FromPlayer,
            SetBehavior_FromFaction_SomeOtherReason,
            SetBehavior_FromFaction_GoBackToGuardingCommand,
            SetBehavior_FromFaction_ThreatGoesBackToSleep,
            SetBehavior_FromFaction_AbandonBecauseLargeEnemyForce,
            SetBehavior_FromFaction_ITractoredShips,
            SetBehavior_FromFaction_NoPraetorianGuard,
            SetBehavior_FromFaction_RunAwayWithEnemyShips,
            SetBehavior_FromFaction_ThreatTimeToFight,
            SetBehavior_FromFaction_SendOnThreatRaid,
            SetBehavior_FromFaction_RetreatThreat,
            Debug_RevealAll,
            Debug_ExploreAll,
            ScrapUnits,
            ChangeFrameSize,
            ChangeGameSpeed,
            ChangePlanetFactionBooleanFlag,
            SetWaiting,
            SetActiveHack,
            UpdateExternalInvulnerability,
            SaveGame,
            TransferEntitiesToFaction,
            TransformUnits,
            DeleteSaveGame,
            DeleteAllSaveGames,
            ModderCommand,
            SetTargetList,
            SetFRDTarget,
            StackUnits,
            SetStopToShootAnySeenTargets,
            CreateSpeedGroup_Player,
            ToggleEnabled,
            UnloadTransports,
            SetTransportIntoLoadMode,
            DecollideUnits_MoveManyToMany_Player,
            PauseOnly,
            UnpauseOnly,
            DestroyDistantFleetMembers,
            FlushReinforcementPoints,
            SetWormholePath_NPCSingleUnit,
            SetWormholePath_NPCFireTeams,
            SetWormholePath_AIRaidKing,
            SetWormholePath_ExoMove,
            SetWormholePath_AIThreatTractor,
            SetWormholePath_AIThreatRaid,
            SetWormholePath_AIThreatRetreat,
            SetWormholePath_AIIndFleet,
            SetWormholePath_OtherRaidKing,
            SetWormholePath_NPCAngryWanderMob,
            SetWormholePath_OutguardAttack,
            SetWormholePath_NPCSingleSpecialMission,
            SetWormholePath_NPCDirectedMob,
            SetWormholePath_UtilRaidKing,
            SetWormholePath_UtilDivideShips,
            SetWormholePath_UtilRaidSpecific,
            SetWormholePath_UtilRaidFromList,
            CreateSpeedGroup_Destroy,
            CreateSpeedGroup_ExoAttack,
            CreateSpeedGroup_FireteamAttack,
            CreatePreset,
            CreateNewPlanet,
            LinkPlanets,
            UnlinkPlanets,
            ChatCommand,
            JournalEntry,
            Chat,
            AddVassalMission,
            UpdateVassalMission,
            RemoveVassalMission,
            ResetFireteams,
            SetUnit_UIImportance,
            SetPlanet_UIImportance,
            RetrieveSpentScience,
            UpgradeSpireCity,
            UpgradeSpireSidekickCity,
            JournalEntryMarkedAsRead,
            BuildNecropolis,
            TransformNecrofleet,
            TransformApkalluFlagship,
            BuildStronghold,
            BuildSphere,
            BuildDrill,
            BuildScourgeStructure,
            TransformScourgeStructure,
            BuildStarbase,
            BuildMine,
            BuildArmadaStructure,
            BuildZiggurat,
            BuildDuru,
            SetWormholePath_UtilRaidAfterDisband,
            DecollideUnits_MoveManyToMany_NPC,
            UpdateEpistyleProduction,
            DepositDZResources,
            RequestTerminusBuild,
            ClaimMoon,
            SendSpireRelicToLocation,
            SendSpireSidekickRelicToLocation,
            Stop,
            Length,
        }

        protected static void Helper_GetListOfEntitiesWithSomeAsNull( GameCommand command, List<SafeSquadWrapper> ListToFill )
        {
            ListToFill.Clear();

            foreach ( GameEntity_Squad entity in command.RelatedSquads() )
            {
                if ( entity == null || entity.TypeData == null || entity.Planet == null || entity.ToBeRemovedAtEndOfThisFrame || entity.HasBeenRemovedFromSim )
                {
                    ListToFill.Add( null ); //this seems silly, but we need to do this because otherwise things that match entities to points (or whatever) in matching arrays won't match
                    continue;
                }
                ListToFill.Add( entity );
            }
        }

        protected static bool ShouldSkipEntityForPlanetOrderMismatch( GameCommand command, GameEntity_Squad entity, bool AllowQueuedDestinationPlanet, string CommandDebugName )
        {
            if ( command.PlanetOrderWasIssuedFrom < 0 || command.PlanetOrderWasIssuedFrom == entity.Planet.Index )
                return false;

            Planet destinationPlanet = null;
            if ( AllowQueuedDestinationPlanet && command.ToBeQueued )
            {
                destinationPlanet = entity.GetDestinationPlanet();
                if ( destinationPlanet != null && destinationPlanet.Index == command.PlanetOrderWasIssuedFrom )
                    return false;
            }

            if ( command.FromActualInputEventOfPlayerID < 255 || command.CameFromMultiplayerClientConnectionIndex < 255 )
            {
                lock ( planetGateDropLogLock )
                {
                    planetGateDropsSinceLastLog++;
                    float now = ArcenTime.TimeSinceStartF;
                    if ( lastLoggedPlanetGateDropRealtime < 0 || now - lastLoggedPlanetGateDropRealtime >= 5f )
                    {
                        int dropsToReport = planetGateDropsSinceLastLog;
                        planetGateDropsSinceLastLog = 0;
                        lastLoggedPlanetGateDropRealtime = now;
                        ArcenDebugging.ArcenDebugLogSingleLine( "MP planet-order gate dropped command=" + CommandDebugName +
                            " type=" + ( command.TypeData == null ? "null" : command.TypeData.InternalName ) +
                            " entity=" + entity.PrimaryKeyID +
                            " entityPlanet=" + entity.Planet.Index +
                            " issuedFromPlanet=" + command.PlanetOrderWasIssuedFrom +
                            " queued=" + command.ToBeQueued +
                            " destinationPlanet=" + ( destinationPlanet == null ? -1 : destinationPlanet.Index ) +
                            " dropsSinceLastLog=" + dropsToReport +
                            " fromPlayer=" + command.FromActualInputEventOfPlayerID +
                            " fromClientConnection=" + command.CameFromMultiplayerClientConnectionIndex,
                            Verbosity.DoNotShow );
                    }
                }
            }

            return true;
        }
    }
}
