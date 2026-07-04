using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    internal static class DeathEffect_Metabolism_Base
    {
        public static void HandleDeath(
            GameEntity_Squad Entity, 
            ref int ThisDeathEffectDamageSustained,
            Faction FactionResponsibleForTheDeathEffect, 
            int NumShipsDying, 
            ArcenSimContextAnyStatus Context,
            DeathEffectType Type,
            float ConversionRatio,
            int MaxGainPerShip_Metal,
            int MaxGainPerShip_AIBudget)
        {
            var hostCtx = Context?.GetHostOnlyContext();
            if ( hostCtx == null )
                return;
            
            if ( FactionResponsibleForTheDeathEffect == null )
                return;

            //20% of the metal cost of the entity
            float metalGainPerShip = Entity.DataForMark.MetalCost * ConversionRatio;
            
            //max 5000
            if ( metalGainPerShip > MaxGainPerShip_Metal )
                metalGainPerShip = MaxGainPerShip_Metal;
            
            int numShipsAffected = ThisDeathEffectDamageSustained / Type.Scale;

            int numTimesToProc = Math.Min(NumShipsDying, numShipsAffected);
            if (numTimesToProc <= 0)
                return;
            
            // we have consumed some amount of death effect points
            // so subtract that, this value is returned via ref
            ThisDeathEffectDamageSustained -= numTimesToProc * Type.Scale;
            
            int metalGain = (int)(metalGainPerShip * numTimesToProc);
            
            FactionResponsibleForTheDeathEffect.StoredMetal += metalGain;
            if ( FactionResponsibleForTheDeathEffect.StoredMetal > FactionResponsibleForTheDeathEffect.MetalStorage )
                FactionResponsibleForTheDeathEffect.StoredMetal = (FInt)FactionResponsibleForTheDeathEffect.MetalStorage;
            
            FactionResponsibleForTheDeathEffect.TotalMetalMetabolized += metalGain;
            (Entity.GetFleetOrNull_Safe()?.BaseInfo as IFleetMetrics)?.OnFleetMetabolization( metalGain, Entity );
            //ThisDeathEffectDamageSustained = 0;
            
            //ArcenDebugging.ArcenDebugLogSingleLine("Gained " + metalToGain + " metal", Verbosity.DoNotShow );
            if ( FactionResponsibleForTheDeathEffect.Type == FactionType.AI )
            {
                //the AI doesn't used StoredMetal, so put it in the reinforcements budget
                AISentinelsCoreData factionExternal = FactionResponsibleForTheDeathEffect.TryGetAISentinelsCoreData()?.SentinelInfo;
                
                // this seems like really really small value to clamp to
                // why not compute a proper metal-cost to ai-cost conversion rate?
                // also.. this isn't scaling AT ALL with the number of ships dying
                if ( metalGain > MaxGainPerShip_AIBudget )
                    metalGain = MaxGainPerShip_AIBudget;
                
                factionExternal.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement] += metalGain;
            }
        }
    }
    
    public class DeathEffect_Metabolization : IDeathEffectImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "DeathEffect_Metabolizations" );

        public DeathEffectType Type;
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public DeathEffect_Metabolization()
        {
            RefTracker.IncrementObjectCount();
        }
        
        public override string ToString()
        {
            return "Metabolization"; //makes exports more brief and clear
        }
        
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained, 
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            DeathEffect_Metabolism_Base.HandleDeath(Entity, ref ThisDeathEffectDamageSustained, FactionResponsibleForTheDeathEffect, NumShipsDying, Context, Type, 0.2f, 5000, 100);
        }
    }

    public class DeathEffect_GreaterMetabolization : IDeathEffectImplementation
    {
        public DeathEffectType Type;
        
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "DeathEffect_GreaterMetabolizations" );
        public DeathEffect_GreaterMetabolization()
        {
            RefTracker.IncrementObjectCount();
        }
        
        public override string ToString()
        {
            return "GreaterMetabolization";
        }
        
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained,
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            DeathEffect_Metabolism_Base.HandleDeath(Entity, ref ThisDeathEffectDamageSustained, FactionResponsibleForTheDeathEffect, NumShipsDying, Context, Type, 0.5f, 50000, 400);
        }
    }
}
