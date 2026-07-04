using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    #region TargetListPlanning(s)
    public class TargetListPlanning_Player : TargetListPlanningRoot, IBetweenMapGenPoolable<TargetListPlanning_Player>
    {
        private TargetListPlanning_Player()
        {
            this.TargetingFor = FactionTargetPlanningGroup.Player;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<TargetListPlanning_Player> Pool = BetweenMapGenPool<TargetListPlanning_Player>.Create_WillNeverBeGCed( "TargetListPlanning_Player", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TargetListPlanning_Player(); } );

        public static TargetListPlanning_Player GetFromPoolOrCreate()
        {
            TargetListPlanning_Player context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class TargetListPlanning_NPC_A : TargetListPlanningRoot, IBetweenMapGenPoolable<TargetListPlanning_NPC_A>
    {
        private TargetListPlanning_NPC_A()
        {
            this.TargetingFor = FactionTargetPlanningGroup.NPC_A;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<TargetListPlanning_NPC_A> Pool = BetweenMapGenPool<TargetListPlanning_NPC_A>.Create_WillNeverBeGCed( "TargetListPlanning_NPC_A", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TargetListPlanning_NPC_A(); } );

        public static TargetListPlanning_NPC_A GetFromPoolOrCreate()
        {
            TargetListPlanning_NPC_A context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class TargetListPlanning_NPC_B : TargetListPlanningRoot, IBetweenMapGenPoolable<TargetListPlanning_NPC_B>
    {
        private TargetListPlanning_NPC_B()
        {
            this.TargetingFor = FactionTargetPlanningGroup.NPC_B;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<TargetListPlanning_NPC_B> Pool = BetweenMapGenPool<TargetListPlanning_NPC_B>.Create_WillNeverBeGCed( "TargetListPlanning_NPC_B", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TargetListPlanning_NPC_B(); } );

        public static TargetListPlanning_NPC_B GetFromPoolOrCreate()
        {
            TargetListPlanning_NPC_B context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class TargetListPlanning_NPC_C : TargetListPlanningRoot, IBetweenMapGenPoolable<TargetListPlanning_NPC_C>
    {
        private TargetListPlanning_NPC_C()
        {
            this.TargetingFor = FactionTargetPlanningGroup.NPC_C;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<TargetListPlanning_NPC_C> Pool = BetweenMapGenPool<TargetListPlanning_NPC_C>.Create_WillNeverBeGCed( "TargetListPlanning_NPC_C", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TargetListPlanning_NPC_C(); } );

        public static TargetListPlanning_NPC_C GetFromPoolOrCreate()
        {
            TargetListPlanning_NPC_C context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class TargetListPlanning_NPC_D : TargetListPlanningRoot, IBetweenMapGenPoolable<TargetListPlanning_NPC_D>
    {
        private TargetListPlanning_NPC_D()
        {
            this.TargetingFor = FactionTargetPlanningGroup.NPC_D;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<TargetListPlanning_NPC_D> Pool = BetweenMapGenPool<TargetListPlanning_NPC_D>.Create_WillNeverBeGCed( "TargetListPlanning_NPC_D", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TargetListPlanning_NPC_D(); } );

        public static TargetListPlanning_NPC_D GetFromPoolOrCreate()
        {
            TargetListPlanning_NPC_D context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }
    #endregion

    #region TargetListPlanningRoot
    public abstract class TargetListPlanningRoot : ArcenLongTermContinuousPlanningContext
    {
        //Set immediately before workingShortList.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since this is long-term planning code.
        [ThreadStatic] private static int cb_tlpAttackerMarker;
        protected TargetListPlanningRoot()
            : base( ArcenSimContextType.LongTermContinuous )
        {
        }

        protected FactionTargetPlanningGroup TargetingFor = FactionTargetPlanningGroup.Player;

        protected float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return !this.IsRunning && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }
        
        private GameCommand cmdTargetList;
        private GameCommand cmdFRDTarget;

        public int MaxSystemsToCalculate = 0;
        public int MaxTargetsPerSystem = 0;

        private int currentPlanningCycle;
        
        // For logging you must enable tracing 'Targeting'
        // and either use the Settings menu to set Debug_TargettingDebugShipID
        // or mouseover the ship whose targeting you want to track
        // (since otherwise the trace volume is just overwhelming).
        private int tracing_entityToFollow; 
        private bool tracing;

        static float MulPerSecFarther = 1.0f;
        static float MulPerSecCloser = 1.0f;
        static bool ShowTargetInfo = false;
        
        protected override void Execute()
        {
            MulPerSecFarther = ExternalConstants.Instance.GetCustomFloat_Slow("target_priority_mul_per_sec_farther");
            MulPerSecCloser = ExternalConstants.Instance.GetCustomFloat_Slow("target_priority_mul_per_sec_closer");
            ShowTargetInfo = GameSettings.Current.GetBoolBySetting( "Debug_WeaponTargetInfo" );
            
            try
            {
                TargetPlanningStats stats = World_AIW2.Instance.Targeting_Player;
                switch ( this.TargetingFor )
                {
                    case FactionTargetPlanningGroup.NPC_A:
                        stats = World_AIW2.Instance.Targeting_NPC_A;
                        break;
                    case FactionTargetPlanningGroup.NPC_B:
                        stats = World_AIW2.Instance.Targeting_NPC_B;
                        break;
                    case FactionTargetPlanningGroup.NPC_C:
                        stats = World_AIW2.Instance.Targeting_NPC_C;
                        break;
                    case FactionTargetPlanningGroup.NPC_D:
                        stats = World_AIW2.Instance.Targeting_NPC_D;
                        break;
                }

                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    //don't let that timer get strange
                    stats.TimeOfCurrentTargetListPlanningCycleStart = 0;
                    return;
                }
                if ( World.Instance.IsPaused )
                {
                    //don't let that timer get strange
                    stats.TimeOfCurrentTargetListPlanningCycleStart = 0;
                    return;
                }
                if ( stats.TimeOfCurrentTargetListPlanningCycleStart == 0 )
                    stats.TimeOfCurrentTargetListPlanningCycleStart = ArcenTime.TimeSinceStartF;

                if ( CentralVars.DEBUG_TURN_OFF_TARGET_PLAN )
                    return;

                MaxSystemsToCalculate = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MaxShipSystemsToCalculatePerTargetingPass" );
                MaxTargetsPerSystem = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MaxTargetsPerShipSystemPerTargetingPass" );

                cmdTargetList = null;
                cmdFRDTarget = null;

                workingSystemsAdded_Targets = 0;
                workingSystemsAdded_FRD = 0;

                currentPlanningCycle = stats.CurrentTargetListPlanningCycle;
                tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Targeting );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Starting Target Planning Thread", Verbosity.DoNotShow );
                    tracing_entityToFollow = GameSettings.Current.GetIntBySetting( "Debug_TargettingDebugShipID" );
                    GameEntity_Base hoveredEntity = GameEntity_Base.CurrentlyHoveredOver;
                    if ( tracing_entityToFollow < 1 && hoveredEntity != null )
                        tracing_entityToFollow = hoveredEntity.PrimaryKeyID;
                }
                
                #region Central Loop
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        PlanetFaction faction = planet.Factions[j];

                        if ( faction.Faction.TargetingPlanningGroup != this.TargetingFor )
                            continue; //only handle our specific group

                        this.IsFactionPrecalculationNeeded = true;
                        bool breakFactionLoop = false;
                        foreach ( GameEntity_Squad AttackerEntity in faction.Entities.Squads() )
                        {
                            if ( DoEntityFramePlanningLogic_TargetPrioritizing( AttackerEntity ) == DelReturn.Break )
                            {
                                breakFactionLoop = true;
                                break;
                            }
                        }
                        if ( breakFactionLoop )
                            break;
                    }
                    if ( workingSystemsAdded_Targets >= MaxSystemsToCalculate || workingSystemsAdded_FRD >= MaxSystemsToCalculate )
                        break;
                }
                #endregion

                int systemsAdded = 0;
                if ( cmdTargetList != null )
                {
                    if ( cmdTargetList.RelatedEntityIDs.Count > 0 )
                    {
                        systemsAdded = cmdTargetList.RelatedIntegers.Count; //systemIDs
                        stats.CurrentSoFarTargetListPlanningCycleAssignments_Targets += cmdTargetList.RelatedIntegers4.Count; //targets

                        cmdTargetList.ToBeQueued = false;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdTargetList, false );
                    }
                    else
                    {
                        cmdTargetList.ReturnToPool();
                    }
                    
                    cmdTargetList = null;
                }

                if ( cmdFRDTarget != null )
                {
                    if ( cmdFRDTarget.RelatedEntityIDs.Count > 0 )
                    {
                        if ( cmdFRDTarget.RelatedEntityIDs.Count > systemsAdded )
                            systemsAdded = cmdFRDTarget.RelatedEntityIDs.Count;
                        
                        stats.CurrentSoFarTargetListPlanningCycleAssignments_Targets += cmdFRDTarget.RelatedIntegers4.Count; //targets

                        #region countByPriority export
                        //System.Text.StringBuilder builder = new System.Text.StringBuilder();
                        //List<KeyValuePair<int, int>> valueList = List<KeyValuePair<int, int>.Create_WillNeverBeGCed>();
                        //foreach ( KeyValuePair<int, int> kv in countByPriority )
                        //    valueList.Add( kv );
                        //valueList.Sort( static delegate ( KeyValuePair<int, int> left, KeyValuePair<int, int> right )
                        //{
                        //    return left.Squad.CompareTo( right.Squad );
                        //} );
                        //foreach ( KeyValuePair<int, int> kv in valueList )
                        //    builder.AppendLine( kv.Squad + "   x" + kv.Info );
                        //UnityEngine.Debug.Log( builder.ToString() );
                        #endregion
                        
                        cmdFRDTarget.ToBeQueued = false;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdFRDTarget, false );
                    }
                    else //prevents having a leak!
                    {
                        cmdFRDTarget.ReturnToPool();
                    }
                    
                    cmdFRDTarget = null;
                }

                stats.CurrentSoFarTargetListPlanningCycleAssignments_Systems += systemsAdded; //systemIDs

                //hey, we finished without getting a full load; must mean we got everything this cycle
                if ( workingSystemsAdded_Targets < MaxSystemsToCalculate && workingSystemsAdded_FRD < MaxSystemsToCalculate )
                {
                    stats.CurrentTargetListPlanningCycle++;
                    float currentTime = ArcenTime.TimeSinceStartF;
                    //only set that timer if we have real data for it
                    if ( stats.TimeOfCurrentTargetListPlanningCycleStart > 0 )
                    {
                        stats.TotalTimeOfLastTargetListPlanningCycle = currentTime - stats.TimeOfCurrentTargetListPlanningCycleStart;
                        if ( stats.TotalTimeOfLastTargetListPlanningCycle < 0.01f )
                            stats.TotalTimeOfLastTargetListPlanningCycle = 0.01f;
                    }
                    //don't directly set the timer for the next cycle, instead set it to 0 so that it will get properly set when the next timer starts if there is a gap
                    stats.TimeOfCurrentTargetListPlanningCycleStart = 0;

                    stats.LastTargetListPlanningCycleAssignments_Systems = stats.CurrentSoFarTargetListPlanningCycleAssignments_Systems;
                    stats.LastTargetListPlanningCycleAssignments_Targets = stats.CurrentSoFarTargetListPlanningCycleAssignments_Targets;
                    stats.CurrentSoFarTargetListPlanningCycleAssignments_Systems = 0;
                    stats.CurrentSoFarTargetListPlanningCycleAssignments_Targets = 0;

                    //try to run this every 5 seconds, or with 1 second gaps, whichever is slower
                    float timeToWaitBeforeNextStart = 1f - stats.TotalTimeOfLastTargetListPlanningCycle;
                    if ( timeToWaitBeforeNextStart < 1 )
                        timeToWaitBeforeNextStart = 1;
                    this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
                }
            }
            catch ( ArcenPleaseStopThisThreadException ) { }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "TargetListPlanning Exception: " + e, Verbosity.ShowAsError );
            }
        }

        private bool IsFactionPrecalculationNeeded = false;

        #region Central Add Methods
        private int workingSystemsAdded_Targets = 0;
        private int workingSystemsAdded_FRD = 0;

        private void WriteTargetableSystemListToCommand(EntitySystem System, int SystemIndex, ArcenCharacterBuffer traceBuffer)
        {
            if (System.ParentEntity == null)
                return;
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t" ).Add( "Actual result! WriteTargetableSystemListToCommand called with " ).Add( finalListToSend_Targetable.Count ).Add( " targets:" );
                for ( int i = 0; i < finalListToSend_Targetable.Count; i++ )
                {
                    var kvp = finalListToSend_Targetable[i];
                    var e = kvp.Squad;
                    var v = kvp.Info.Importance;
                    
                    traceBuffer.Add( "\n\t\t\t" ).Add("[").Add(v).Add("]").Add("\t").Add(e.OrNull());
                }
            }
            #endregion

            //create it late!
            if ( cmdTargetList == null )
                cmdTargetList= GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetTargetList], GameCommandSource.AnythingElse );

            cmdTargetList.RelatedEntityIDs.Add(System.ParentEntity.PrimaryKeyID);
            cmdTargetList.RelatedIntegers.Add(SystemIndex);
            
            //count of target entities to send
            cmdTargetList.RelatedIntegers2.Add(finalListToSend_Targetable.Count);
            
            //then actually add the targetentities
            for (int i = 0; i < finalListToSend_Targetable.Count; i++)
            {
                var cand = finalListToSend_Targetable[i];
                cmdTargetList.RelatedIntegers4.Add(cand.Squad?.PrimaryKeyID ?? -1);
            }
            
            finalListToSend_Targetable.Clear();
            
            workingSystemsAdded_Targets++;
        }

        private void WriteFRDItemToCommand(EntitySystem System, int SystemIndex, Candidate Target, ArcenCharacterBuffer traceBuffer )
        {
            if ( System.ParentEntity == null )
                return;
            
            #region Tracing
            if ( traceBuffer != null )
            {
                if ( Target.Squad != null )
                    traceBuffer.Add( "\n\t\t" ).Add( "entity has FRD target " ).Add( Target.Squad.ToString() ).Add( " with importance " ).Add( Target.Info.Importance );
                else
                    traceBuffer.Add( "\n\t\t" ).Add( "entity has NULL FRD target " );
            }
            #endregion

            //create it late!
            if ( cmdFRDTarget == null )
                cmdFRDTarget= GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetFRDTarget], GameCommandSource.AnythingElse );

            cmdFRDTarget.RelatedEntityIDs.Add(System.ParentEntity.PrimaryKeyID);
            cmdFRDTarget.RelatedIntegers.Add(SystemIndex);
            cmdFRDTarget.RelatedIntegers4.Add(Target.Squad == null ? -1 : Target.Squad.PrimaryKeyID);
            cmdFRDTarget.RelatedIntegers2.Add(Target.Squad == null ? 0 :  Target.Info.Importance);
            finalChoicesForFRD.Clear();
            workingSystemsAdded_FRD++;
        }
        #endregion
        
        #region GetTargetingInfo
        private NonSimTargetPlanningInfo GetTargetingInfo( GameEntity_Squad squad )
        {
            switch ( this.TargetingFor )
            {
                case FactionTargetPlanningGroup.Player:
                    return squad.TargetingInfo_Player;
                case FactionTargetPlanningGroup.NPC_A:
                    return squad.TargetingInfo_NPC_A;
                case FactionTargetPlanningGroup.NPC_B:
                    return squad.TargetingInfo_NPC_B;
                case FactionTargetPlanningGroup.NPC_C:
                    return squad.TargetingInfo_NPC_C;
                case FactionTargetPlanningGroup.NPC_D:
                    return squad.TargetingInfo_NPC_D;
            }
            throw new Exception( "no entry found for " + this.TargetingFor );
        }
        #endregion

        private DelReturn DoEntityFramePlanningLogic_TargetPrioritizing(GameEntity_Squad AttackerEntity)
        {
            #region Tracing
            ArcenCharacterBuffer traceBuffer = null;
            if ( tracing && 
                 AttackerEntity != null && 
                 AttackerEntity.PrimaryKeyID == tracing_entityToFollow )
            {
                traceBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "TargetListPlanning-DoEntityFramePlanningLogic_TargetPrioritizing-trace", 10f );
                traceBuffer.Add( "\n" ).Add( "Targeting for " ).Add( AttackerEntity.TypeData.InternalName ).Add( " " ).Add( AttackerEntity.PrimaryKeyID );
            }
            #endregion
            
            int debugLine = 1;
            try
            {
                if ( AttackerEntity.PrimaryKeyID == -17 )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Oi!  Tried to run DoEntityFramePlanningLogic_TargetPrioritizing on the FakeEntity!  How did this even get into the game?  Faction: " +
                        AttackerEntity.GetFactionDisplayNameSafe() + " typeData: " + AttackerEntity.GetTypeDisplayNameSafe() + " planet: " + AttackerEntity.GetPlanetNameSafe(), Verbosity.ShowAsError );
                    return DelReturn.Continue;
                }

                debugLine = 100;
                if ( workingSystemsAdded_Targets >= MaxSystemsToCalculate || workingSystemsAdded_FRD >= MaxSystemsToCalculate )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        if ( workingSystemsAdded_Targets >= MaxSystemsToCalculate )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because workingSystemsAdded_Targets >= MaxSystemsToCalculate" );
                        else if ( workingSystemsAdded_FRD >= MaxSystemsToCalculate )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because workingSystemsAdded_FRD >= MaxSystemsToCalculate" );
                        ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                        traceBuffer.ReturnToPool();
                        traceBuffer = null;
                    }
                    #endregion
                    return DelReturn.Break;
                }
                
                debugLine = 200;
                if ( AttackerEntity == null || 
                     AttackerEntity.GetHasBeenDestroyed() || 
                     AttackerEntity.HasBeenRemovedFromSim || 
                     AttackerEntity.ToBeRemovedAtEndOfThisFrame || 
                     AttackerEntity.SecondsSpentAsRemains > 0 )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        if ( AttackerEntity == null )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because entity is null" ); // should never be true, just being paranoid
                        else if ( AttackerEntity.GetHasBeenDestroyed() )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because entity.GetHasBeenDestroyed()" );
                        else if ( AttackerEntity.HasBeenRemovedFromSim )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because entity.HasBeenRemovedFromSim" );
                        else if ( AttackerEntity.ToBeRemovedAtEndOfThisFrame )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because entity.ToBeRemovedAtEndOfThisFrame" );
                        else if ( AttackerEntity.SecondsSpentAsRemains > 0 )
                            traceBuffer.Add( "\n\t" ).Add( "skipping because entity.SecondsSpentAsRemains > 0" );
                        ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                        traceBuffer.ReturnToPool();
                        traceBuffer = null;
                    }
                    #endregion
                    return DelReturn.Continue;
                }

                EntitySystem system;
                debugLine = 300;
                for ( int i = 0; i < AttackerEntity.Systems.Count; i++ )
                {
                    debugLine = 400;
                    system = AttackerEntity.Systems[i];
                    if ( system == null )
                        continue;
                    
                    debugLine = 420;
                    EntitySystemTypeData systemData = system.TypeData;
                    if ( systemData == null )
                        continue;
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t" ).Add( "*System-specific targeting for " ).Add( system.TypeData.InternalName_Longer );
                    }
                    #endregion
                    
                    debugLine = 500;
                    if ( systemData.Category != EntitySystemCategory.Weapon )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( "skipping because Category != EntitySystemCategory.Weapon" );
                        }
                        #endregion
                        continue;
                    }
                    
                    debugLine = 600;
                    if ( systemData.OnlyFiresOnDeath )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( "skipping because OnlyFiresOnDeath" );
                        }
                        #endregion
                        continue;
                    }
                    debugLine = 610;
                    try
                    {
                        if ( system.DataForMark.ShotsPerSalvo <= 0 )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t" ).Add( "skipping because ForMark[system.DataForMark.MarkLevel.Ordinal].ShotsPerSalvo" );
                            }
                            #endregion
                            continue;
                        }
                    }
                    catch { } //this sometimes has errors, let's ignore them

                    //if ( !systemData.SkipChecksInTargetingCodeForIfDisabledByMark )
                    {
                        try
                        {
                            if ( !system.DataForMark.IsFunctionalAtThisMarkLevel )
                            {
                                #region Tracing
                                if ( traceBuffer != null )
                                {
                                    traceBuffer.Add( "\n\t\t" ).Add( "skipping because !system.DataForMark.IsFunctionalAtThisMarkLevel" );
                                }
                                #endregion
                                continue;
                            }
                        }
                        catch { } //this sometimes has errors, let's ignore them
                    }

                    debugLine = 640;
                    //if ( systemData.SkipAllTargetingCodeIfDisabled )
                    {
                        try
                        {
                            var disabledReason = system.ComputeDisabledReason(CheckEntityStatus:true,CheckReload:false);
                            if ( disabledReason != ArcenRejectionReason.Unknown )
                                continue;
                        }
                        catch { } // just copying everyone else, weird though
                    }

                    if ( systemData.FiringTiming == FiringTiming.Never )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( "skipping because FiringTiming == FiringTiming.Never" );
                        }
                        #endregion
                        continue;
                    }
                    if ( system.LastTargetListPlanningCycle >= currentPlanningCycle )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( "skipping because LastTargetListPlanningCycle >= currentPlanningCycle" );
                        }
                        #endregion
                        continue;
                    }
                    
                    debugLine = 700;
                    system.LastTargetListPlanningCycle = currentPlanningCycle;
                    
                    debugLine = 800;
                    this.DoEntityFramePlanningLogic_TargetPrioritizing_SpecificSystem( AttackerEntity, system, i, traceBuffer );
                    
                    debugLine = 900;
                    if ( workingSystemsAdded_Targets >= MaxSystemsToCalculate || 
                         workingSystemsAdded_FRD >= MaxSystemsToCalculate )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            if ( workingSystemsAdded_Targets >= MaxSystemsToCalculate )
                                traceBuffer.Add( "\n\t" ).Add( "stopping because workingSystemsAdded_Targets >= MaxSystemsToCalculate" );
                            else if ( workingSystemsAdded_FRD >= MaxSystemsToCalculate )
                                traceBuffer.Add( "\n\t" ).Add( "stopping because workingSystemsAdded_FRD >= MaxSystemsToCalculate" );
                            ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                            traceBuffer.ReturnToPool();
                            traceBuffer = null;
                        }
                        #endregion
                        
                        return DelReturn.Break;
                    }
                }
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t" ).Add( "skipping because of exception" );
                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                    traceBuffer.ReturnToPool();
                    traceBuffer = null;
                }
                #endregion
                
                LOG.Err("exception in target planning; debugLine={0}\n{1}", debugLine, e);

                return DelReturn.Continue;
            }
            finally
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                    traceBuffer.ReturnToPool();
                    traceBuffer = null;
                }
                #endregion
            }
            
            return DelReturn.Continue;
        }

        private void DoEntityFramePlanningLogic_TargetPrioritizing_SpecificSystem(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, int SystemIndex, ArcenCharacterBuffer traceBuffer )
        {
            try
            {
                var attackerSystemTypeData = AttackerSystem.TypeData;
                if ( attackerSystemTypeData != null )
                    this.FindTopEntriesForSystem(AttackerEntity, AttackerSystem, attackerSystemTypeData, SystemIndex, traceBuffer);

                //we have nothing to add to the targeting list, that's fine
                if ( finalListToSend_Targetable.Count == 0 )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "No entries found for targeting list" );
                    }
                    #endregion
                    
                    //we have nothing to add, but add the system as empty so that it will clear what was there
                    if ( AttackerSystem.CountOfPotentialTargetsByPriorityForSystem() > 0 )
                    {
                        this.WriteTargetableSystemListToCommand( AttackerSystem, SystemIndex, traceBuffer );
                    }
                }
                //we DO have something to add, just do it and don't worry about what used to be there
                else 
                {
                    this.WriteTargetableSystemListToCommand( AttackerSystem, SystemIndex, traceBuffer );
                }

                // for debugging
                /*
                GameEntity_Squad cur_frd_target = null;
                bool cur_frd_listed = false;
                bool cur_frd_valid = false;

                cur_frd_target = AttackerSystem.CurrentFRDTarget.GetSquad();
                if (cur_frd_target != null)
                {
                    cur_frd_listed = finalChoicesForFRD.Exists((i)=> i.Squad == cur_frd_target );
                    cur_frd_valid = AttackerSystem.GetIsTargetValid(cur_frd_target);
                }
                */

                if (finalChoicesForFRD.Count > 0)
                {
                    //if (!cur_frd_valid)
                    {
                        //at one time we picked at random from the finalChoices list. However,
                        //this list was helpfully sorted for us in ProcessShortlistIntoTargetRanking().
                        //Taking distance into account is useful
                        this.WriteFRDItemToCommand( AttackerSystem, SystemIndex, finalChoicesForFRD[0], traceBuffer );
                    }
                }
                else
                {
                    this.WriteFRDItemToCommand( AttackerSystem, SystemIndex, new Candidate(), traceBuffer );
                }

                //if (cur_frd_listed != cur_frd_valid)
                //{
                //    ArcenDebugging.ArcenDebugLogNoDateOrAnything(
                //                string.Format("warning: entity {0} system {1} had frdtarget of {2} which is still valid but wasn't in the new list ({3})",
                //                    AttackerEntity.ToString(), AttackerSystem.ToString(), cur_frd_target.ToString(), finalChoicesForFRD.Count),
                //                DebugLogDestination.ArcenDebugLog, Verbosity.ShowAsError);
                //}

                //if (cur_frd_valid)
                //{
                //    if (!cur_frd_listed)
                //    {
                //        this.WriteFRDItemToCommand( AttackerSystem, SystemIndex, new KeyValuePair<GameEntity_Squad,NonSimTargetPlanningInfo>( null, null ), traceBuffer );
                //    }
                //}

                //else
                //{
                //    //we have no FRD choices, that's fine
                //    if ( finalChoicesForFRD.Count == 0 )
                //    {
                //        #region Tracing
                //        if ( traceBuffer != null )
                //        {
                //            traceBuffer.Add( "\n\t\t" ).Add( "No entries found for FRD" );
                //        }
                //        #endregion
                    
                //        this.WriteFRDItemToCommand( AttackerSystem, SystemIndex, new KeyValuePair<GameEntity_Squad,NonSimTargetPlanningInfo>( null, null ), traceBuffer );
                //    }
                //    else
                //    {
                        
                //}
                //}

                //how long do we keep the chosen FRD unit?  etc
                //how many possible targets do we really send to the individual ships?  Only a max of 8x their salvo size, maybe?  Minimum of 2x their salvo size of already-in-range units?
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLog("Error in targeting logic " + e.ToString(), Verbosity.ShowAsError);
            }
        }

        private readonly List<SafeSquadWrapper> fullPossibilitiesListForFaction_Positive = List<SafeSquadWrapper>.Create_WillNeverBeGCed(90000, "TargetListPlanning-fullPossibilitiesListForFaction_Positive" );
        private readonly List<SafeSquadWrapper> fullPossibilitiesListForFaction_Zero = List<SafeSquadWrapper>.Create_WillNeverBeGCed(90000, "TargetListPlanning-fullPossibilitiesListForFaction_Zero" );
        private Int16 factionTargetCountForFRDChoices = 1;
        private bool factionIsPlayerFaction;
        private bool factionPriorityAIBased;

        #region PreFillFactionBasedData
        /// <summary>
        /// The idea here is to calculate the stuff that is faction-specific as few times as possible.
        /// </summary>
        public void PreFillFactionBasedData(PlanetFaction PFac, ArcenCharacterBuffer traceBuffer )
        {
            if ( PFac == null )
                return;
            
            Faction faction = PFac.Faction;
            if ( faction == null )
                return;
            
            Planet planet = PFac.Planet;
            if ( planet == null )
                return;

            int debugCode = 0;
            try
            {
                fullPossibilitiesListForFaction_Positive.Clear();
                fullPossibilitiesListForFaction_Zero.Clear();
                
                factionIsPlayerFaction = faction.Type == FactionType.Player;
                factionPriorityAIBased = false;
                
                debugCode = 1000;
                
                #region factionTargetCountForFRDChoices

                this.factionTargetCountForFRDChoices = 1;
                if ( factionIsPlayerFaction )
                {
                    factionTargetCountForFRDChoices = 1;
                }
                else 
                if ( faction.Type == FactionType.AI )
                {
                    debugCode = 1100;
                    AISentinelsCoreData sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( sentinelsExternal != null )
                    {
                        debugCode = 1110;
                        //for AIs > 4 difficulty
                        if ( sentinelsExternal.AIDifficulty.Difficulty > 4 )
                        {
                            debugCode = 1120;
                            factionPriorityAIBased = true;
                            factionTargetCountForFRDChoices = 10;
                            if ( sentinelsExternal.AIDifficulty.Difficulty > 6 )
                                factionTargetCountForFRDChoices = 5;
                            if ( sentinelsExternal.AIDifficulty.Difficulty > 8 )
                                factionTargetCountForFRDChoices = 3;
                        }
                        else //less than 4 difficulty
                            factionTargetCountForFRDChoices = 30;
                    }
                }
                else 
                if ( faction.SpecialFactionData.InternalName == "HunterFleet" )
                {
                    debugCode = 1200;
                    //hunter fleet also makes bad targeting choices at lower difficulties
                    AIHunterCoreData factionExternal = faction.GetAISentinelsCoreData().HunterInfo;
                    if ( factionExternal.AIDifficulty.Difficulty > 4 )
                    {
                        debugCode = 1210;
                        factionPriorityAIBased = true;
                        factionTargetCountForFRDChoices = 10;
                        if ( factionExternal.AIDifficulty.Difficulty > 6 )
                            factionTargetCountForFRDChoices = 5;
                        if ( factionExternal.AIDifficulty.Difficulty > 8 )
                            factionTargetCountForFRDChoices = 3;
                    }
                    else //less than 4 difficulty
                        factionTargetCountForFRDChoices = 30;
                }
                else 
                if ( faction.SpecialFactionData.InternalName == "AIWarden" )
                {
                    debugCode = 1300;
                    //warden fleet also makes bad targeting choices at lower difficulties
                    AIWardenCoreData factionExternal = faction.GetAISentinelsCoreData().WardenInfo;
                    if ( factionExternal.AIDifficulty.Difficulty > 4 )
                    {
                        debugCode = 1310;
                        factionPriorityAIBased = true;
                        factionTargetCountForFRDChoices = 10;
                        if ( factionExternal.AIDifficulty.Difficulty > 6 )
                            factionTargetCountForFRDChoices = 5;
                        if ( factionExternal.AIDifficulty.Difficulty > 8 )
                            factionTargetCountForFRDChoices = 3;
                    }
                    else //less than 4 difficulty
                        factionTargetCountForFRDChoices = 30;
                }
                else
                {
                    factionPriorityAIBased = true;
                    factionTargetCountForFRDChoices = 5;
                }
                
                #endregion
                
                debugCode = 2000;

                if ( traceBuffer != null )
                    traceBuffer.Add( "PreFillFactionBasedData: faction: " + faction.GetDisplayName() + " FactionIndicesIAmHostileTo: " +
                        faction.FactionIndicesIAmHostileTo.Count + " planet: " + planet.Name +"\n" );

                int innerDebugStage = 0;
                for ( int i = 0; i < faction.FactionIndicesIAmHostileTo.Count; i++ )
                {
                    debugCode = 2100;
                    PlanetFaction pFacOther = planet.Factions[faction.FactionIndicesIAmHostileTo[i]];
                    if ( pFacOther.Faction.Type == FactionType.NaturalObject )
                        continue;
                    
                    if ( traceBuffer != null )
                        traceBuffer.Add( "Against: faction: " ).Add(pFacOther.Faction.GetDisplayName()).Add(" Faction entity count: ").Add(pFacOther.Entities.SquadCount).NewLine();

                    foreach ( GameEntity_Squad defenderEntity in pFacOther.Entities.Squads() )
                    {
                        debugCode = 2200;
                        try
                        {
                            innerDebugStage = 0;
                            if ( defenderEntity == null ||
                                 defenderEntity.GetHasBeenDestroyed() ||
                                 defenderEntity.HasBeenRemovedFromSim ||
                                 defenderEntity.ToBeRemovedAtEndOfThisFrame ||
                                 defenderEntity.SecondsSpentAsRemains > 0 )
                            {
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "Dead in some way: " + defenderEntity.TypeData.DisplayName + "\n" );
                                continue;
                            }
                            //pull this into a local variable so that the reference can't go away later
                            GameEntityTypeData.MarkLevelStats markData = defenderEntity.DataForMark;
                            if ( markData == null )
                            {
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "null markData: " + defenderEntity.TypeData.DisplayName + "\n" );
                                continue;
                            }

                            innerDebugStage = 100;
                            if ( !PFac.GetDoesTargetPassBasicEligibilityTests( defenderEntity, false ) )
                            {
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "faction does not PassBasicEligibilityTests: " + defenderEntity.TypeData.DisplayName + "\n" );
                                continue;
                            }

                            innerDebugStage = 300;
                            var istargetable = defenderEntity.GetDoesThisTargetPassBasicEligibilityTests( PFac, null, null, false, RangeCheckType.DoNotCheckRange );
                            if ( istargetable != GameEntity_Squad.TargetEligibilityResult.Valid )
                            {
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "entity does not PassBasicEligibilityTests: " + defenderEntity.TypeData.DisplayName + "for reason "+istargetable+"\n");

                                continue;
                            }

                            innerDebugStage = 400;
                            int importanceFromXMLInt = factionPriorityAIBased ?
                                                defenderEntity.TypeData.PriorityAsAITarget.ImportanceMetric :
                                                defenderEntity.TypeData.PriorityAsFRDTarget.ImportanceMetric;

                            innerDebugStage = 500;
                            if ( importanceFromXMLInt < 0 )
                                continue; //these are explicit ignores for autotargeting

                            innerDebugStage = 600;
                            NonSimTargetPlanningInfo defenderTargetingInfo = GetTargetingInfo( defenderEntity );
                            defenderTargetingInfo.DistanceSqr = -1;
                            defenderTargetingInfo.Distance = -1;
                            defenderTargetingInfo.IsInRange = false;
                            defenderTargetingInfo.BeingOverkilled = false;
                            defenderTargetingInfo.FactionPriorityDivisor = 1;

                            innerDebugStage = 610;
                            if (AIWar2GalaxySettingQuickAccess.FRD_Considers_Overkill &&
                                defenderEntity.GetExpectsToBeOverkilled( 0 ) )
                            {
                                #region Trace
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "GetExpectsToBeOverkilled: " + defenderEntity.TypeData.DisplayName + "\n" );
                                if ( tracing && defenderEntity.PrimaryKeyID == tracing_entityToFollow )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "GetExpectsToBeOverkilled: " + defenderEntity.CalculatedAttackerDamageTotal_LastFrame, Verbosity.DoNotShow );
                                #endregion

                                defenderTargetingInfo.BeingOverkilled = true;
                            }

                            innerDebugStage = 700;
                            if ( importanceFromXMLInt == 0 )
                            {
                                fullPossibilitiesListForFaction_Zero.Add( defenderEntity );
                                continue; //meant to be the "very last thing"
                            }

                            innerDebugStage = 800;
                            float importanceFromXML = (float)importanceFromXMLInt;
                            float importanceWithAdjustment = 0;

                            #region Normalize From The XML to importanceWithAdjustment
                            innerDebugStage = 900;
                            //If things are in the regular range
                            if ( importanceFromXML < BASE_NORMALIZATION_FOR_XML_IMPORTANCE )
                                importanceWithAdjustment = (importanceFromXML / BASE_NORMALIZATION_FOR_XML_IMPORTANCE) * FACTOR_IMPORTANCE_FOR_XML;
                            else //if things go beyond the normal range
                            {
                                innerDebugStage = 1000;
                                //to start with, with just have the base
                                importanceWithAdjustment = FACTOR_IMPORTANCE_FOR_XML;
                                //now remove the normal range
                                importanceFromXML -= BASE_NORMALIZATION_FOR_XML_IMPORTANCE;
                                //now add on up to the extra range for things that are really important
                                importanceWithAdjustment += ((importanceFromXML / EXTENDED_NORMALIZATION_FOR_XML_IMPORTANCE_MINUS_STANDARD) * FACTOR_IMPORTANCE_FOR_EXTRA_HIGH_PRIORITY_XML);
                            }
                            #endregion

                            innerDebugStage = 1100;
                            defenderTargetingInfo.FactionPriorityPart = importanceWithAdjustment;

                            innerDebugStage = 1500;
                            fullPossibilitiesListForFaction_Positive.Add( defenderEntity );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLog( "Error in PreFillFactionBasedData inner entity loop innerDebugStage: " + innerDebugStage + " \n" + e.ToString(), Verbosity.ShowAsError );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit an Exception in PreFillFactionBasedData. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        private struct Candidate
        {
            public GameEntity_Squad Squad;
            public NonSimTargetPlanningInfo Info;

            public override string ToString()
            {
                return string.Format("[{0}]{1}", Info?.Importance.ToString()??"?", Squad.OrNull());
            }
        }
        
        //these two are not readonly because they get swapped in position
        private List<Candidate> workingFullList = List<Candidate>.Create_WillNeverBeGCed(90000, "TargetListPlanning-workingFullList" );
        private List<Candidate> workingFullListAlt = List<Candidate>.Create_WillNeverBeGCed(90000, "TargetListPlanning-workingFullListAlt" );
        
        private readonly List<Candidate> workingShortList = List<Candidate>.Create_WillNeverBeGCed(90000, "TargetListPlanning-workingShortList" );
        private readonly List<Candidate> finalListToSend_Targetable = List<Candidate>.Create_WillNeverBeGCed(60, "TargetListPlanning-finalListToSend_Targetable" );
        private readonly List<Candidate> finalChoicesForFRD = List<Candidate>.Create_WillNeverBeGCed( 60, "TargetListPlanning-finalChoicesForFRD" );
        
        private int targetCountForFRDChoices = 1;
        private float highestPriorityFound;
        private bool foundAnyCombatants = false;
        private int maxToSendForThisSystem;
        private int remainingRequiredToBeInRange;

        private void FindTopEntriesForSystem( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, EntitySystemTypeData attackerSystemTypeData, int SystemIndex, ArcenCharacterBuffer traceBuffer )
        {
            foundAnyCombatants = false;
            if ( this.IsFactionPrecalculationNeeded )
            {
                this.PreFillFactionBasedData( AttackerEntity.PlanetFaction, traceBuffer /*AttackerEntity.TypeData.InternalName.Contains( "Chrysalis" )*/ );
                this.IsFactionPrecalculationNeeded = false;
            }
            
            //if ( AttackerEntity.TypeData.InternalName.Contains( "Chrysalis" ) )
            //    ArcenDebugging.SingleLineQuickDebug( "TLP for " + AttackerEntity.TypeData.InternalName +
            //        " fullPossibilitiesListForFaction_Positive: " + fullPossibilitiesListForFaction_Positive.Count +
            //        " fullPossibilitiesListForFaction_Zero: " + fullPossibilitiesListForFaction_Zero.Count +
            //        " IsFactionPrecalculationNeeded: " + wasIsFactionPrecalculationNeeded );
            if ( fullPossibilitiesListForFaction_Positive.Count == 0 && 
                 fullPossibilitiesListForFaction_Zero.Count == 0 )
            {
                #region Tracing
                if ( traceBuffer != null )
                    traceBuffer.Add( "\n\t\t" ).Add( "skip because fullPossibilitiesListForFaction_Positive.Count == 0 && fullPossibilitiesListForFaction_Zero.Count == 0" );
                #endregion
                return;
            }

            workingShortList.Clear();
            workingFullList.Clear();
            finalListToSend_Targetable.Clear();
            finalChoicesForFRD.Clear();

            if (ShowTargetInfo)
                AttackerSystem.PotentialTargetToPriorityLookup.ClearConstructionDictForStartingConstruction();
            else
                AttackerSystem.PotentialTargetToPriorityLookup.ClearAndWipeInnerReferences();
            try
            {
                bool can_move = AttackerEntity.TypeData.IsMobile && 
                                AttackerEntity.CalculatedSpeed > 0;
                
                int seek_range = 0;
                if (can_move)
                {
                    var temp = AttackerSystem.DataForMark.CalculateActualSeekRange(AttackerEntity);
                    if (temp > 0)
                        seek_range = temp;
                        
                    if (AttackerEntity.Orders != null &&
                        AttackerEntity.Orders.Behavior == EntityBehaviorType.Attacker_Full)
                    {
                        seek_range = int.MaxValue;
                    }
                    
                    // the actual seek_range value can't be used here
                    // since distance isn't going to be measured from the parent
                    // .. but the important thing is to let it pick targets at all
                    // so basically consider seek_range to be infinite like attacker-full 
                    if ( !string.IsNullOrEmpty(AttackerEntity.TypeData.OrbitsParentUnlessParentSystemHasTarget) &&
                         AttackerEntity.ParentGameEntity.GetSquad() != null)
                    {
                        seek_range = int.MaxValue;
                    }
                }
                
                bool can_seek = seek_range > 0;
                
                #region targetCountForFRDChoices
                targetCountForFRDChoices = this.factionTargetCountForFRDChoices;
                //if not mobile, or we're not in attacker mode, then don't calculate FRD stuff since it won't be used!
                if ( can_seek == false )
                {
                    //...that said, to keep things responsive for player ships, go ahead and make it so that they always keep something on tap if mobile
                    //good note by Badger that otherwise FRD felt sluggish
                    if ( factionIsPlayerFaction == false )
                    {
                        targetCountForFRDChoices = 0;

                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( "targetCountForFRDChoices = 0 because non-player and either non-mobile or not in FRD mode" );
                        }
                        #endregion
                    }
                }
                #endregion

                highestPriorityFound = 0;
                
                //fill workingFullList, lowestDamageAmountFound, and highestPriorityFound
                GameEntity_Squad defenderEntity;
                List<SafeSquadWrapper> workingListFromFaction;
                for ( int outerLoop = 0; outerLoop < 2; outerLoop++ )
                {
                    if ( outerLoop == 0 )
                    {
                        workingListFromFaction = fullPossibilitiesListForFaction_Positive;
                        #region Tracing
                        if ( traceBuffer != null )
                            traceBuffer.Add( "\n\t\t" ).Add( "checking entities in fullPossibilitiesListForFaction_Positive (count:" ).Add( workingListFromFaction.Count ).Add( ")" );
                        #endregion
                    }
                    else
                    {
                        workingListFromFaction = fullPossibilitiesListForFaction_Zero;
                        #region Tracing
                        if ( traceBuffer != null )
                            traceBuffer.Add( "\n\t\t" ).Add( "checking entities in fullPossibilitiesListForFaction_Zero (count:" ).Add( workingListFromFaction.Count ).Add( ")" );
                        #endregion
                    }

                    for ( int i = 0; i < workingListFromFaction.Count; i++ )
                    {
                        defenderEntity = workingListFromFaction[i].GetSquad();
                        if ( defenderEntity == null )
                            continue; //it died or changed type

                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t" ).Add( ">Evaluating defenderEntity: " + defenderEntity );
                        }
                        #endregion
                        
                        if ( defenderEntity == AttackerEntity )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity == AttackerEntity" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.GetHasBeenDestroyed() )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.GetHasBeenDestroyed()" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.HasBeenRemovedFromSim )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.HasBeenRemovedFromSim" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.ToBeRemovedAtEndOfThisFrame )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.ToBeRemovedAtEndOfThisFrame" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.SecondsSpentAsRemains > 0 ) //we duplicate this check because it may have changed
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.SecondsSpentAsRemains > 0" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.PlanetFaction == null ) //we duplicate this check because it may have changed
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.PlanetFaction is null" );
                            }
                            #endregion
                            continue;
                        }
                        if ( defenderEntity.GetFactionTypeSafe() == FactionType.NaturalObject ) //we duplicate this check because it may have changed
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity faction is NaturalObject" );
                            }
                            #endregion
                            continue;
                        }

                        NonSimTargetPlanningInfo defenderTargetingInfo = GetTargetingInfo( defenderEntity );
                        
                        if (AIWar2GalaxySettingQuickAccess.FRD_Considers_Overkill)
                        {
                            if (defenderTargetingInfo.BeingOverkilled == false)
                                defenderTargetingInfo.BeingOverkilled = defenderEntity.GetExpectsToBeOverkilled( 0 );

                            if (defenderTargetingInfo.BeingOverkilled)
                            {
                                #region Tracing
                                if ( traceBuffer != null )
                                    traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderEntity.GetExpectsToBeOverkilled(0)" );
                                #endregion
                                continue;
                            }
                        }
                        
                        if ( !AttackerSystem.GetIsTargetValid( defenderEntity ) )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because !AttackerSystem.GetIsTargetValid(defenderEntity)" );
                            #endregion
                            continue;
                        }

                        defenderTargetingInfo.Distance = ArcenPoint.Distance( AttackerEntity.WorldLocation, defenderEntity.WorldLocation );
                        //defenderTargetingInfo.DistanceSqr = ArcenPoint.DistanceSqr( AttackerEntity.WorldLocation, defenderEntity.WorldLocation );
                        
                        int range = AttackerSystem.GetRangeToShoot(defenderEntity);
                        if (range < 0)
                        {
                            #region Tracing
                            if ( traceBuffer != null ) 
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because AttackerSystem.GetRangeToShoot(defenderEntity) returned " ).Add(range);
                            #endregion
                            continue;
                        }
                        
                        defenderTargetingInfo.IsInRange = (range > defenderTargetingInfo.Distance);

                        /*
                        if (AttackerSystem.ShouldShowTargetDebug())
                        {
                            defenderEntity.TargetingInfo_Debug.Distance = defenderTargetingInfo.Distance;
                            defenderEntity.TargetingInfo_Debug.IsInRange = defenderTargetingInfo.IsInRange;
                        }
                        */
                        
                        #region Tracing
                        if ( traceBuffer != null ) traceBuffer.Add( "\n\t\t\t" ).Add( " range=" + range + " seek_range=" + seek_range + " distance=" + defenderTargetingInfo.Distance + " isinrange=" + defenderTargetingInfo.IsInRange );
                        #endregion

                        //skip stuff that is out of range if we can't move
                        if ( defenderTargetingInfo.IsInRange == false )
                        {
                            if ( can_seek == false || seek_range < defenderTargetingInfo.Distance )
                            {
                                #region Tracing
                                if ( traceBuffer != null )
                                {
                                    traceBuffer.Add( "\n\t\t\t" ).Add( "skip because !canIMove && !IsInRange" );
                                }
                                #endregion
                                continue;
                            }
                        }

                        //or if limited to range from our parent, skip stuff outside that
                        if (!string.IsNullOrEmpty(AttackerEntity.TypeData.OrbitsParentUnlessParentSystemHasTarget))
                        {
                            var parentSquad = AttackerEntity.ParentGameEntity.GetSquad();
                            var parentSquadsParentEntity = parentSquad?.ParentGameEntity.GetSquad();
                            var parentParentSystem = parentSquadsParentEntity?.GetSystemById( AttackerEntity.TypeData.OrbitsParentUnlessParentSystemHasTarget );
                            
                            int distance;
                            if ( (parentParentSystem?.GetIsTargetInRange( defenderEntity, RangeCheckType.ForFiringOrSeeking, out distance )).NullOrFalse() )
                            {
                                #region Tracing
                                if ( traceBuffer != null )
                                {
                                    traceBuffer.Add( "\n\t\t\t" ).Add( "skip because OrbitsParentUnlessHasTargetInRange and target was not" );
                                }
                                #endregion
                                continue;
                            }
                        }

                        try
                        {
                            defenderTargetingInfo.Importance = this.CalculateCoreImportance( AttackerEntity, AttackerSystem, attackerSystemTypeData, 
                                defenderEntity, defenderTargetingInfo, traceBuffer );

                            if (ShowTargetInfo)
                                AttackerSystem.PotentialTargetToPriorityLookup.SetToConstructionDict(defenderEntity.PrimaryKeyID, defenderTargetingInfo.Importance);
                        }
                        catch ( ArcenPleaseStopThisThreadException )
                        {
                        }
                        catch ( Exception e )
                        {
                            defenderTargetingInfo.Importance = 1;
                            ArcenDebugging.ArcenDebugLog( "Error in targeting core calculation of importance " + e.ToString(), Verbosity.ShowAsError );
                        }
                        
                        if ( defenderTargetingInfo.Importance < 0 )
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t" ).Add( "skip because defenderTargetingInfo.Importance < 0" );
                            }
                            #endregion
                            continue;
                        }

                        //if ( !countByPriority.ContainsKey( defenderTargetingInfo.Importance ) )
                        //    countByPriority[defenderTargetingInfo.Importance] = 1;
                        //else
                        //    countByPriority[defenderTargetingInfo.Importance]++;

                        if ( defenderTargetingInfo.Importance > highestPriorityFound )
                            highestPriorityFound = defenderTargetingInfo.Importance;

                        //this one is at least in the running, so let's see if it's worth pursuing for real.
                        defenderTargetingInfo.ToBeRemoved = false;
                        if ( defenderEntity.TypeData.IsCombatant )
                            foundAnyCombatants = true;

                        workingFullList.Add( new Candidate(){Squad=defenderEntity, Info=defenderTargetingInfo} );

                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t" ).Add( "Congratulations, you actually got added to workingFullList! Importance: " + defenderTargetingInfo.Importance );
                        }
                        #endregion
                    }
                    
                    if ( workingFullList.Count <= 0 )
                    {
                        #region Tracing
                        if ( traceBuffer != null ) traceBuffer.Add( "\n\t\t" ).Add( "skip because workingFullList.Count <= 0" );
                        #endregion
                        return;
                    }

                    #region Tracing
                    if ( traceBuffer != null ) traceBuffer.Add( "\n\t\t" ).Add( "workingFullList has " + workingFullList.Count + " entries" );
                    #endregion

                    //sending more than 8x the number of targets that this has salvos is a waste
                    maxToSendForThisSystem = AttackerSystem.DataForMark.ShotsPerSalvo * 8;
                    //but for high-salvo ships we still want that limited to MaxTargetsPerSystem
                    maxToSendForThisSystem = Math.Min( maxToSendForThisSystem, MaxTargetsPerSystem );
                    //UNLESS MaxTargetsPerSystem is actually smaller than the salvo size of the ship!
                    maxToSendForThisSystem = Math.Max( AttackerSystem.DataForMark.ShotsPerSalvo, maxToSendForThisSystem );
                    //but we want to be sure that a certain number are literally within range if we're sending a larger list
                    remainingRequiredToBeInRange = AttackerSystem.DataForMark.ShotsPerSalvo + AttackerSystem.DataForMark.ShotsPerSalvo;
                    remainingRequiredToBeInRange = Math.Min( remainingRequiredToBeInRange, maxToSendForThisSystem );

                    if ( this.ProcessWorkingLists( AttackerEntity, AttackerSystem, attackerSystemTypeData, can_seek, traceBuffer ) )
                        return;
                }
            }
            finally
            {
                if (ShowTargetInfo)
                    AttackerSystem.PotentialTargetToPriorityLookup.SwitchConstructionToDisplay();
            }
        }

        #region ProcessWorkingLists
        private bool ProcessWorkingLists(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, EntitySystemTypeData attackerSystemTypeData, bool can_roam, ArcenCharacterBuffer traceBuffer )
        {
            //Note that the workingFullList has entries in it
#region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t\t" ).Add( "ProcessWorkingLists, we have " + workingFullList.Count + " full entries to consider and " + workingShortList.Count + " short entries" );
            }
