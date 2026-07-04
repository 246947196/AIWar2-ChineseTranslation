using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum ExoGalacticAttackType
    {
        Normal,
        BiggestLeadShipPhysicallyPossible, //not used yet
        Reconquest //the ExoLeader is always a Usurper
    }
    public enum ExoUnitType
    {
        Strikecraft,
        Guardians,
        DireGuardians,
        ExoLeaders,
    }
    public class ExoOptions : ConcurrentPoolable<ExoOptions>, IProtectedListable
    {
        public readonly List<SafeSquadWrapper> Targets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "ExoOptions-Targets" );
        public int AttackStrength;
        public Faction spawningFaction;
        public Faction invokingFaction; //can be different (like the Fallen Spire might have the AI send an exo)
        public ExoGalacticAttackType type;
        public int distanceFromTargetOverride;
        public string exoText;
        public string newExoLeaderTag; //override the default ExoLeader tag. Note that perhaps this needs additional options to allow for customized exos
        public Planet ForceOrigin;
        public readonly List<ExoUnitType> UnitBlocksToUse = List<ExoUnitType>.Create_WillNeverBeGCed( 12, "ExoOptions-UnitBlocksToUse" );

        //IMPORTANT: any additions to the above need to have a clear entry in SetDefaults!

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ExoOptions()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ExoOptions" );
            RefTracker.IncrementObjectCount();
            SetDefaults();
        }

        private static readonly ConcurrentPool<ExoOptions> Pool = new ConcurrentPool<ExoOptions>( "ExoOptions", 3000, 
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ExoOptions(); } );

        private static ExoOptions GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public ExoOptions CreateNewForPool()
        {
            return new ExoOptions();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            SetDefaults();
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        public void SetDefaults()
        {
            this.Targets.Clear();
            this.AttackStrength = 0;
            this.spawningFaction = null;
            this.invokingFaction = null;
            this.type = ExoGalacticAttackType.Normal;
            this.distanceFromTargetOverride = -1;
            this.exoText = string.Empty;
            this.newExoLeaderTag = string.Empty;
            this.ForceOrigin = null;
            this.UnitBlocksToUse.Clear();
        }

        public static ExoOptions CreateWithDefaults( GameEntity_Squad target, int strength, Faction spawningFaction, Faction invokingFaction )
        {
            ExoOptions options = GetFromPoolOrCreate();
            options.Targets.Add( target );
            options.AttackStrength = strength;
            options.spawningFaction = spawningFaction;
            options.invokingFaction = invokingFaction;
            options.UnitBlocksToUse.Add( ExoUnitType.Strikecraft );
            options.UnitBlocksToUse.Add( ExoUnitType.Guardians );
            options.UnitBlocksToUse.Add( ExoUnitType.ExoLeaders );
            return options;
        }

        public static ExoOptions CreateWithDefaults( List<SafeSquadWrapper> targets, int strength, Faction spawningFaction, Faction invokingFaction )
        {
            ExoOptions options = GetFromPoolOrCreate();
            for ( int i = 0; i < targets.Count; i++ )
                options.Targets.Add( targets[i] );
            options.AttackStrength = strength;
            options.spawningFaction = spawningFaction;
            options.invokingFaction = invokingFaction;
            options.UnitBlocksToUse.Add( ExoUnitType.Strikecraft );
            options.UnitBlocksToUse.Add( ExoUnitType.Guardians );
            options.UnitBlocksToUse.Add( ExoUnitType.ExoLeaders );
            return options;
        }

        public static ExoOptions CreateAllUnset()
        {
            ExoOptions options = GetFromPoolOrCreate();
            options.UnitBlocksToUse.Add( ExoUnitType.Strikecraft );
            options.UnitBlocksToUse.Add( ExoUnitType.Guardians );
            options.UnitBlocksToUse.Add( ExoUnitType.ExoLeaders );
            return options;
        }
    }
}
