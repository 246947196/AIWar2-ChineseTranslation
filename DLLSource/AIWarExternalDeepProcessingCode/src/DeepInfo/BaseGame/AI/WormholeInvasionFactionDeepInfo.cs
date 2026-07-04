using Arcen.AIW2.Core;
using System;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class WormholeInvasionFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public WormholeInvasionFactionBaseInfo BaseInfo;
        public static WormholeInvasionFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<WormholeInvasionFactionBaseInfo>();
            Instance = this;
        }


        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 15;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {

            int debugCode = 0;
            try{
                debugCode = 100;
                bool debug = false;

                if ( BaseInfo.DebugSpawnInvasion )
                {
                    WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults( BaseInfo.DebugInvasionStrength, this.AttachedFaction );
                    options.ForceInvasionLaunch = true;
                    bool wasLaunched = WormholeInvasionManager.LaunchWormholeInvasion( options, Context );
                    BaseInfo.DebugSpawnInvasion = false;
                    BaseInfo.DebugInvasionStrength = -1;
                }
                
                for ( int i = BaseInfo.IncomingInvasionList.Count -1; i >= 0; i-- )
                {
                    debugCode = 200;
                    WormholeInvasionData data = BaseInfo.IncomingInvasionList[i];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Processing: " + data.ToString(), Verbosity.DoNotShow );
                    if ( data.ProjectorAppearanceTime == World_AIW2.Instance.GameSecond )
                    {
                        debugCode = 300;
                        if ( !data.HasSpawnedProjector)
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine("create a wormhole projector", Verbosity.DoNotShow );
                            GameEntity_Squad projector = CreateWormholeProjector( data, Context );
                            if ( projector == null )
                                throw new Exception("Failed to spawn projector. This shouldn't be possible, since deep is only executed on the host");
                            data.WormholeProjector = projector;
                            data.WormholeProjectorId = projector.PrimaryKeyID;
                            CreateProjectorDefenses( projector, data, Context );
                            data.HasSpawnedProjector = true; //this doesn't seem to work?
                            BaseInfo.IncomingInvasionList[i] = data; //assignment needed since this is a struct
                        }
                    }
                    debugCode = 400;
                    if ( data.PlanetLinkTime == World_AIW2.Instance.GameSecond )
                    {
                        //set the planets to be linked. See the Miners and Nomad code for some examples
                        //I think using the LinkPlanets game commmand is the way to go, then in the "DoOnDeath" code
                        //remove the link with a new UnlinkPlanets game command
                        debugCode = 500;
                        GameCommand createCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.LinkPlanets], GameCommandSource.AnythingElse );
                        createCommand.RelatedIntegers.Add(data.InvasionStartPlanet.Index);
                        createCommand.RelatedIntegers.Add(data.InvasionDestinationPlanet.Index);
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, createCommand, false );
                    }
                    debugCode = 600;
                    for ( int j = data.WaveData.Count - 1; j >= 0; j-- )
                    {
                        debugCode = 700;
                        if ( data.WaveData[j].TimeForWave == World_AIW2.Instance.GameSecond )
                        {
                            debugCode = 800;
                            //Launch the wave, then dequeue it

                            //This is what the Exo code uses, so figure out how to adapt this
                            Faction aiFaction = data.ResponsibleAIFaction;
                            bool canUseRelentlessAIWaveFaction = true;
                            WavesHelper.Instance.DeployComposition( Context, aiFaction, data.WormholeProjector, (short)-1, data.WaveData[j].ShipsInWave, null, ArcenPoint.ZeroZeroPoint, null, canUseRelentlessAIWaveFaction, debug );
                            //now that we've sent the wave, remove it
                            data.WaveData.RemoveAt( j, true );
                            BaseInfo.IncomingInvasionList[i] = data; //assignment needed since this is a struct
                        }
                    }
                    debugCode = 2000;
                }
                List<SafeSquadWrapper> Projectors = this.BaseInfo.WormholeProjectors.GetDisplayList();
                for ( int i = 0; i < Projectors.Count; i++ )
                {
                    GameEntity_Squad projector = Projectors[i].GetSquad();
                    if ( projector == null )
                        continue;
                    WormholeInvasionPerUnitBaseInfo data = projector.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
                    if ( data.TimeToRemoveUnit > 0 &&
                         World_AIW2.Instance.GameSecond > data.TimeToRemoveUnit )
                    {
                        GameCommand createCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnlinkPlanets], GameCommandSource.AnythingElse );
                        createCommand.RelatedIntegers.Add(projector.Planet.Index);
                        createCommand.RelatedIntegers.Add(data.LinkedPlanetIdx);
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, createCommand, false );
                        projector.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Wormhole Invasion Stage 3 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        private GameEntity_Squad CreateWormholeProjector( WormholeInvasionData data, ArcenHostOnlySimContext Context )
        {
            //create a wormhole projector on data.StartPlanet, then set the data.WormholeProjectorId to that unit's PrimaryKeyID
            //and also set data.WormholeProjector to the new unit
            Planet planet = data.InvasionStartPlanet;
            PlanetFaction pFaction = data.InvasionStartPlanet.GetPlanetFactionForFaction( this.AttachedFaction );
            GameEntityTypeData projectorData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "LivingWormholeProjector" );
            if ( projectorData == null )
                throw new Exception("No LivingWormholeProjector xml found");
            ArcenPoint spawnLocation = data.InvasionStartPlanet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, projectorData, Engine_AIW2.Instance.CombatCenter, FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 650 ) );

            GameEntity_Squad projector = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, projectorData, 1, this.AttachedFaction.LooseFleet, 0, spawnLocation, Context, "WormholeProjector" );
            WormholeInvasionPerUnitBaseInfo perUnitData = projector.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
            perUnitData.LinkedPlanetIdx = data.InvasionDestinationPlanet.Index;
            return projector;

       //   GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, wormholeData, wormholePoint, Context );
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.GetHasTag("LivingWormholeProjector") )
            {
                //spawn a destabilized wormhole projector
                Planet planet = entity.Planet;
                PlanetFaction pFaction = entity.PlanetFaction;
                GameEntityTypeData projectorData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DestabilizedWormholeProjector" );
                if ( projectorData == null )
                    throw new Exception("No LivingWormholeProjector xml found");
                ArcenPoint spawnLocation = entity.WorldLocation;

                GameEntity_Squad projector = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, projectorData, 1, this.AttachedFaction.LooseFleet, 0, spawnLocation, Context, "WormholeProjectorAfterDeath" );
                WormholeInvasionPerUnitBaseInfo oldData = entity.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
                WormholeInvasionPerUnitBaseInfo newData = projector.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
                newData.LinkedPlanetIdx = oldData.LinkedPlanetIdx;
                newData.TimeToRemoveUnit = World_AIW2.Instance.GameSecond + 60;
            }

        }
        private void CreateProjectorDefenses( GameEntity_Squad projector, WormholeInvasionData data, ArcenHostOnlySimContext Context )
        {
            //Using data.WormholeProjector (which is set in CreateWormholeProjector), we also seed the turrets and guardians
            int debugCode = 0;
            try{
                debugCode = 100;
                Planet planet = projector.Planet;
                bool debug = false;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Creating defenses for " + projector.ToStringWithPlanet() +". We are spawning " + data.TurretStrength + " turret strength and " + data.GuardStrength + " guardian strength", Verbosity.DoNotShow );

                Faction aiFaction = data.ResponsibleAIFaction;
                if ( aiFaction == null )
                    throw new Exception("No responsible AI faction found\n");
                PlanetFaction pFaction = projector.PlanetFaction;
                debugCode = 200;
                while ( data.TurretStrength > 0 )
                {
                    debugCode = 300;
                    GameEntityTypeData typeToBuy = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WormholeInvasionDefense" );
                    if ( typeToBuy == null )
                        throw new Exception("Could not find turret to buy for projector defenses");
                    ArcenPoint point = planet.GetSafePlacementPoint_AroundEntity( Context, typeToBuy, projector, FInt.FromParts( 0, 040 ), FInt.FromParts( 0, 150 ) );
                    debugCode = 350;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToBuy, aiFaction.CurrentGeneralMarkLevel, this.AttachedFaction.LooseFleet, 0, point, Context, "WormholeProjectorDefenses" );
                    debugCode = 370;
                    data.TurretStrength -= typeToBuy.CostForAIToPurchase;
                }
                data.TurretStrength = -1;
                while ( data.GuardStrength > 0 )
                {
                    debugCode = 400;
                    GameEntityTypeData typeToBuy = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WormholeInvasionGuardian" );
                    if ( typeToBuy == null )
                        throw new Exception("Could not find guardian to buy for projector defenses");
                    ArcenPoint point = planet.GetSafePlacementPoint_AroundEntity( Context, typeToBuy, projector, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
                    debugCode = 450;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToBuy, aiFaction.CurrentGeneralMarkLevel, this.AttachedFaction.LooseFleet, 0, point, Context, "WormholeProjectorDefenses" );
                    debugCode = 470;
                    data.GuardStrength -= typeToBuy.CostForAIToPurchase;
                    if ( newEntity != null )
                    {
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                        newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    }
                }
                data.GuardStrength = -1;

            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in CreateProjectorDefenses debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

    }
}
