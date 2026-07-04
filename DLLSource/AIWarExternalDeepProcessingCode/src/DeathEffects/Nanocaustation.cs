using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /* Used to do the actual zombification by the Nanocaust Guns from the XML */
    public class DeathEffect_Nanocaustation : IDeathEffectImplementation
    {
        public DeathEffectType Type;
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return "Nanocaustation"; 
        }
        
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained, 
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            var hostCtx = Context?.GetHostOnlyContext();
            if ( hostCtx == null )
                return;
            
            if ( FactionResponsibleForTheDeathEffect == null )
                return;
            
            if ( Entity == null )
                return;
            
            int numTimesToProc = ThisDeathEffectDamageSustained / this.Type.Scale;
            numTimesToProc = Math.Min(NumShipsDying, numTimesToProc);
            
            int numShipsBefore = IsFromOnlyPartOfStackDying ? Entity.ShipCount+NumShipsDying : NumShipsDying;
            //LOG.Msg("{0}x {1} stack handling death of {2}x ships", numShipsBefore, Entity.ToStringWithOwner(), NumShipsDying);
            //LOG.Msg("  {0}x {1} points = {2}x procs, and {3} rem points", ThisDeathEffectDamageSustained, Type.GetShortDisplayName(), numTimesToProc, ThisDeathEffectDamageSustained-(numTimesToProc*this.Type.Scale));

            if (numTimesToProc <= 0)
                return;
            
            // we have consumed some amount of death effect points
            // so subtract that, this value is returned via ref
            ThisDeathEffectDamageSustained -= numTimesToProc * this.Type.Scale;

            Faction destinationFaction = FactionResponsibleForTheDeathEffect;
            GameEntityTypeData EntityTypeToCreate = Entity.TypeData;

            //if we are throttling, nanocaustation will fail sometimes to prevent too many units from appearing
            if ( Entity.TypeData.IsStrikecraft && 
                 FactionUtilityMethods.Instance.ShouldFactionsThrottle() && 
                 Context.RandomToUse.Next(0, 100) < 50 )
            {
                return;
            }

            if ( destinationFaction.SpecialFactionData.InternalName == "Nanocaust" &&
                 (Entity.TypeData.IsTurret || Entity.TypeData.IsNonTurretDefense ||
                  Entity.TypeData.SpecialType == SpecialEntityType.GuardPost ||
                  Entity.TypeData.SpecialType == SpecialEntityType.DireGuardPost) &&
                 (Entity.GetFactionTypeSafe() == FactionType.AI ||
                  Entity.GetSpecialFactionDataInternalName_Safe() == "DarkZenith" ||
                  Entity.GetSpecialFactionDataInternalName_Safe() == "DarkZenithSvikari" ||
                  Entity.GetSpecialFactionDataInternalName_Safe() == "ZenithArchitrave") )
            {
                //some turrets can be taken by the Nanocaust
                EntityTypeToCreate = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NanocaustTurret" );
            }

            if ( !Entity.TypeData.IsMobileCombatant && EntityTypeToCreate == Entity.TypeData )
                return; //this isn't a mobile combatant and its not a turret for the nanocaust, so skip

            if ( (Entity.GetImmuneToCapture() || Entity.TypeData.GetHasTag( "AIScourge" )) &&
                 EntityTypeToCreate == Entity.TypeData )
            {
                //These are units that generally can't be Nanocausted, but the Nanocaust itself
                //can generate new ship variants from them
                if ( !(destinationFaction.SpecialFactionData.InternalName == "Nanocaust") )
                    return; //if this isn't the Nanocaust, no bonus ships
                if ( Entity.GetFactionTypeSafe() == FactionType.Player )
                    return; //don't give bonus ships to the Nanocaust from player units
                if ( Entity.GetSpecialFactionDataInternalName_Safe() == "ZenithDysonSphere" ||
                     Entity.GetSpecialFactionDataInternalName_Safe() == "DarkSpire" ||
                     Entity.GetSpecialFactionDataInternalName_Safe() == "ZenithArchitrave" )
                    return; //In general, factions that can create essentially limitless units shouldn't be nanocausted for balance reasons

                //If this is a non-nanocaustifiable unit, give the Nanocaust instead something interesting
                string tag;
                if ( Entity.TypeData.IsStrikecraft || Entity.TypeData.GetHasTag( "ScourgeWarriorBase" ) ||
                     Entity.TypeData.GetHasTag( "AIScourge" ) )
                {
                    tag = "NanocaustBonusWeak";
                }
                else if ( Entity.TypeData.SpecialType == SpecialEntityType.AIGuardian ||
                          Entity.TypeData.GetHasTag( "ScourgeWarrior" ) ||
                          Entity.TypeData.GetHasTag( "DysonTierOne" ) )
                {
                    tag = "NanocaustBonusMedium";
                }
                else if ( Entity.TypeData.SpecialType == SpecialEntityType.AIDireGuardian ||
                          Entity.TypeData.GetHasTag( "ExtragalacticWar" ) ||
                          Entity.TypeData.GetHasTag( "AIScourgeLarge" ) ||
                          Entity.TypeData.GetHasTag( "AIGolem" ) ||
                          Entity.TypeData.GetHasTag( "AstroTrain" ) ||
                          Entity.TypeData.GetHasTag( "DysonTierTwo" ) ||
                          Entity.TypeData.GetHasTag( "AIDragon" ) ||
                          Entity.TypeData.GetHasTag( "NanocaustsToLich" ) )
                {
                    tag = "NanocaustBonusScary";
                }
                else
                {
                    tag = "NanocaustBonusMedium"; //just in case, use this
                }
                
                EntityTypeToCreate = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( hostCtx, tag );
            }

            //The faction responsible for the death effect gets the ship (except players only get Zombies)

            PlanetFaction factionForNewEntity = Entity.Planet.GetPlanetFactionForFaction( destinationFaction );
            GameEntity_Squad zombie = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( factionForNewEntity, EntityTypeToCreate, Entity.CurrentMarkLevel,
                factionForNewEntity.Faction.LooseFleet, 0, Entity.WorldLocation, hostCtx, "DeathEffect-Nanocausted" );

            if ( zombie != null )
            {
                zombie.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                zombie.SetShipCount(numTimesToProc);
                zombie.NumTimesZombified = Entity.NumTimesZombified + 1;
                
                //this is okay because it will get caught in the fast-blast sync of being new
                int antiPingPongDamage = Entity.NumTimesZombified * (zombie.GetMaxHullPoints() / 7);
                zombie.TakeDamageDirectly( antiPingPongDamage, null, null, DamageSource.BeingScrapped, hostCtx );
            
                if ( Entity.TypeData.GetHasTag("Elderling") )
                {
                    //Elderlings come back to life at less than full health for balance reasons, per Zeus' balance suggestion
                    int elderlingStartingHealthDivisor = 2; //note: this can't be 100
                    int damage = Entity.GetMaxHullPoints() / elderlingStartingHealthDivisor;
                    zombie.TakeDamageDirectly( damage, null, null, DamageSource.BeingScrapped, hostCtx ); //this is okay because it will get caught in the fast-blast sync of being new
                }

                zombie.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //not sure if breaks in multiplayer.
            }
        }
    }
}