#endregion

            //infinite range
            if (AttackerSystem.DataForMark.BaseRange > 99999)
            {
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "sniper range" );
                }

                this.FindTopEntriesForSystem_Shortlist_ByPreferredTypeToShoot( AttackerEntity, AttackerSystem );
                if (this.ProcessShortlistIntoTargetRankingIgnoringRange(AttackerSystem, false, traceBuffer))
                {
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "path a" );
                    }

                    return true;
                }
                this.FindTopEntriesForSystem_Shortlist_VeryImportant(AttackerEntity, AttackerSystem, traceBuffer);
                if (this.ProcessShortlistIntoTargetRankingIgnoringRange(AttackerSystem, false))
                {
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "path b" );
                    }
                                        
                    return true;
                }
                //shortcut out if we have enough targets already
                //if ( finalListToSend_Targetable.Count >= attackerSystemTypeData.ShotsPerSalvo && finalChoicesForFRD.Count >= 1 )
                //    return true;
                this.FindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange(AttackerEntity, AttackerSystem, traceBuffer);
                if (this.ProcessShortlistIntoTargetRankingIgnoringRange(AttackerSystem, false))
                {
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "path c" );
                    }

                    return true;
                }
                this.FindTopEntriesForSystem_Shortlist_TheRest();
                if (this.ProcessShortlistIntoTargetRankingIgnoringRange(AttackerSystem, false, traceBuffer))
                {
                                        if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "path d" );
                    }

                    return true;
                }
            }
            else //regular range
            {
                //  WM's Notes:
                //  Passing true to the function causes the target ship to be added to the possible attack targets
                //  Passing false to the function only seems to add the ship to future FRD movements however
                //  Knowing this, Important + 3x Range should be false(?), HalfImportant + 4x Range / any range false
                //  Others should have true passed to the function so they get evaluated for salvos
                //  The problem with multitarget not working correctly sometimes is that "TheRest" had false passed to it
                //  Meaning that when a high priority target was present, strikecraft would end up in TheRest and not get salvo evaluation

                bool mustBeInRangeForFRD = (can_roam == false);
                bool IsStuffWithinANearRange = true;
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Start to Process Working Lists for regular range units; mustBeInRangeForFRD " + mustBeInRangeForFRD + " can_roam " + can_roam );
                }
                #endregion

                this.FindTopEntriesForSystem_Shortlist_ImportantAndInRange(AttackerEntity, AttackerSystem, traceBuffer);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are important and in range" );
                }
                #endregion
                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }
                //shortcut out if we have enough targets already
                //if ( finalListToSend_Targetable.Count >= attackerSystemTypeData.ShotsPerSalvo && finalChoicesForFRD.Count >= 1 )
                //    return true;
                this.FindTopEntriesForSystem_Shortlist_ImportantAndInTripleRange(AttackerEntity, AttackerSystem, traceBuffer);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are important and in triple range" );
                }
                #endregion
                
                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, !IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }

                this.FindTopEntriesForSystem_Shortlist_VeryImportant(AttackerEntity, AttackerSystem);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are Very Important." );
                }
                #endregion
                
                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ))
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }

                this.FindTopEntriesForSystem_Shortlist_HalfImportantAndInRange(AttackerEntity, AttackerSystem);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are half important and in range." );
                }
                #endregion
                
                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }

                this.FindTopEntriesForSystem_Shortlist_HalfImportantAndInQuadrupleRange(AttackerEntity, AttackerSystem);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are half important and in quadruple range." );
                }
                #endregion

                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, !IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }

                this.FindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange(AttackerEntity, AttackerSystem);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are half important and in any range." );
                }
                #endregion

                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, !IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }
                this.FindTopEntriesForSystem_Shortlist_TheRest();
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t" ).Add( "Code has calculated " + workingShortList.Count + " short list entries that are 'the rest'." );
                }
                #endregion

                if (this.ProcessShortlistIntoTargetRanking(AttackerSystem, attackerSystemTypeData, IsStuffWithinANearRange, mustBeInRangeForFRD, false, traceBuffer ) )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t" ).Add( "Found " + finalListToSend_Targetable.Count + " targets, " );
                    }
                    #endregion
                    return true;
                }
            }

            workingShortList.Clear();
            workingFullList.Clear();
            return false;
        }
        #endregion

        #region CalculateCoreImportance
        //This is considered the top priority for the "normal" range of priority items
        public const float BASE_NORMALIZATION_FOR_XML_IMPORTANCE = 2000f;
        //This is the top priority for things that are game-changingly unique.
        public const float EXTENDED_NORMALIZATION_FOR_XML_IMPORTANCE = 8000f;
        //This is the top priority for things that are game-changingly unique.
        public const float EXTENDED_NORMALIZATION_FOR_XML_IMPORTANCE_MINUS_STANDARD = EXTENDED_NORMALIZATION_FOR_XML_IMPORTANCE - BASE_NORMALIZATION_FOR_XML_IMPORTANCE;

        public const float FACTOR_IMPORTANCE_FOR_XML = 10f;
        public const float FACTOR_IMPORTANCE_FOR_EXTRA_HIGH_PRIORITY_XML = 30f;
        public const float FACTOR_IMPORTANCE_FOR_DAMAGE = 2f;
        public const float FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT = 10f;
        public const float FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT = 20f;
        public const float FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT = 50f;
        public const float FACTOR_IMPORTANCE_FOR_ADDED_SUPER_HUGE_EFFECT = 150f;

        /// <summary>
        /// If this returns a negative number, then this target won't even be considered at all, so do that with care.
        /// If this returns 0, it will only target this if everything else is already gone
        /// </summary>
        private int CalculateCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, EntitySystemTypeData attackerSystemTypeData, 
            GameEntity_Squad DefenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer)
        {
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t\t" ).Add( ">CalculateCoreImportance, DefenderEntity:" ).Add( DefenderEntity.ToString() );
            }
            #endregion
            if (defenderTargetingInfo.FactionPriorityPart <= 0)
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "return 0 because defenderTargetingInfo.FactionPriorityPart <= 0" );
                }
                #endregion
                return 0;
            }
            
            // idea: don't decloak yourself auto-attacking
            /*
            if (AttackerEntity.GetCurrentCloakingPoints() > 0)
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "return -1 because AttackerEntity is cloaked" );
                }
                #endregion
                return -1;
            }
            */
            
            int damageToDo = AttackerSystem.GetAttackPowerAgainst(DefenderEntity, null, false, 0, AttackerSystem.GetMaxCompressedShotsAtThisTarget(DefenderEntity)  );
            if ( damageToDo <= 0 )
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "return -1 because AttackerSystem.GetAttackPowerAgainst(DefenderEntity, null, false) <= 0" );
                }
                #endregion
                return -1; //hey, we can't even hurt this!  Ignore it!
            }
            
            float importanceWithAdjustment = defenderTargetingInfo.FactionPriorityPart;
            
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment = defenderTargetingInfo.FactionPriorityPart ; " + importanceWithAdjustment );
            }
            #endregion

            #region Normalize From The damageToDo to importanceWithAdjustment
            if ( AttackerSystem.DataForMark.BaseTheoreticalMaxDamage <= 0 ) //somehow it wasn't supposed to do damage but does, so assume it's fully loaded
            {
                importanceWithAdjustment += FACTOR_IMPORTANCE_FOR_DAMAGE;
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment += FACTOR_IMPORTANCE_FOR_DAMAGE ; " + importanceWithAdjustment );
                }
                #endregion
            }
            else //otherwise do it based on the percentage damage this will do against the target out of its theoretical max, ignoring outside factors
            {
                importanceWithAdjustment += (((float) damageToDo / AttackerSystem.DataForMark.CalculateActualTheoreticalMaxDamagePerShot( AttackerEntity, AttackerEntity.FleetMembership ).ToFloatNonSim()) * FACTOR_IMPORTANCE_FOR_DAMAGE);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment += (((float) damageToDo / AttackerSystem.DataForMark.CalculateActualTheoreticalMaxDamage( AttackerEntity ).ToFloatNonSim()) * FACTOR_IMPORTANCE_FOR_DAMAGE) ; " + importanceWithAdjustment );
                }
                #endregion
            }
            #endregion

            //if the system has an added target evaluator on it, then use that to get added data
            if ( attackerSystemTypeData.TargetEvaluator != null && attackerSystemTypeData.TargetEvaluator.Implementation != null)
            {
                importanceWithAdjustment += attackerSystemTypeData.TargetEvaluator.Implementation.CalculateNormalizedAdditionToCoreImportance(AttackerEntity, AttackerSystem, DefenderEntity, factionPriorityAIBased);
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment += attackerSystemTypeData.TargetEvaluator.Implementation.CalculateNormalizedAdditionToCoreImportance(AttackerEntity, AttackerSystem, DefenderEntity, factionPriorityAIBased); " + importanceWithAdjustment );
                }
                #endregion

                if ( importanceWithAdjustment < 0 )
                {
                    importanceWithAdjustment = 0.01f;
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment < 0" );
                    }
                    #endregion
                    
                    //return -1;
                }
            }

            if ( defenderTargetingInfo.FactionPriorityDivisor > 1 )
            {
                importanceWithAdjustment /= defenderTargetingInfo.FactionPriorityDivisor;
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "importanceWithAdjustment < 0.01f, so now = " + importanceWithAdjustment );
                }
                #endregion
            }

            bool isMySpecialTarget = false;
            List<int> currentTractorsHittingMe = AttackerEntity.CurrentTractorSourcesHittingThis.GetDisplayList();
            if ( currentTractorsHittingMe.Count > 0 )
            {
                for ( int i = 0; i < currentTractorsHittingMe.Count; i++ )
                {
                    if ( currentTractorsHittingMe[i] == DefenderEntity.PrimaryKeyID )
                    {
                        isMySpecialTarget = true;
                        importanceWithAdjustment *= 10f; //strongly prioritize any units tractoring me
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "AttackerEntity.StrongestTractorPullingThis == DefenderEntity.PrimaryKeyID, so importanceWithAdjustment *= 10f ; " + importanceWithAdjustment );
                        }
                        #endregion
                        break;
                    }
                }
            }

            if ( AttackerEntity.PreferredEntityTypeDataForTargeting != null && 
                 AttackerEntity.PreferredEntityTypeDataForTargeting == DefenderEntity.TypeData )
            {
                isMySpecialTarget = true;
                importanceWithAdjustment *= 100f; //even more strongly prioritize any units of the type that the player set as preferred
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "AttackerEntity.PreferredEntityTypeDataForTargeting != null && AttackerEntity.PreferredEntityTypeDataForTargeting == DefenderEntity.TypeData, so importanceWithAdjustment *= 100f ; " + importanceWithAdjustment );
                }
                #endregion
            }

            if ( !isMySpecialTarget )
            {
                int defenderHealth = DefenderEntity.GetAbsoluteDurabilityOfMyselfAndStack();
                if ( damageToDo > defenderHealth * 10 )
                {
                    //deprioritize things that we dramatically overkill
                    importanceWithAdjustment /= 5;
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "damageToDo > defenderHealth * 10, so importanceWithAdjustment /= 5 ; " + importanceWithAdjustment );
                    }
                    #endregion
                }

                if ( DefenderEntity.TypeData.SpecialType == SpecialEntityType.Frigate && 
                     DefenderEntity.GetFactionTypeSafe() == FactionType.Player )
                {
                    importanceWithAdjustment /= 3;
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "DefenderEntity.TypeData.SpecialType == SpecialEntityType.Frigate && DefenderEntity.GetFactionTypeSafe() == FactionType.Player, so importanceWithAdjustment /= 3 ; " + importanceWithAdjustment );
                    }
                    #endregion
                }
                
                int numProtectingForcefields = DefenderEntity.ProtectingShields.Count;
                if ( numProtectingForcefields > 0 && !AttackerSystem.HasBonusToForcefieldOf(DefenderEntity))
                {
                    int mul = 3;
                    if ( this.factionPriorityAIBased )
                    {
                        //AIs are more reluctant to attack shields than non-AIs are
                        mul = 5;
                    }
                    
                    int div = mul * numProtectingForcefields;
                    importanceWithAdjustment /= div;
                    #region Tracing
                    if ( traceBuffer != null ) 
                    {
                        traceBuffer
                            .Add( "\n\t\t\t\t" )
                            .Add("DefenderEntity.numProtectingForcefields == ").Add(numProtectingForcefields)
                            .Add(" and targeting is ").Add(this.factionPriorityAIBased?"ai-style":"not ai-style")
                            .Add(", so importanceWithAdjustment /= ").Add(div)
                            .Add("(").Add(numProtectingForcefields).Add("*").Add(mul).Add(") ; ").Add(importanceWithAdjustment);
                    }
                    #endregion
                }
            }

            // estimate time to get into range to shoot this target
            // (which would be zero if already in range)
            // the farther away the lower priority
            {
                int range = AttackerSystem.GetRangeToShoot(DefenderEntity);
                float dist = defenderTargetingInfo.Distance; 
                float to_be_in_range = dist - range;
                
                traceBuffer?.Add( "\n\t\t\t\t" ).Add( string.Format("### dsqr={0} dist={1} range={2} torange={3}",
                    defenderTargetingInfo.DistanceSqr, dist, range, to_be_in_range ) );

                if (range >= 0)
                {
                    float norm = 0.0f;
                    var sign = Mathf.Sign(to_be_in_range);
                    var dif = Mathf.Abs(to_be_in_range);
                    
                    float scale = 1.0f;
                    float MulPerSec = 0.0f;
                    if (dif < 0.0001f)
                    {
                        norm = 0;
                        scale = 1;
                        sign = 1;
                    }
                    else
                    {
                        float speed = AttackerEntity.CalculatedSpeed;
                        if (speed < 1)
                            speed = 1.0f;
                        
                        norm = dif / speed;
                        if (sign > 0)
                        {
                            MulPerSec = MulPerSecFarther;
                        }
                        else
                        {
                            MulPerSec = MulPerSecCloser;
                        }

                        scale = (float)Mathf.Pow(MulPerSec, norm);
                    }
                    
                    float adjustedAmount = importanceWithAdjustment * scale;
                    
                    traceBuffer?.Add( "\n\t\t\t\t" ).Add( string.Format("### {0} === to_be_in_range={1} secs={2} MulPerSec={3} scale={4}  === {5} -> {6}",
                        (adjustedAmount>importanceWithAdjustment?"increased":adjustedAmount<importanceWithAdjustment?"decreased":"unchanged"),
                        to_be_in_range, norm, MulPerSec, scale, importanceWithAdjustment, adjustedAmount ) );
                    
                    importanceWithAdjustment = adjustedAmount;
                }
            }

            /*
            if ( (sentinelsExternal == null || sentinelsExternal.AIDifficulty.Difficulty >= 7) && //only for high AI difficulties
                 AIWar2GalaxySettingQuickAccess.MethodicalFRD && !DefenderEntity.GetHasAnyWeaponInRange( AttackerEntity ) )
            {
                importanceWithAdjustment /= 50f; //if this unit can't actually hit me, de-prioritize it (to help make drones less crazy about antagonizing things
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "!DefenderEntity.GetHasAnyWeaponInRange( AttackerEntity ), so importanceWithAdjustment /= 50f ; " + importanceWithAdjustment );
                }
                #endregion
            }
            */

            // allow ai type to modify if they desire
            AttackerEntity.GetFactionOrNull_Safe()?.TryGetAISentinelsCoreData()?.SentinelInfo.AIType.Implementation.CalculateCoreImportance(ref importanceWithAdjustment, AttackerEntity, AttackerSystem, attackerSystemTypeData, DefenderEntity, defenderTargetingInfo, traceBuffer);

            return UnityEngine.Mathf.RoundToInt(importanceWithAdjustment * 100);
        }
        #endregion

        #region ProcessShortlistIntoTargetRanking
        private bool ProcessShortlistIntoTargetRanking(EntitySystem AttackerSystem, EntitySystemTypeData attackerSystemTypeData, bool IsStuffWithinANearRange, bool MustBeInRangeForFRD, bool IgnoreNonCombatantsForFRDChoices, ArcenCharacterBuffer traceBuffer )
        {
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t" ).Add( ">ProcessShortlistIntoTargetRanking" );
            }
            #endregion
            if (workingFullList.Count <= 0)
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( "return true because workingFullList.Count <= 0" );
                }
                #endregion
                return true; //nothing more to add
            }
            if (workingShortList.Count <= 0)
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( "return false because workingShortList.Count <= 0" );
                }
                #endregion
                return false;
            }

            bool preferMoreDistantStuff = attackerSystemTypeData.HitsAllIntersectingTargets && IsStuffWithinANearRange;
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t\t" ).Add( "sorting with preferMoreDistantStuff:").Add( preferMoreDistantStuff );
            }
            #endregion
            workingShortList.Sort(
                static (a,b)=>
                {
                    int val = 0;
                    /*
                    if (preferMoreDistantStuff)
                    {
                         //go for the farther stuff first
                         val = right.Info.Distance.CompareTo(left.Info.Distance);
                        if (val != 0)
                            return val;
                    }
                    else
                    {
                         //go for the closest stuff first
                         val = left.Info.Distance.CompareTo(right.Info.Distance);
                        if (val != 0)
                            return val;
                    }
                    */
                    
                    //if it's equally close, then sort by importance
                    val = b.Info.Importance.CompareTo(a.Info.Importance);
                    if (val != 0)
                        return val;
                    
                    //if somehow it is still equal, sort by just the essentially random unique ID to ensure consistent results
                    val = a.Squad.NonSimPermanentUniqueID.CompareTo(b.Squad.NonSimPermanentUniqueID);
                    return val;
                });

            Candidate candidate;
            int numberAdded = 0;
            for (int i = 0; i < workingShortList.Count; i++)
            {
                candidate = workingShortList[i];
                var e = candidate.Squad;
                var info = candidate.Info;
                if ( e == null )
                    continue;
                
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( ">Evaluating " ).Add( candidate.ToString() ).Add( " with IsStuffWithinANearRange:" ).Add( IsStuffWithinANearRange );
                }
                #endregion

                if ( finalListToSend_Targetable.Count >= maxToSendForThisSystem && finalChoicesForFRD.Count >= targetCountForFRDChoices)
                    break;
                
                #region finalListToSend_Targetable
                info.ToBeRemoved = true;
                numberAdded++;
                if (IsStuffWithinANearRange && finalListToSend_Targetable.Count < maxToSendForThisSystem)
                {
                    //this is in range, definitly add it to the things we can shoot!
                    if (info.IsInRange)
                    {
                        if (remainingRequiredToBeInRange > 0)
                            remainingRequiredToBeInRange--;
                        finalListToSend_Targetable.Add(candidate);
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually add to finalListToSend_Targetable because IsStuffWithinANearRange && finalListToSend_Targetable.Count < maxToSendForThisSystem && entityToAdd.Info.IsInRange" );
                        }
                        #endregion
                    }
                    else //this is not in range!
                    {
                        //only add it to the list if we would still have enough room for the remaining required to be in range
                        if ( maxToSendForThisSystem - finalListToSend_Targetable.Count > remainingRequiredToBeInRange )
                        {
                            finalListToSend_Targetable.Add( candidate );
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually add to finalListToSend_Targetable because IsStuffWithinANearRange && finalListToSend_Targetable.Count < maxToSendForThisSystem && !entityToAdd.Info.IsInRange && maxToSendForThisSystem - finalListToSend_Targetable.Count > remainingRequiredToBeInRange" );
                            }
                            #endregion
                        }
                        else
                        {
                            #region Tracing
                            if ( traceBuffer != null )
                            {
                                traceBuffer.Add( "\n\t\t\t\t" ).Add( "do not add to finalListToSend_Targetable because IsStuffWithinANearRange && finalListToSend_Targetable.Count < maxToSendForThisSystem && !entityToAdd.Info.IsInRange && maxToSendForThisSystem - finalListToSend_Targetable.Count <= remainingRequiredToBeInRange" );
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                // add to frd list (or not)
                #region finalChoicesForFRD
                if ( finalChoicesForFRD.Count >= targetCountForFRDChoices )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "do not add to finalChoicesForFRD because finalChoicesForFRD.Count >= targetCountForFRDChoices ( " ).Add( finalChoicesForFRD.Count ).Add( " >= " ).Add( targetCountForFRDChoices ).Add( " )" );
                    }
                    #endregion
                }
                else 
                if (!IgnoreNonCombatantsForFRDChoices || 
                    candidate.Squad.TypeData.IsCombatant || 
                    !foundAnyCombatants )
                {
                    if ( !MustBeInRangeForFRD || candidate.Info.IsInRange )
                    {
                        finalChoicesForFRD.Add( candidate );

                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually add to finalChoicesForFRD" );
                        }
                        #endregion
                    }
                    else
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "do not add to finalChoicesForFRD because MustBeInRangeForFRD && !entityToAdd.Info.IsInRange" );
                        }
                        #endregion
                    }
                }
                else
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "do not add to finalChoicesForFRD because IgnoreNonCombatantsForFRDChoices && !entityToAdd.Squad.TypeData.IsCombatant && foundAnyCombatants" );
                    }
                    #endregion
                }
                #endregion
            }

            if (numberAdded > 0)
                this.ProcessFromFullListIntoFullListAlt();

            workingShortList.Clear();

            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t\t" ).Add( "finalListToSend_Targetable.Count:").Add( finalListToSend_Targetable.Count)
                    .Add( " ; maxToSendForThisSystem:").Add( maxToSendForThisSystem )
                    .Add( " ; finalChoicesForFRD.Count:" ).Add( finalChoicesForFRD.Count )
                    .Add( " ; targetCountForFRDChoices:" ).Add( targetCountForFRDChoices )
                    .Add( " ; workingFullList.Count:" ).Add( workingFullList.Count )
                    ;
            }
            #endregion

            return (finalListToSend_Targetable.Count >= maxToSendForThisSystem && finalChoicesForFRD.Count >= targetCountForFRDChoices) || workingFullList.Count <= 0;
        }
        #endregion

        #region ProcessShortlistIntoTargetRankingIgnoringRange
        private bool ProcessShortlistIntoTargetRankingIgnoringRange(EntitySystem AttackerSystem, bool MustBeInRangeForFRD, ArcenCharacterBuffer traceBuffer = null)
        {
            if (workingFullList.Count <= 0)
                return true; //nothing more to add
            if (workingShortList.Count <= 0)
                return false;
            if ( traceBuffer != null )
                traceBuffer.Add( "\n\t\t\t" ).Add("process " + workingShortList.Count + " short list and " + workingFullList.Count + " full list" );
            int attackerMarker = AttackerSystem.ParentEntity.NonSimPermanentUniqueID % 10;

           cb_tlpAttackerMarker = attackerMarker;
           workingShortList.Sort(
               static (a,b)=>
               {
                   int val = 0;
                   
                   //sort by importance
                   val = b.Info.Importance.CompareTo(a.Info.Importance);
                   if (val != 0)
                       return val;
                   
                   //sort by marker being equal
                   val = (a.Squad.NonSimPermanentUniqueID % 10 == cb_tlpAttackerMarker).CompareTo((b.Squad.NonSimPermanentUniqueID % 10 == cb_tlpAttackerMarker));
                   if (val != 0)
                       return val;

                   //if still equal, sort by just the essentially random unique ID to ensure consistent results
                   val = a.Squad.NonSimPermanentUniqueID.CompareTo(b.Squad.NonSimPermanentUniqueID);
                   return val;
               });

            Candidate candidate;
            int numberAdded = 0;
            for (int i = 0; i < workingShortList.Count; i++)
            {
                if (finalListToSend_Targetable.Count >= maxToSendForThisSystem && finalChoicesForFRD.Count >= targetCountForFRDChoices)
                    return true;
                
                candidate = workingShortList[i];
                if ( candidate.Squad == null )
                    continue;
                
                candidate.Info.ToBeRemoved = true;
                numberAdded++;
                
                if (finalListToSend_Targetable.Count < maxToSendForThisSystem)
                    finalListToSend_Targetable.Add(candidate);
                
                if (finalChoicesForFRD.Count < targetCountForFRDChoices)
                {
                    if (!MustBeInRangeForFRD || candidate.Info.IsInRange)
                        finalChoicesForFRD.Add(candidate);
                }
            }
            
            if ( traceBuffer != null )
                traceBuffer.Add( "\n\t\t\t" ).Add( "We have " + finalChoicesForFRD.Count + " FRD choices" );

            if (numberAdded > 0)
                this.ProcessFromFullListIntoFullListAlt();
            
            workingShortList.Clear();
            
            return workingFullList.Count == 0 || 
                   (finalListToSend_Targetable.Count >= maxToSendForThisSystem && 
                    finalChoicesForFRD.Count >= targetCountForFRDChoices);
        }
        #endregion

        #region ProcessFromFullListIntoFullListAlt
        private void ProcessFromFullListIntoFullListAlt()
        {
            workingFullListAlt.Clear();
            for (int i = 0; i < workingFullList.Count; i++)
            {
                var candidate = workingFullList[i];
                if ( candidate.Squad == null )
                    continue;
                if (!candidate.Info.ToBeRemoved)
                    workingFullListAlt.Add(candidate);
            }
            workingFullList.Clear();
            List<Candidate> list = workingFullList;
            workingFullList = workingFullListAlt;
            workingFullListAlt = list;
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_ImportantAndInRange
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_ImportantAndInRange(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, ArcenCharacterBuffer traceBuffer)
        {
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t" ).Add( ">FindTopEntriesForSystem_Shortlist_ImportantAndInRange" );
            }
            #endregion
            workingShortList.Clear();

            float eightyPercentPriority = highestPriorityFound * 0.8f;

            Candidate entityForConsideration;
            //if we have a few units nearby, wait and kill EVERYTHING before you move on
            bool anyOverkilledFound = false;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( ">" ).Add( i ).Add( ":" ).Add( entityForConsideration.ToString() )
                        .Add( "; Importance:" ).Add( entityForConsideration.Info.Importance )
                        .Add( "; BeingOverkilled:" ).Add( entityForConsideration.Info.BeingOverkilled )
                        .Add( "; IsInRange:" ).Add( entityForConsideration.Info.IsInRange )
                        ;
                }
                #endregion

                //only consider stuff that is within 80% of the top priority
                if (entityForConsideration.Info.Importance < eightyPercentPriority)
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skip because entityForConsideration.Info.Importance < eightyPercentPriority" );
                    }
                    #endregion
                    continue;
                }
                //prefer to skip entities being overkilled
                if ( entityForConsideration.Info.BeingOverkilled )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skip because entityForConsideration.Info.BeingOverkilled" );
                    }
                    #endregion
                    anyOverkilledFound = true;
                    continue;
                }
                //and stuff that is within range
                if (!entityForConsideration.Info.IsInRange)
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skip because !entityForConsideration.Info.IsInRange" );
                    }
                    #endregion
                    continue;
                }
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually adding to workingShortList" );
                }
                #endregion
                workingShortList.Add(entityForConsideration);
            }
            if ( workingShortList.Count == 0 && anyOverkilledFound)
            {
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( "workingShortList.Count == 0 && anyOverkilledFound, so retry while ignoring the overkill rule" );
                }
                #endregion
                for ( int i = 0; i < workingFullList.Count; i++)
                {
                    entityForConsideration = workingFullList[i];
                    if ( entityForConsideration.Squad == null )
                        continue;
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t" ).Add( ">" ).Add( i ).Add( ":" ).Add( entityForConsideration.ToString() )
                            .Add( "; Importance:" ).Add( entityForConsideration.Info.Importance )
                            .Add( "; IsInRange:" ).Add( entityForConsideration.Info.IsInRange )
                            ;
                    }
                    #endregion
                    //only consider stuff that is within 80% of the top priority
                    if ( entityForConsideration.Info.Importance < eightyPercentPriority )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "skip because entityForConsideration.Info.Importance < eightyPercentPriority" );
                        }
                        #endregion
                        continue;
                    }
                    //and stuff that is within range
                    if ( !entityForConsideration.Info.IsInRange )
                    {
                        #region Tracing
                        if ( traceBuffer != null )
                        {
                            traceBuffer.Add( "\n\t\t\t\t" ).Add( "skip because !entityForConsideration.Info.IsInRange" );
                        }
                        #endregion
                        continue;
                    }
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually adding to workingShortList" );
                    }
                    #endregion
                    workingShortList.Add(entityForConsideration);
                }
            }
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_ImportantAndInTripleRange
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_ImportantAndInTripleRange(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, ArcenCharacterBuffer traceBuffer)
        {
            #region Tracing
            if ( traceBuffer != null )
            {
                traceBuffer.Add( "\n\t\t" ).Add( ">FindTopEntriesForSystem_Shortlist_ImportantAndInTripleRange; workingFullList.Count:" ).Add( workingFullList.Count );
            }
            #endregion
            workingShortList.Clear();
            float eightyPercentPriority = highestPriorityFound * 0.8f;
            
            int tripleRange = AttackerSystem.DataForMark.CalculateActualRange( AttackerEntity ) * 3;

            Candidate entityForConsideration;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t" ).Add( ">" ).Add( i ).Add( ":" ).Add( entityForConsideration.ToString() )
                        .Add( "; Importance:" ).Add( entityForConsideration.Info.Importance )
                        .Add( "; BeingOverkilled:" ).Add( entityForConsideration.Info.BeingOverkilled )
                        .Add( "; IsInRange:" ).Add( entityForConsideration.Info.IsInRange )
                        ;
                }
                #endregion
                //only consider stuff that is within 80% of the top priority
                if ( entityForConsideration.Info.Importance < eightyPercentPriority)
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skipping because entityForConsideration.Info.Importance < eightyPercentPriority");
                    }
                    #endregion
                    continue;
                }
                //and stuff that is within triple range
                if (entityForConsideration.Info.Distance > tripleRange)
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skipping because entityForConsideration.Info.Distance > tripleRange" );
                    }
                    #endregion
                    continue;
                }
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                {
                    #region Tracing
                    if ( traceBuffer != null )
                    {
                        traceBuffer.Add( "\n\t\t\t\t" ).Add( "skipping because entityForConsideration.Info.BeingOverkilled" );
                    }
                    #endregion
                    continue;
                }
                #region Tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( "\n\t\t\t\t" ).Add( "actually adding to workingShortList" );
                }
                #endregion
                workingShortList.Add(entityForConsideration);
            }
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_VeryImportant
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_VeryImportant(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, ArcenCharacterBuffer traceBuffer = null)
        {
            workingShortList.Clear();
//            bool debug = false;
            // if(AttackerEntity.Planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() && AttackerEntity.PrimaryKeyID == entityToFollow)
            //     debug = true;

            float ninetyPercentPriority = highestPriorityFound * 0.9f;
            if ( traceBuffer != null )
                traceBuffer.Add( "\n\t\t\t" ).Add("FindTopEntriesForSystem_Shortlist_VeryImportant " + workingShortList.Count + " short list and " + workingFullList.Count + " full list" );
            Candidate entityForConsideration;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //only consider stuff that is within 90% of the top priority
                if (entityForConsideration.Info.Importance < ninetyPercentPriority)
                    continue;

                float importanceFromXML = (factionPriorityAIBased ? entityForConsideration.Squad.TypeData.PriorityAsAITarget.ImportanceMetric :
                    entityForConsideration.Squad.TypeData.PriorityAsFRDTarget.ImportanceMetric);
//                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("FindTopEntriesForSystem_Shortlist_VeryImportant potential " + entityForConsideration.ToString() + " importance " + importanceFromXML, Verbosity.DoNotShow );
                //only stuff in the very important part of the spectrum of xml importance
                if (importanceFromXML < BASE_NORMALIZATION_FOR_XML_IMPORTANCE)
                {
//                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("FindTopEntriesForSystem_Shortlist_VeryImportant potential " + entityForConsideration.ToString() + " importance < base " + BASE_NORMALIZATION_FOR_XML_IMPORTANCE, Verbosity.DoNotShow );
                    continue;
                }
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                {
//                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_VeryImportant " + entityForConsideration.ToString() + " is being overkilled", Verbosity.DoNotShow );
                    continue;
                }
//                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_VeryImportant adding " + entityForConsideration.ToString() + " to the working shortlist", Verbosity.DoNotShow );
                workingShortList.Add(entityForConsideration);
            }
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_ByPreferredTypeToShoot
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.  
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_ByPreferredTypeToShoot( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem )
        {
            workingShortList.Clear();
            //            bool debug = false;
            // if(AttackerEntity.Planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() && AttackerEntity.PrimaryKeyID == entityToFollow)
            //     debug = true;

            if ( AttackerEntity.PreferredEntityTypeDataForTargeting == null )
                return; //We ONLY care about PreferredEntityTypeDataForTargeting types.
            if ( AttackerEntity.GetFactionTypeSafe() != FactionType.Player )
                return; //also has to be players doing the targeting


            Candidate entityForConsideration;
            for ( int i = 0; i < workingFullList.Count; i++ )
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //only consider stuff that is my chosen type
                if ( AttackerEntity.PreferredEntityTypeDataForTargeting != entityForConsideration.Squad.TypeData )
                    continue;
                
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                {
                    //                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_VeryImportant " + entityForConsideration.ToString() + " is being overkilled", Verbosity.DoNotShow );
                    continue;
                }
                //                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_VeryImportant adding " + entityForConsideration.ToString() + " to the working shortlist", Verbosity.DoNotShow );
                workingShortList.Add( entityForConsideration );
            }
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_HalfImportantAndInRange
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_HalfImportantAndInRange(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, ArcenCharacterBuffer traceBuffer = null)
        {
            workingShortList.Clear();

            float fiftyPercentPriority = highestPriorityFound * 0.5f;
            if ( traceBuffer != null )
                traceBuffer.Add( "\n\t\t\t" ).Add("FindTopEntriesForSystem_Shortlist_HalfImportantAndInRange " + workingShortList.Count + " short list and " + workingFullList.Count + " full list" );

            Candidate entityForConsideration;
            bool anyOverkilledFound = false;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //only consider stuff that is within 50% of the top priority
                if (entityForConsideration.Info.Importance < fiftyPercentPriority)
                    continue;
                //prefer to skip entities being overkilled
                if ( entityForConsideration.Info.BeingOverkilled )
                {
                    anyOverkilledFound = true;
                    continue;
                }

                //and stuff that is within range
                if (!entityForConsideration.Info.IsInRange)
                    continue;
                workingShortList.Add(entityForConsideration);
            }
            if ( workingShortList.Count == 0 && anyOverkilledFound)
            {
                for (int i = 0; i < workingFullList.Count; i++)
                {
                    entityForConsideration = workingFullList[i];
                    if ( entityForConsideration.Squad == null )
                        continue;
                    //only consider stuff that is within 50% of the top priority
                    if (entityForConsideration.Info.Importance < fiftyPercentPriority)
                        continue;

                    //and stuff that is within range
                    if (!entityForConsideration.Info.IsInRange)
                        continue;
                    workingShortList.Add(entityForConsideration);
                }
                
            }

        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_HalfImportantAndInQuadrupleRange
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_HalfImportantAndInQuadrupleRange(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem)
        {
            workingShortList.Clear();

            float fiftyPercentPriority = highestPriorityFound * 0.5f;
            int quadrupleRange = AttackerSystem.DataForMark.CalculateActualRange( AttackerEntity ) * 4;

            Candidate entityForConsideration;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //only consider stuff that is within 50% of the top priority
                if (entityForConsideration.Info.Importance < fiftyPercentPriority)
                    continue;
                //and stuff that is within quadruple range
                if (entityForConsideration.Info.Distance > quadrupleRange)
                    continue;
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                    continue;
                workingShortList.Add(entityForConsideration);
            }
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange
        /// <summary>
        /// This could very validly return no entries at all, and that's ok!
        /// We'll catch them next time.
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, ArcenCharacterBuffer traceBuffer = null )
        {
            workingShortList.Clear();
//            bool debug = false;
            // if(AttackerEntity.Planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() && AttackerEntity.PrimaryKeyID == entityToFollow)
            //     debug = true;

            float fiftyPercentPriority = highestPriorityFound * 0.5f;

            Candidate entityForConsideration;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange for " + AttackerEntity.ToString() + " potential " + entityForConsideration.ToString(), Verbosity.DoNotShow );
                //only consider stuff that is within 50% of the top priority
                if (entityForConsideration.Info.Importance < fiftyPercentPriority)
                    continue;
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                {
//                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tskip; overkill", Verbosity.DoNotShow );
                    continue;
                }
//                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tFindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange for " + AttackerEntity.ToString() + " adding " + entityForConsideration.ToString() + " to list", Verbosity.DoNotShow );
                workingShortList.Add(entityForConsideration);
            }
