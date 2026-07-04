using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class DeathEffect_Zombification : IDeathEffectImplementation
    {
        public DeathEffectType Type;
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "DeathEffect_Zombifications" );
        public DeathEffect_Zombification()
        {
            RefTracker.IncrementObjectCount();
        }
        public override string ToString()
        {
            return "Zombification"; //makes exports more brief and clear
        }
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained, 
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                var hostCtx = Context?.GetHostOnlyContext();
                if ( hostCtx == null )
                    return;
                
                if ( Entity == null )
                    return;
                
                if ( FactionResponsibleForTheDeathEffect == null )
                {
                    //LOG.Msg("Handle Zombification: {0}; FactionResponsibleForTheDeathEffect=null!", Entity.ToStringWithOwner() );
                    return;
                }
                
                var factionSpecialData = FactionResponsibleForTheDeathEffect.SpecialFactionData;
                if ( factionSpecialData == null )
                {
                    //LOG.Msg("Handle Zombification: {0}; FactionResponsibleForTheDeathEffect.SpecialFactionData=null!", Entity.ToStringWithOwner());
                    return;
                }
                
                var entityPlanet = Entity.Planet;
                if ( entityPlanet == null )
                    return;
                
                var typeData = Entity.TypeData;
                if ( typeData == null )
                    return;
                
                if (Entity.GetImmuneToCapture())
                {
                    //LOG.Msg("Handle Zombification: {0}; ReasonImmuneToCapture={0}!", Entity.ToStringWithOwner(), Entity.GetImmuneToCaptureReason());
                    return;
                }

                Faction zombieFactionToResult = null;

                //Warden fleets, hunter fleets and anti-player zombies also make anti-player zombies
                debugCode = 300;
                if ( FactionResponsibleForTheDeathEffect.Type == FactionType.AI ||
                     factionSpecialData.AlliedToAIByDefault ||
                     factionSpecialData.InternalName == "AntiPlayerZombie" ||
                     FactionUtilityMethods.Instance.IsACoreAISubFaction( FactionResponsibleForTheDeathEffect ) )
                {
                    debugCode = 400;
                    zombieFactionToResult = ZombieAntiPlayerFactionBaseInfo.Instance.AttachedFaction;
                }
                else 
                if ( FactionResponsibleForTheDeathEffect.Type == FactionType.Player ||
                     FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( FactionResponsibleForTheDeathEffect ) )
                {
                    zombieFactionToResult = ZombieAntiAIFactionBaseInfo.Instance.AttachedFaction;
                    
                    // Returns true if handled.
                    // If not handled, execution should continue as normal.
                    bool HandleSendToNecromancer(ref int zombify_damage_remaining)
                    {
                        debugCode = 500;
                        if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ZombiesGoToNecromancer" ) )
                            return false;

                        debugCode = 510;
                        Faction destFaction;
                        if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( FactionResponsibleForTheDeathEffect ) )
                            destFaction = FactionResponsibleForTheDeathEffect;
                        else
                            destFaction = FactionUtilityMethods.Instance.GetStrongestNecromancerFactionOnPlanet( entityPlanet );
                            
                        if ( destFaction == null )
                            return false;

                        debugCode = 530;
                        Fleet fleet = NecromancerEmpireFactionDeepInfo.GetBestMobileFleetForShip( Entity, FactionResponsibleForTheDeathEffect, hostCtx );
                        if ( fleet == null )
                            return false; 
                        
                        debugCode = 540;
                        GameEntity_Squad centerpieceOfFleet = fleet.Centerpiece.GetSquad();
                        if ( centerpieceOfFleet == null )
                            return false;
                        
                        debugCode = 520;
                        GameEntityTypeData EntityTypeToCreate = NecromancerEmpireFactionDeepInfo.GetShipToSummonViaNecromancy( Entity, fleet, hostCtx );
                        if ( EntityTypeToCreate == null )
                            return false;
                        
                        debugCode = 550;
                        PlanetFaction pFact = centerpieceOfFleet.PlanetFaction;
                        byte markLevel = destFaction.GetGlobalMarkLevelForShipLine( EntityTypeToCreate );
                        GameEntity_Squad newEnt = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFact, EntityTypeToCreate, markLevel,
                                    fleet, 0, centerpieceOfFleet.WorldLocation, hostCtx, "DeathEffect-ZombieToNecro" );
                            
                        if ( newEnt == null )
                            return false;
                            
                        debugCode = 560;
                        if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer ) )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Necromancy Practiced (regular player path) Spawned a " + EntityTypeToCreate.GetDisplayName() + " on " + newEnt.GetPlanetName_Safe() + " for fleet " + fleet.GetName() + " that has flagship " + centerpieceOfFleet.ToStringWithPlanet(), Verbosity.DoNotShow );
                            
                        newEnt.AddOrSetExtraStackedSquadsInThis( 0, true );
                        zombify_damage_remaining -= Type.Scale;
                            
                        for ( int i = 0; i < (NumShipsDying-1); i++ )
                        {
                            if ( zombify_damage_remaining <= 0 || 
                                 zombify_damage_remaining < Type.Scale )
                                break;
                            
                            newEnt.AddOrSetExtraStackedSquadsInThis( 1, false );
                            
                            //zombify only the number we can based on the damage total on there
                            zombify_damage_remaining -= Type.Scale;
                        }
                            
                        newEnt.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        
                        debugCode = 700;
                        
                        return true;
                    }
                    
                    if (HandleSendToNecromancer(ref ThisDeathEffectDamageSustained))
                        return;
                }
                else
                {
                    //Note that anti-ai zombies make anti-everyone zombies. I think this is desirable to limit the potential utility of
                    //
                    //                ArcenDebugging.ArcenDebugLogSingleLine("Faction " + FactionResponsibleForTheDeathEffect.Type + " " + FactionResponsibleForTheDeathEffect.Implementation.GetSpecialFactionName() + " just killed a " + typeData.GetDisplayName() + " on " + Entity.GetPlanetName_Safe() + " which becomes anti-everyone", Verbosity.DoNotShow );
                    zombieFactionToResult = ZombieAntiEveryoneFactionBaseInfo.Instance.AttachedFaction;
                }
                
                if ( zombieFactionToResult == null )
                    return;
                
                debugCode = 800;

                PlanetFaction entityPlanetFaction = Entity.PlanetFaction;
                if ( entityPlanetFaction == null )
                    return;

                if ( entityPlanetFaction.Faction == zombieFactionToResult ) //if a zombie would rejoin the same faction again, don't let it
                    return;

                PlanetFaction factionForNewEntity = entityPlanet.GetPlanetFactionForFaction( zombieFactionToResult );
                GameEntity_Squad zombie = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( factionForNewEntity, typeData, Entity.CurrentMarkLevel,
                    //zombies go to the loose fleet, not any particular fleet of the player or AI or anything
                    factionForNewEntity.Faction.LooseFleet, 0, Entity.WorldLocation, hostCtx, "DeathEffect-Zombification" );
                if ( zombie == null )
                    return;
                zombie.AddOrSetExtraStackedSquadsInThis( 0, true );

                zombie.NumTimesZombified = Entity.NumTimesZombified + 1;
                int antiPingPongDamage = Entity.NumTimesZombified * (zombie.GetMaxHullPoints() / 7);
                zombie.TakeDamageDirectly( antiPingPongDamage, null, null, DamageSource.BeingScrapped, hostCtx ); //this is okay because it will get caught in the fast-blast sync of being new

                //since damage builds up on a stack, reduce it
                ThisDeathEffectDamageSustained -= Type.Scale;
                
                //for each stacked entity that we killed that we can afford, add it in
                for ( int i = 0; i < (NumShipsDying-1); i++ )
                {
                    if ( ThisDeathEffectDamageSustained <= 0 || ThisDeathEffectDamageSustained < Type.Scale )
                        break;
                    zombie.AddOrSetExtraStackedSquadsInThis( 1, false );
                    //zombify only the number we can based on the damage total on there
                    ThisDeathEffectDamageSustained -= Type.Scale;
                }
                
                zombie.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                zombie.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //not sure if breaks in multiplayer.
            } 
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
    public class DeathEffect_Zombification_HostileToAll : IDeathEffectImplementation
    {
        public DeathEffectType Type;
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return "Zombification_HostileToAll"; //makes exports more brief and clear
        }
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained, 
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            var hostCtx = Context?.GetHostOnlyContext();
            if ( hostCtx == null ) //client
                return;
            if ( FactionResponsibleForTheDeathEffect == null )
                return;
            if (Entity.GetImmuneToCapture())
                return;
            
            Faction zombieFactionToResult = ZombieAntiEveryoneFactionBaseInfo.Instance.AttachedFaction;
            if ( zombieFactionToResult == null )
                return;

            PlanetFaction entityPlanetFaction = Entity.PlanetFaction;
            if ( entityPlanetFaction == null )
                return;

            if ( entityPlanetFaction.Faction == zombieFactionToResult ) //if a zombie would rejoin the same faction again, don't let it
                return;

            Planet entityPlanet = Entity.Planet;
            if ( entityPlanet == null )
                return;

            PlanetFaction factionForNewEntity = entityPlanet.GetPlanetFactionForFaction( zombieFactionToResult );
            GameEntity_Squad zombie = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( factionForNewEntity, Entity.TypeData, Entity.CurrentMarkLevel,
                //zombies go to the loose fleet, not any particular fleet of the player or AI or anything
                factionForNewEntity.Faction.LooseFleet, 0, Entity.WorldLocation, hostCtx, "DeathEffect-Zombified" );
            if ( zombie == null )
                return; //client
            
            zombie.NumTimesZombified = Entity.NumTimesZombified + 1;
            int antiPingPongDamage = Entity.NumTimesZombified * (zombie.GetMaxHullPoints() / 7);
            zombie.TakeDamageDirectly( antiPingPongDamage, null, null, DamageSource.BeingScrapped, hostCtx ); //this is okay because it will get caught in the fast-blast sync of being new

            zombie.AddOrSetExtraStackedSquadsInThis( 0, true );
            
            //since damage builds up on a stack, reduce it
            ThisDeathEffectDamageSustained -= Type.Scale;
            
            //for each stacked entity that we killed that we can afford, add it in
            for ( int i = 0; i < (NumShipsDying - 1); i++ )
            {
                if ( ThisDeathEffectDamageSustained <= 0 || ThisDeathEffectDamageSustained < Type.Scale )
                    break;
                zombie.AddOrSetExtraStackedSquadsInThis( 1, false );
                //zombify only the number we can based on the damage total on there
                ThisDeathEffectDamageSustained -= Type.Scale;
            }
            zombie.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            zombie.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //not sure if breaks in multiplayer.
        }
    }
}
