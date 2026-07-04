using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class NomadPlanetsFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public NomadPlanetsFactionBaseInfo BaseInfo;
        public static NomadPlanetsFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<NomadPlanetsFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            ExoTargets.Clear();
        }

        public static readonly List<SafeSquadWrapper> ExoTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "NomadPlanetsFactionDeepInfo-ExoTargets" );

        /* This class does some utility work, but the main work is done in the EntitySimLogic. That's where the planets actually move;
           we need to do it there so as to not have tons of racing threads.   */

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.TypeData.Type == PlanetType.Nomad )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DisabledNomadNexus" );
                    planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                }
            }
        }

        private readonly List<SafeSquadWrapper> humanHomeCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "NomadPlanetsFactionDeepInfo-humanHomeCommandStations" );
        private readonly List<SafeSquadWrapper> otherTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "NomadPlanetsFactionDeepInfo-otherTargets" );

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            //Note that the actual Nomad movement is done in EntitySimLogic to avoid racing threads
            ExoTargets.Clear();
            bool foundHomeworldTarget = false; //if we are against a homeworld (or bastion world)
            GameEntity_Squad foundCrashingNexus = null;
            List<SafeSquadWrapper> nexuses = BaseInfo.NomadPlanetNexuses.GetDisplayList();
            for ( int i = 0; i < nexuses.Count; i++ )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_NomadPlanets_ActiveNexus", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                GameEntity_Squad nexus = nexuses[i].GetSquad();
                if ( nexus == null )
                    continue;
                if ( nexus.Planet.NomadTargetPlanetIdx == -1 )
                    continue;
                
                if ( nexus.Planet.SecondsTillNomadCrashes > 0 )
                    nexus.Planet.SecondsTillNomadCrashes--;
                
                if (! BaseInfo.MoveIntervalForCrash.ContainsKey(nexus.Planet.Index) )
                {
                    Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex(nexus.Planet.NomadTargetPlanetIdx);
                    int distance = Mat.DistanceBetweenPointsImprecise( nexus.Planet.GalaxyLocation, targetPlanet.GalaxyLocation );
                    int totalCrashTime = NomadPlanetsFactionBaseInfo.Instance == null ? -1 : NomadPlanetsFactionBaseInfo.Instance.GetCrashTime( nexus.Planet, targetPlanet, distance);
                    int numHops = distance / BaseInfo.DistanceToMoveForCrash;
                    BaseInfo.MoveIntervalForCrash[nexus.Planet.Index] = 30;
                    nexus.Planet.TimeForNextMove = World_AIW2.Instance.GameSecond + BaseInfo.MoveIntervalForCrash[nexus.Planet.Index];
                    BaseInfo.EstimatedTimeToCrash = totalCrashTime;
                    if ( targetPlanet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                         targetPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                        foundHomeworldTarget = true;
                }
                
                foundCrashingNexus = nexus;
                ExoTargets.Add(nexus);
            }
            
            if (foundCrashingNexus == null )
            {
                BaseInfo.TimeForNextExoSpawn = -1;
                BaseInfo.TimeNomadCrashStarted = -1;
                BaseInfo.EstimatedTimeToCrash = -1;
                return;
            }
            
            if ( BaseInfo.TimeForNextExoSpawn == -1 )
            {
                BaseInfo.TimeForNextExoSpawn = World_AIW2.Instance.GameSecond;
                BaseInfo.TimeNomadCrashStarted = World_AIW2.Instance.GameSecond;
                //the Time Estimate is set above
            }

            if ( World_AIW2.Instance.GameSecond >= BaseInfo.TimeForNextExoSpawn )
            {
                //Spawn an Exo during the crash
                int timeElapsed = World_AIW2.Instance.GameSecond - BaseInfo.TimeNomadCrashStarted;
                BaseInfo.TimeForNextExoSpawn = World_AIW2.Instance.GameSecond + BaseInfo.ExoResponseInterval;
                ExoGalacticAttackManager.GetAllHumanHomeCommandStations( humanHomeCommandStations );
                ExoTargets.AddRange( humanHomeCommandStations );
                
                ExoGalacticAttackManager.GetTastyPlayerTargets( otherTargets, omitHomeworlds:true );
                if ( otherTargets.Count > 0 && timeElapsed > 100 )
                {
                    //sometimes we pick some other things to go after as well,
                    //to add variety to the attacks. Only start doing this after we've sent a few waves already
                    int random =  Context.RandomToUse.Next(0, 100);
                    if ( random < 30 )
                        ExoTargets.Add(otherTargets[0]);
                    else if ( random < 50 && otherTargets.Count > 1 )
                    {
                        ExoTargets.Add( otherTargets[Context.RandomToUse.Next(0, otherTargets.Count)] );
                        ExoTargets.Add( otherTargets[Context.RandomToUse.Next(0, otherTargets.Count)] );
                    }
                }

                //figure out which AI to use, and which
                Planet firstTargetPlanet = World_AIW2.Instance.GetPlanetByIndex(ExoTargets[0].Planet.NomadTargetPlanetIdx);
                Faction targetFaction = firstTargetPlanet.GetControllingFaction();
                if ( targetFaction.Type != FactionType.AI ) //in case someone killed the AI while we are en route
                    targetFaction = World_AIW2.GetRandomAIFaction( Context );

                AISentinelsFactionDeepInfo aiDeepInfo = targetFaction.GetAISentinelsDeepLogic();
                int WaveSize = aiDeepInfo.BaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                FInt playerStrength = FactionUtilityMethods.Instance.GetOverallPowerLevelOfPlayersAndAlliedFactions();
                if ( playerStrength < FInt.One )
                    playerStrength = FInt.One;
                FInt multiplier = BaseInfo.ExoResponseStrengthPerPlayerPowerLevel * playerStrength;
                int strength = (WaveSize * multiplier).IntValue;
                //the nomad exos take some time to really get going; this lets a player use the nomad crash hack
                //to move a nomad, then break it.
                if ( timeElapsed < 21 )
                    strength /= 10;
                else if ( timeElapsed < 41 )
                    strength /= 5;
                else if ( timeElapsed < 61 )
                    strength /= 2;
                else if ( timeElapsed < 120 )
                    strength =  (strength * 3) / 2;
                else
                    strength *= 2;
                //Put in a failsafe for extremely heavily defended planets; crank up the strength if it seems relevant
                int strengthOnNomadPlanet = foundCrashingNexus.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + foundCrashingNexus.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( timeElapsed > 60 && strength < strengthOnNomadPlanet / 10 )
                    strength = strengthOnNomadPlanet / 10;
                

                if ( !foundHomeworldTarget )
                    strength /= 2; //if not crashing into a homeworld or bastion, reduced response strength
//                ArcenDebugging.ArcenDebugLogSingleLine("It is " + World_AIW2.Instance.GameSecond + " and the next exo is in " + BaseInfo.TimeForNextExoSpawn+ ", time elapsed " + timeElapsed + ". This wave is strength " + strength + ", with mult " + multiplier, Verbosity.DoNotShow );
                ExoOptions options = ExoOptions.CreateWithDefaults(ExoTargets, strength, targetFaction, AttachedFaction);
                if ( Context.RandomToUse.Next(0, 100) < BaseInfo.ExoChanceOfIncludingExtragalactic )
                    options.newExoLeaderTag = "ExtragalacticWar";
                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
            }
            if ( AttachedFaction.HasBeenSeenByPlayer )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_NomadPlanets_InitialDiscovery", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
            if ( entity == null )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( entity.TypeData.GetHasTag( "DisabledNomadNexus" ) || entity.TypeData.GetHasTag( "NomadNexus" ) )
                {
                    debugStage = 200;
                    //If the nomad nexus is destroyed, the nomad is "broken" and stops moving
                    entity.Planet.IsDisabledNomad = true;
                    entity.Planet.NomadTargetPlanetIdx = -1;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_NomadPlanets.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