//            if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("FindTopEntriesForSystem_Shortlist_HalfImportantAndInAnyRange has " + workingShortList.Count + " targets on it", Verbosity.DoNotShow );
        }
        #endregion

        #region FindTopEntriesForSystem_Shortlist_TheRest
        /// <summary>
        /// Whatever else there is, return it
        /// </summary>
        private void FindTopEntriesForSystem_Shortlist_TheRest()
        {
            workingShortList.Clear();

            Candidate entityForConsideration;
            for (int i = 0; i < workingFullList.Count; i++)
            {
                entityForConsideration = workingFullList[i];
                if ( entityForConsideration.Squad == null )
                    continue;
                //skip overkilled units
                if ( entityForConsideration.Info.BeingOverkilled )
                    continue;
                workingShortList.Add(entityForConsideration);
            }
        }
        #endregion
    }
    #endregion

    #region TargetEvaluator(s)
    
    #region TargetEvaluator_LowestHealthAsMajorPrimary
    /// <summary>
    /// If my primary purpose is hitting low-health things, I will really focus on that.  Probably means better AOE targeting
    /// </summary>
    public class TargetEvaluator_LowestHealthAsMajorPrimary : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased )
        {
            int remainingHealth = DefenderEntity.GetCurrentHullPoints() + DefenderEntity.GetCurrentShieldPoints();
            if ( remainingHealth > 200000 ) //if above 200,000 health, then whatever
                return 1f;

            float baseMult = (1f - ((float)remainingHealth / (float)200000));
            return (baseMult * baseMult * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_SUPER_HUGE_EFFECT) +
                //also spread these out every 6 units, to spread it around!
                (AttackerEntity.NonSimPermanentUniqueID % 6 == DefenderEntity.NonSimPermanentUniqueID % 6 ? TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT : 0);
        }
    }
    #endregion

    #region TargetEvaluator_EngineDamageAsMajorPrimary
    /// <summary>
    /// If my primary purpose is engine-stunning, then by golly I'm going to stun him good
    /// </summary>
    public class TargetEvaluator_EngineDamageAsMajorPrimary : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            if (DefenderEntity.CurrentEngineStunSeconds >= attackerSystemTypeData.MaxEngineStunSeconds)
                return 0; //he's already stunned as much as we can, so don't bother!
            int amountICanStunHim = AttackerSystem.GetEngineStunAgainst(DefenderEntity);
            if (amountICanStunHim <= 0)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT; //he's immune to my wiles, so ignore him a bit!

            //the less stunned he is, the more I want to stun him -- by the huge multiplier, too!
            return ((1f -
                ((float)DefenderEntity.CurrentEngineStunSeconds / (float)attackerSystemTypeData.MaxEngineStunSeconds))
                * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_SUPER_HUGE_EFFECT) +
                //also spread these out every 10 units, to spread it around!
                (AttackerEntity.NonSimPermanentUniqueID % 10 == DefenderEntity.NonSimPermanentUniqueID % 10 ? TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT : 0);
        }
    }
    #endregion

    #region TargetEvaluator_EngineDamageAsMinorPart
    /// <summary>
    /// If I'm able to partly stun enemy engines, then keep track of whether or not they are that stunned
    /// </summary>
    public class TargetEvaluator_EngineDamageAsMinorPart : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            if (DefenderEntity.CurrentEngineStunSeconds >= attackerSystemTypeData.MaxEngineStunSeconds)
                return 0; //he's already stunned as much as we can, so don't bother!

            int amountICanStunHim = AttackerSystem.GetEngineStunAgainst(DefenderEntity);
            if (amountICanStunHim <= 0)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT; //he's immune to my wiles, so ignore him a bit!;

            //the less stunned he is, the more I want to stun him -- but only for minor effect
            return ((1f -
                ((float)DefenderEntity.CurrentEngineStunSeconds / (float)attackerSystemTypeData.MaxEngineStunSeconds))
                * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT) +
                //also spread these out every 10 units, to spread it around!
                (AttackerEntity.NonSimPermanentUniqueID % 10 == DefenderEntity.NonSimPermanentUniqueID % 10 ? TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT : 0);
        }
    }
    #endregion

    #region TargetEvaluator_Melee
    /// <summary>
    /// The ship has melee range... so I guess put some priority on stuff that is slower than it?
    /// </summary>
    public class TargetEvaluator_Melee : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            if ( AttackerSystem == null || !attackerSystemTypeData.FiresThroughEnemyShields )
            {
                //take away a lot depending on how many shields are on top of it, or if it has shields, since we want to focus on non-shields with these guys
                if ( DefenderEntity.GetIsProtectedByAnyForcefield() || DefenderEntity.GetHasBubbleForcefieldRightNow() )
                {
                    var count = DefenderEntity.ProtectingShields.Count + (DefenderEntity.GetHasBubbleForcefieldRightNow() ? 1 : 0);
                    if (DefenderEntity.GetHasBubbleForcefieldRightNow())
                        count++;
                    
                    return -1 * count * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT;
                }
            }

            float mySpeed = AttackerEntity.CalculatedSpeed;
            if (mySpeed <= 0)
                return 0; //I'm stopped!
            float speedOfTarget = DefenderEntity.CalculatedSpeed;
            if (speedOfTarget <= 0) //it's stopped, so return the full amount
                return TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT;
            if (speedOfTarget > mySpeed)
                return 0; //it's faster than us, yikes!
            //depending on how much faster I am, give it extra priority as a thing I should attack
            return ((mySpeed - speedOfTarget) / mySpeed) * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT;
        }
    }
    #endregion

    #region TargetEvaluator_ParalyzerAsPrimary
    /// <summary>
    /// If my primary purpose is paralysis, then by golly I'm going to do that
    /// </summary>
    public class TargetEvaluator_ParalyzerAsPrimary : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            int maxParalysis = ExternalConstants.Instance.MaxParalysisTime;
            if (DefenderEntity.CurrentParalysisSeconds >= maxParalysis)
                return 0; //he's already paralyzed as much as we can, so don't bother!
            int amountICanParalyzeHim = AttackerSystem.GetParalysisAttackPowerAgainst(DefenderEntity);
            if (amountICanParalyzeHim <= 0)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT; //he's immune to my wiles, so ignore him a bit!

            //the less paralyzed he is, the more I want to stun him -- by the huge multiplier, too!
            return ((1f -
                ((float)DefenderEntity.CurrentParalysisSeconds / (float)maxParalysis))
                * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_SUPER_HUGE_EFFECT) +
                //also spread these out every 10 units, to spread it around!
                (AttackerEntity.NonSimPermanentUniqueID % 10 == DefenderEntity.NonSimPermanentUniqueID % 10 ? TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT : 0);
        }
    }
    #endregion

    #region TargetEvaluator_ZombifyAsPrimary
    /// <summary>
    /// Zombify ships that can be, ignore ships that will already spawn a zombie.
    /// </summary>
    public class TargetEvaluator_ZombifyAsPrimary : TargetEvaluatorBase
    {
        private static DeathEffectType cachedZombificationEffect = null;

        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            bool hasDeathEffect = attackerSystemTypeData.HasAnyDeathEffectOffenses;
            int zombieAmount = 1;
            int zombieLimit = 1;

            if (!hasDeathEffect)
                return 0; //This weapon doesn't zombify, abort priority modification

            #region Zombification_Checklist
            //Zombie checklist from Zombification.cs, return lower importance for non-Zombifiable targets
            if (!DefenderEntity.TypeData.IsMobileCombatant )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsKingUnit )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsBattlestation )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsMobileFleetFlagship)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.GetImmuneToCapture())
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.CanReclaimPlanet)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.HasAnyWeaponDeathEffects)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsDrone )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;
            #endregion

            //Find and cache the Zombification death effect entry (table is static after load)
            if ( cachedZombificationEffect == null )
            {
                for (int i = 0; i < DeathEffectTypeTable.Instance.Rows.Count; i++)
                {
                    if (DeathEffectTypeTable.Instance.Rows[i].DescriptionPrefix.Equals("Zombification", StringComparison.OrdinalIgnoreCase))
                    {
                        cachedZombificationEffect = DeathEffectTypeTable.Instance.Rows[i];
                        break;
                    }
                }
            }
            if ( cachedZombificationEffect != null )
            {
                zombieLimit = cachedZombificationEffect.Scale;
                if ( !DefenderEntity.DeathEffectCausingDamageReceivedToEntity.TryGetValue( cachedZombificationEffect, out zombieAmount ) )
                    zombieAmount = 0;
                if ( zombieAmount >= zombieLimit )
                    return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;
            }

            //Successfully found non-Zombified, Zombifiable ship. Increase zombification targeting the closer it is to being zombified
            return TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT + 
                (zombieAmount/zombieLimit) * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

        }
    }
    #endregion

    #region TargetEvaluator_NecromancyAsPrimary
    /// <summary>
    /// Zombify ships that can be, ignore ships that will already spawn a zombie.
    /// </summary>
    public class TargetEvaluator_NecromancyAsPrimary : TargetEvaluatorBase
    {
        private static DeathEffectType cachedNecromancyEffect = null;

        public override float CalculateNormalizedAdditionToCoreImportance(GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased)
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            if (!attackerSystemTypeData.HasAnyDeathEffectOffenses) {
                return 0; //This weapon doesn't necromancy, abort priority modification
            }

            #region Necromancy_Checklist
            //Necromancy checklist from Necromancy.cs, return lower importance for non-Zombifiable targets
            if (!DefenderEntity.TypeData.IsMobileCombatant )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsKingUnit )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsBattlestation )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            if (DefenderEntity.TypeData.IsMobileFleetFlagship)
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

            DLC3GameEntityTypeDataExtension Entity_DLC3TypeData = DefenderEntity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            if ( Entity_DLC3TypeData?.ImmuneToNecromancy ?? false )
            {
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;
            }

            if ( NecromancerEmpireFactionDeepInfo.GetShipTypeToSummonViaNecromancy(DefenderEntity.TypeData) == NecromancyShipType.None )
            {
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT;
            }

            #endregion

            int necromancyAmount = 1;
            int necromancyLimit = 1;
            //Find and cache the Necromancy death effect entry (table is static after load)
            if ( cachedNecromancyEffect == null )
            {
                for (int i = 0; i < DeathEffectTypeTable.Instance.Rows.Count; i++)
                {
                    if (DeathEffectTypeTable.Instance.Rows[i].DescriptionPrefix.Equals("Necromancy", StringComparison.OrdinalIgnoreCase))
                    {
                        cachedNecromancyEffect = DeathEffectTypeTable.Instance.Rows[i];
                        break;
                    }
                }
            }
            if ( cachedNecromancyEffect != null )
            {
                necromancyLimit = cachedNecromancyEffect.Scale;
                if ( !DefenderEntity.DeathEffectCausingDamageReceivedToEntity.TryGetValue( cachedNecromancyEffect, out necromancyAmount ) )
                    necromancyAmount = 0;
                if ( necromancyAmount >= necromancyLimit )
                    return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;
            }

            //Successfully found non-Necormancied, Necromancable ship. Increase necromancifaction targeting the closer it is to being necromancied
            return TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT +
                (necromancyAmount/necromancyLimit) * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT;

        }
    }
    #endregion

    #region TargetEvaluator_CanKnockback
    public class TargetEvaluator_CanKnockback : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased )
        {
            int knockbackAmount = 0;
            knockbackAmount = AttackerSystem.GetKnockbackPowerAgainst( DefenderEntity );
            if ( knockbackAmount == 0 )
            {
                // Knockback amount returns exactly 0 if we can't knockback
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_MINOR_EFFECT * 0.1f;
            }
            else
            {
                // Found something that isn't tractored, shielded, immobile, or too heavy
                return TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT * 0.1f;
            }
        }
    }
    #endregion

    #region TargetEvaluator_WeaponJamAsPrimary
    /// <summary>
    /// If my primary purpose is weapon jamming, then by golly I'm going to do that
    /// </summary>
    public class TargetEvaluator_WeaponJamAsPrimary : TargetEvaluatorBase
    {
        public override float CalculateNormalizedAdditionToCoreImportance( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased )
        {
            EntitySystemTypeData attackerSystemTypeData = AttackerSystem.TypeData;
            if ( attackerSystemTypeData == null )
                return 0; //we died, apparently

            if ( DefenderEntity.CurrentWeaponAddedReloadSeconds >= attackerSystemTypeData.MaxEnemyWeaponReloadSlowingSeconds )
                return 0; //he's already jammed as much as we can, so don't bother!
            int amountICanJamHim = AttackerSystem.GetWeaponReloadSlowingSecondsAgainst( DefenderEntity );
            if ( amountICanJamHim <= 0 )
                return -TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_LOW_MID_EFFECT; //he's immune to my wiles, so ignore him a bit!

            //the less jammed he is, the more I want to jam him -- by the huge multiplier, too!
            return ((1f -
                ((float)DefenderEntity.CurrentWeaponAddedReloadSeconds / (float)attackerSystemTypeData.MaxEnemyWeaponReloadSlowingSeconds))
                * TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_SUPER_HUGE_EFFECT) +
                //also spread these out every 10 units, to spread it around!
                (AttackerEntity.NonSimPermanentUniqueID % 10 == DefenderEntity.NonSimPermanentUniqueID % 10 ? TargetListPlanningRoot.FACTOR_IMPORTANCE_FOR_ADDED_HUGE_EFFECT : 0);
        }
    }
    #endregion

    #region TargetEvaluatorBase
    public abstract class TargetEvaluatorBase : ITargetEvaluatorImplementation
    {
        public abstract float CalculateNormalizedAdditionToCoreImportance( GameEntity_Squad AttackerEntity, EntitySystem AttackerSystem, GameEntity_Squad DefenderEntity, bool priorityAIBased );
    }
    #endregion
    
    #endregion
}
