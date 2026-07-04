using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

using Arcen.Universal.Sprites;
using Arcen.AIW2.External;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Arcen.AIW2.ExternalVisualization
{
    public class SquadVisualizer : ConcurrentPoolable<SquadVisualizer>, IInstancedRenderer, IArcenShapeOwner, IRelatedEntity
    {
        public Vector3 CurrentPosition = Mat.V3N_Zero;
        public UnityEngine.Quaternion CurrentRotation = Mat.Quater_Ident;
        private bool currentRotationIsSet = false;
        
        public SquadRenderManagerGroup Pool = null;

        private Vector3 targetLoc = Mat.V3N_Zero;
        private float targetRotY = -1;
        private float currentRotY = -1;
        private float movementSpeed = 0f;
        
        public readonly List<ShipVisualizer> ShipsLiving = List<ShipVisualizer>.Create_WillNeverBeGCed( 25, "SquadVisualizer-ShipsLiving" );
        public readonly List<ShipVisualizer> ShipsDying = List<ShipVisualizer>.Create_WillNeverBeGCed( 10, "SquadVisualizer-ShipsDying" );
        
        public UnityEngine.Transform GimbalT;
        public GimbalVisualizer Gimbal;

        private ArcenShape_ThicknessRingBase hoverRing = null;
        private ArcenShape_ThicknessRingBase factionRing = null;
        private readonly List<ArcenShape_ThicknessRingBase> rangeRings = List<ArcenShape_ThicknessRingBase>.Create_WillNeverBeGCed( 18, "SquadVisualizer-rangeRings" );
        private int rangeRingCurrentIndex = 0;

        public long RelatedEntityID = -1;
        public LazyLoadSquadWrapper RelatedEntity;
        public GameEntityTypeData RelatedTypeData;
        public FactionType RelatedFactionType;

        public RingStatus CurrentRingStatus = RingStatus.NoRing;
        public ActionStatus CurrentActionStatus = ActionStatus.Normal;
        public IconShipStatus CustomIconStatus = null;
        public ProtectionStatus CurrentProtectionStatus = ProtectionStatus.None;
        public int CurrentHealthTicks = -1;
        public int CurrentShieldTicks = -1;
        public int CurrentStackCount = -1;
        public int CurrentLOD = 0;

        private int lastFFRadius = -1;
        private UnityEngine.GameObject ffObject = null;
        private UnityEngine.Transform ffObjectT = null;
        private ArcenForcefieldRippler ffRippler = null;
        private UnityEngine.MeshRenderer[] ffRenderers = null;

        private WrapperedAddonObjectOrParticleField otherAddonObject = null;

        //Note that ToBeRemovedType is not currently in use
        public SquadRemovalType ToBeRemovedType = SquadRemovalType.None; 
        
        //When a squad is Active, it has a couple possible states to be in
        //Alive and happy
        //fatalDamageTaken ==> move all my ships to BurningAndDying. Set in ShotVisualizer
        //needsToExplode ==> all ships are on the BurningAndDying list, so I can explode now. Set in the Squad
        //                   Once needsToExplode is set, the SquadDyingProgress
        //                   starts to count up. When it gets to SquadDeathTime the squad will be flagged for removal
        //                   This extra time will allow some nice effects on the icon
        public bool needsToExplode;
        private float SquadDyingProgress = 0f;
        private float SquadDeathTime = 0.3f;
        public float TimeLeftUntilVisuallyAppears = -1;

        public InstancedRendererDeactivationReason DeactivatingBecause = InstancedRendererDeactivationReason.Unknown;

        public readonly int myID;
        static int id = 0;

        public SquadDisplayType OriginalDisplay = SquadDisplayType.Unknown;

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "SquadVisualizers" );
        public SquadVisualizer( SquadDisplayType Display )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            RefTracker.IncrementObjectCount();

            if ( OriginalDisplay == SquadDisplayType.Unknown )
                OriginalDisplay = Display;
            else if ( Display != OriginalDisplay )
                ArcenDebugging.ArcenDebugLog( "Oops!  Squad with display type of " + OriginalDisplay + " was attempted to be used as type " + Display, Verbosity.ShowAsError );
            
            myID = id++;

            {
                UnityEngine.GameObject obj = new UnityEngine.GameObject();
                this.GimbalT = obj.transform;
                this.GimbalT.parent = ArcenVisualOrganizer.Instance.MainGameObjectParent;
                this.GimbalT.SetDefaultTRS();
                this.GimbalT.rotation = UnityEngine.Quaternion.Euler( 180, 0, 180 );

                ArcenGimbal arcenGimbalComponent = obj.AddComponent<ArcenGimbal>();
                arcenGimbalComponent.GimbalID = ArcenGimbal.LastGlobalGimbalID++;

                this.Gimbal = new GimbalVisualizer();
                this.Gimbal.UnityObject = arcenGimbalComponent;
                this.Gimbal.InitializeGimbal();

                arcenGimbalComponent.Visualizer = this.Gimbal;
                arcenGimbalComponent.InstancedRenderer = this;

                obj.name = "Squad" + Display + myID + " Gimbal";
                this.Gimbal.SetSelfVisible( false );
            }
        }

        public bool IsConsideredActive = false;
        //private static long poolActivationCount = 0;
        //private static long badActivationCount = 0;
        public void Activate( GameEntity_Base RelatedToBase, GameEntityTypeData RelatedType )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( this.IsConsideredActive )
            {
                //badActivationCount++;
                return;
            }
            this.IsConsideredActive = true;

            this.lastFFRadius = -1;
            this.CurrentHealthTicks = -1;
            this.CurrentShieldTicks = -1;
            this.CurrentStackCount = -1;

            GameEntity_Squad relatedEnt = (GameEntity_Squad)RelatedToBase;
            this.RelatedEntity = LazyLoadSquadWrapper.Create(relatedEnt);
            this.RelatedTypeData = RelatedType;
            if ( relatedEnt != null )
                this.RelatedFactionType = relatedEnt.GetFactionTypeSafe();
            else
                this.RelatedFactionType = FactionType.NaturalObject;
            this.needsToExplode = false;
            this.currentRotationIsSet = false;
            this.currentRotY = 0;
            this.CurrentLOD = 0;
            this.DeactivatingBecause = InstancedRendererDeactivationReason.Unknown;
            this.wasNotFullyClaimed = relatedEnt.HasNotYetBeenFullyClaimed;
            this.TimeLeftUntilVisuallyAppears = 0.1f;
                        
            this.RelatedEntityID = relatedEnt.PrimaryKeyID;
            //relatedEnt.Visual_SquadShipsAdded_SinceLastDrawn = 0;
            relatedEnt.Visual_IsWarpingInToGravityWell = false;

            if ( BattlefieldVisualSingleton.Instance == null )
                ArcenDebugging.ArcenDebugLog( "Null BattlefieldVisualSingleton.Instance" , Verbosity.ShowAsError );

            BattlefieldVisualSingleton.Instance.ActiveSquads.Add( this );

            this.AddStartingShips();

            this.HandleForcefieldIfAny();
            this.ShowOrHideForcefieldIfNeeded( false ); //hide the forcefield for now!

            this.HandleOtherVisualAddonIfAny();

            if ( this.Gimbal.UnityObject == null )
                ArcenDebugging.ArcenDebugLog( "Null UnityObject on Gimbal for squad " + this.myID , Verbosity.ShowAsError );
            this.Gimbal.UpdateBurningAndDyingStatus( false, 0 );
            this.Gimbal.SetSelfVisible( false );
            
            this.SquadDyingProgress = 0f;

            if ( relatedEnt.WorldLocation != ArcenPoint.OutOfRange )
                this.CurrentPosition = relatedEnt.WorldLocation.ToVisualMainGameCoordinates_Numerics( relatedEnt.Planet );
            this.SyncGimbalAndFFPos();

            this.Gimbal.UpdateValuesIfDirty(this);
        }

        public bool fatalDamageTaken
        {
            get { return this.DeactivatingBecause != InstancedRendererDeactivationReason.Unknown && 
                    this.DeactivatingBecause < InstancedRendererDeactivationReason.AboveThisValueDoesNotShowExplosionAnimations; }
        }

        public bool DieWithoutExplosions
        {
            get
            {
                return this.DeactivatingBecause != InstancedRendererDeactivationReason.Unknown &&
                  this.DeactivatingBecause >= InstancedRendererDeactivationReason.AboveThisValueDoesNotShowExplosionAnimations;
            }
        }

        public void SetDeactivationReason( InstancedRendererDeactivationReason NewReason )
        {
            //if the existing reason is none, then just do it.
            if ( this.DeactivatingBecause == InstancedRendererDeactivationReason.Unknown )
                this.DeactivatingBecause = NewReason;
            else //if we already have a reason for deactivating, only override under certain circumstances
            {
                if ( NewReason >= InstancedRendererDeactivationReason.AboveThisValueDoesNotOverrideLowerValuesExceptUnknown )
                { } //these ones don't override ever
                else if ( NewReason >= InstancedRendererDeactivationReason.AboveThisValueDoesNotShowExplosionAnimations )
                {
                    //if the new reason is not explodey, only override other non-explodey ones
                    if ( this.DeactivatingBecause >= InstancedRendererDeactivationReason.AboveThisValueDoesNotShowExplosionAnimations )
                        this.DeactivatingBecause = NewReason;
                }
            }
        }

        public void DeactivateAndReturnToPool( InstancedRendererDeactivationReason Reason, bool ForceEvenIfNotConsideredActive )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( !this.IsConsideredActive && !ForceEvenIfNotConsideredActive )
                return;
            this.IsConsideredActive = false; //must happen right at the top, above ClearVisualObjIfExists, or bad things happen!

            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();

            if ( relatedEnt != null )
                relatedEnt.ClearVisualObjIfExists( true, false, InstancedRendererDeactivationReason.VisSideDeactivateAndReturnToPoolBeingCareful );

            this.timeStartedNoticingWasAlone = 0;
            this.RelatedEntity.Clear();
            this.RelatedEntityID = 0;
            this.RelatedTypeData = null;
            this.RelatedFactionType = FactionType.NaturalObject;
            this.ToBeRemovedType = SquadRemovalType.None;

            this.CurrentRingStatus = RingStatus.NoRing;
            this.CurrentActionStatus = ActionStatus.Normal;
            this.CustomIconStatus = null;
            this.CurrentProtectionStatus = ProtectionStatus.None;
            this.CurrentHealthTicks = -1;
            this.CurrentShieldTicks = -1;
            this.CurrentStackCount = -1;
            this.CurrentLOD = 0;
            this.lastFFRadius = -1;

            this.needsToExplode = false;
            this.SquadDyingProgress = 0f;
            this.SquadDeathTime = 0.3f;
 

            
            this.wasNotFullyClaimed = false;
			
			// jcf: is this not reset to its initial value for a good reason?
			//TimeLeftUntilVisuallyAppears = -1;
            this.TimeLeftUntilVisuallyAppears = 0.1f;
			
            this.SetDeactivationReason( Reason );

	        this.Gimbal.DoDeactivations();
	        this.Gimbal.UpdateBurningAndDyingStatus( false, 0 );
	        this.Gimbal.SetSelfVisible( false );
            
            BattlefieldVisualSingleton.Instance.ActiveSquads.Remove( this );

            ShipVisualizer vis;
            for ( int i = 0; i < this.ShipsLiving.Count; i++ )
            {
                try
                {
                    vis = this.ShipsLiving[i];
                    if ( vis != null )
                        vis.DeactivateAndReturnToPool( Reason, ForceEvenIfNotConsideredActive );
                }
                catch { }//cross threading errors
            }
            this.ShipsLiving.Clear();
            for(int i = 0; i < this.ShipsDying.Count; i++)
            {
                try
                {
                    vis = this.ShipsDying[i];
                    if ( vis != null )
                        vis.DeactivateAndReturnToPool( Reason, ForceEvenIfNotConsideredActive );
                }
                catch { }//cross threading errors
            }
            this.ShipsDying.Clear();

            this.SquadDyingProgress = 0f;
            this.currentRotationIsSet = false;
            this.currentRotY = 0;
            this.CurrentLOD = 0;

            this.ShowOrHideForcefieldIfNeeded( false );

            if ( Pool == null ) {
                if (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.VisMemLeak )) {
                    ArcenDebugging.ArcenDebugLog( "Memory leak!  Pool was null for squad " + this.myID, Verbosity.ShowAsError );
                }
            } else {
                this.Pool.PutBackInPoolRightAway( this );
            }
            this.Pool = null;

            if ( hoverRing != null )
            {
                hoverRing.DeactivateAndReturnToPool( false );
                hoverRing = null;
            }
            if ( factionRing != null )
            {
                factionRing.DeactivateAndReturnToPool( false );
                factionRing = null;
            }

            for ( int i = 0; i < rangeRings.Count; i++ )
            {
                ArcenShape_ThicknessRingBase ring = rangeRings[i];
                if ( ring != null )
                    ring.DeactivateAndReturnToPool( false );
            }
            rangeRings.Clear();

            if ( this.otherAddonObject != null )
            {
                this.otherAddonObject.ReturnToPool();
                this.otherAddonObject = null;
            }

            //if ( deactivationReasonCount.ContainsKey( Reason ) )
            //    deactivationReasonCount[Reason]++;
            //else
            //    deactivationReasonCount[Reason] = 1;

            //poolDeactivationCount++;
            //if ( ArcenTime.TimeSinceStartF - lastTimeOfDeactivationNotice > 1 )
            //{
            //    lastTimeOfDeactivationNotice = ArcenTime.TimeSinceStartF;
            //    System.Text.StringBuilder builder = new System.Text.StringBuilder();
            //    foreach ( KeyValuePair<InstancedRendererDeactivationReason,int> kv in deactivationReasonCount )
            //        builder.Append( kv.Key.ToString() ).Append( ":" ).Append( kv.Value ).Append( "\n" );

            //    UnityEngine.Debug.Log( "badActivationCount: " + badActivationCount + ", poolActivationCount: " + poolActivationCount + 
            //        ", poolDeactivationCount: " + poolDeactivationCount + ", putBackInPoolActiveCount: " + putBackInPoolActiveCount +
            //        ", takeOutOfPoolActiveCount: " + takeOutOfPoolActiveCount + ", ids granted: " + id + "\n" + builder );
            //}
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.IAmHeadedBackToPool, true );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public string GetTempDebugInfo()
        {
            string retVal = string.Empty;
            retVal += "IsConsideredActive: " + IsConsideredActive + "\n";
            retVal += "ToBeRemovedType: " + ToBeRemovedType + "\n";
            retVal += "RelatedEntity.GetQualifiedName(): " + ( RelatedEntity.GetSquad() == null ? "null" : RelatedEntity.GetSquad().GetQualifiedName() ) + "\n";
            return retVal;
        }

        public object GetTempDebugObject()
        {
            return this.Gimbal.UnityObject;
        }

        //public static long putBackInPoolActiveCount = 0;
        //public static long takeOutOfPoolActiveCount = 0;
        //private static long poolDeactivationCount = 0;
        //private static float lastTimeOfDeactivationNotice = 0;

        public void DetachVisObjectFromSim( InstancedRendererDeactivationReason Reason )
        {
            //UnityEngine.Debug.Log( ( this.RelatedTypeData == null ? "Null squad" : this.RelatedTypeData.InternalName ) + " IsConsideredActive: " + this.IsConsideredActive + " Reason:" + Reason );

            //The Sim will call this when it's going to destroy the underlying GameEntity_Squad
            //for Squad. Instead of just removing the squad instantly, instead tell the Vis layer to remove
            //the squad properly. This helps ensure we get good visual effects of a squad dying.
            //Note that the removal can be either "Blow up" or "Do something else"

            GameEntity_Squad related = this.RelatedEntity.GetSquad();
            /*
              We'd love to disable the rings directly here, but that can only be done on the Main Thread, and this code is called from a variety of contexts.
              Instead we should add these units to a ConcurrentQueue<SquadVisualizer> SquadsRequiringMainThreadCleanup (presumably in Battlefieldvisualsingleton), then
              clean those up later, at most 100 at a time to avoid lag spikes
            this.HideFactionRing();
            this.HideHoverRing();
            this.HideExtraRangeRings();
            */
            try
            {
                if ( related != null )
                {
                    if ( related.despawnVis == DespawnVisualization.Transformation )
                    {
                        //                 ArcenDebugging.ArcenDebugLogSingleLine("Squad " + related.PrimaryKeyID + " is despawning: upgrade path", Verbosity.DoNotShow );
                        for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                        {
                            ShipVisualizer shipVis = this.ShipsLiving[i];
                            if ( shipVis != null )
                                shipVis.stillAntiSpawning = true;
                        }
                    }
                }
            }
            catch { } //it's ok

            this.SetDeactivationReason( Reason );

            if ( Reason == InstancedRendererDeactivationReason.LastCallFromClearForReturningToPoolJustInCase ) {
                this.RelatedEntity.Clear();
            }

            //if ( related.DataForMark.CanAssist_Repair )
            //    ArcenDebugging.ArcenDebugLogSingleLine( "DetachVisObjectFromSim: " + related.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " + Reason +
            //        " " + IsConsideredActive + " " + ToBeRemovedType + " DieWithoutExplosions" + DieWithoutExplosions + " fatalDamageTaken" + fatalDamageTaken, Verbosity.DoNotShow );

        }

        private InstancedRendererDeactivationReason flagForRemovalReason;
        public InstancedRendererDeactivationReason GetLastFlagForRemovalReason()
        {
            return this.flagForRemovalReason;
        }

        public void FlagForRemoval( bool ForceRemoval, InstancedRendererDeactivationReason Reason )
        {
            //if ( relatedEnt.DataForMark.CanAssist_Repair )
            //    ArcenDebugging.ArcenDebugLogSingleLine( "FlagForRemoval: " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " + Reason + " " + 
            //        IsConsideredActive + " " + ToBeRemovedType + " DieWithoutExplosions" + DieWithoutExplosions + " fatalDamageTaken" + fatalDamageTaken, Verbosity.DoNotShow );

            if ( this.ToBeRemovedType != SquadRemovalType.None )
                return;
            //UnityEngine.Debug.Log( ( this.RelatedTypeData == null ? "Null ship" : this.RelatedTypeData.InternalName ) + " IsConsideredActive: " + this.IsConsideredActive + " Reason:" + Reason );
            switch ( Reason )
            {
                case InstancedRendererDeactivationReason.VisLayerSquadFinishedDying:
                    GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
                    if ( relatedEnt != null && relatedEnt.GetCurrentHullPoints() > 0 )
                    {
                        //Chris notes: this is when an entity died to remains or a neutral faction!
                        return;
                    }
                    break;
            }

            this.flagForRemovalReason = Reason;
            World_AIW2.Instance.CountOfIInstancedRendererRemovalRequests++;
            //UnityEngine.Debug.Log( ( this.RelatedTypeData == null ? "Null ship" : this.RelatedTypeData.InternalName ) + " IsConsideredActive: " + this.IsConsideredActive + " Reason:" + Reason );
            BattlefieldVisualSingleton.Instance.IInstancedRendererRemovalRequests.Enqueue( this );
        }

        //public Vector3 GetPosition()
        //{
        //    return this.CurrentPosition;
        //}

        public GameEntity_Base GetEntityRelatedTo()
        {
            return this.RelatedEntity.GetSquad();
        }

        public bool GetIsToUseSharedPool()
        {
            return false;
        }

        public void DoPrototypeOnlyPreprocessing( GameEntityTypeData RelatedTypeData )
        {
            //unused now
        }

        #region UpdateSquad
        private float timeStartedNoticingWasAlone = 0;
        [NonSerialized]
        public bool BaseSquadUpdateChecksPassed = false;
        private bool isPlayerFaction = false;
        private bool wasNotFullyClaimed = false;
        //returns true when the squad is flagged for removal
        public bool CheckSquadForUpdates( int CurrentSimFrameVisualOnly, float DeltaTime )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( this.timeStartedNoticingWasAlone > 0 )
            {
                if ( ArcenTime.TimeSinceStartF - timeStartedNoticingWasAlone > 0.8f )
                {
                    this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.MySimDataDiedAndIWasFoundAloneInTheVisLayer, true );
                    ArcenDebugging.ArcenDebugLogSingleLine( "Squad sat around for 0.8 seconds after death, so was cleaned up.", Verbosity.ShowAsError ); //just for debugging
                }
                return false;
            }

            bool localDebug = false;
            this.BaseSquadUpdateChecksPassed = false;
            if ( this.ToBeRemovedType != SquadRemovalType.None )
                return false;
            if ( relatedEnt == null  || !relatedEnt.IsConsideredActive )
            {
                if ( this.fatalDamageTaken && !this.DieWithoutExplosions ) //delay for the icon to burn
                {
                    this.SquadDyingProgress += DeltaTime;
                    //UnityEngine.Debug.Log( "SquadDyingProgress: " + this.SquadDyingProgress + " " );
                    if ( !ArcenMainGameVisuals.AreSpritesDrawnAtAllRightNow )
                        this.Gimbal.SetSelfVisible( false );
                    else
                        this.Gimbal.UpdateBurningAndDyingStatus( true, this.SquadDyingProgress / this.SquadDeathTime );
                    
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Squad " + this.myID + " " + this.RelatedTypeData.InternalName + " dying progress " + this.SquadDyingProgress, Verbosity.DoNotShow );
                    if ( this.SquadDyingProgress >= this.SquadDeathTime )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Squad " + this.myID + " " + this.RelatedTypeData.InternalName + " dying progress " + this.SquadDyingProgress + " ready to die", Verbosity.DoNotShow );
                        this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerSquadFinishedDying );
                        return true;
                    }
                    return false;
                }
                this.FlagForRemoval( true, InstancedRendererDeactivationReason.VisEntityMissing );
                this.timeStartedNoticingWasAlone = ArcenTime.TimeSinceStartF;
                return false;
            }

            if ( relatedEnt == null || relatedEnt.HasBeenRemovedFromSim )
            {
                this.timeStartedNoticingWasAlone = ArcenTime.TimeSinceStartF;
                return false;
            }

            //Chris notes: this is just in case, with the phantom ships and lines.  This gets the things that have double icons for whatever reason.
            if ( relatedEnt != null && relatedEnt.InstancedRenderer != this )
            {
                //if ( relatedEnt.DataForMark.CanAssist_Repair )
                //ArcenDebugging.ArcenDebugLogSingleLine( "VisLayerRelatedEntityInstancedRendererMismatch: " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " +
                //    " " + IsConsideredActive + " " + ToBeRemovedType + " DieWithoutExplosions" + DieWithoutExplosions + " fatalDamageTaken" + fatalDamageTaken, Verbosity.DoNotShow );
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerRelatedEntityInstancedRendererMismatch );
                return true;
            }


            this.Gimbal.SetSelfVisible( !Engine_AIW2.Instance.InHideGimbalMode && !this.RelatedTypeData.SkipsDrawingGimbal );//&& this.TimeLeftUntilVisuallyAppears <= 0 );

            //originally we immediately removed an object once it had been removed from the sim. Practically this meant that the on-death effects
            //for a squad rarely played, since the Sim seems to outpace the vis code (I suspect it's on a different thread). So don't allow a Squad
            //to be deactivated if it has been hit but has not exploded yet.
            //Note this fix requires and additional check to remove a Squad after objectTimeoutInFrames frames,
            //in case there is a bug and we never get around to removing it. At the moment it's logging something in this case to let me tune the timeout appropriately.
            //If something forces the objects removal then the ClearPlanet code will take care of things.
            if (this.needsToExplode && !this.fatalDamageTaken)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("BUG: a squad is ordered to explode but has not taken lethal damage", Verbosity.DoNotShow );
            }

            if(this.DieWithoutExplosions == true)
            {
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerNotInPlanetViewAtAll );
                return true;
            }
            if (this.fatalDamageTaken && this.needsToExplode) //delay for the icon to burn
            {
                this.SquadDyingProgress += DeltaTime;
                //UnityEngine.Debug.Log( "SquadDyingProgress: " + this.SquadDyingProgress + " " );
                if ( !ArcenMainGameVisuals.AreSpritesDrawnAtAllRightNow )
                    this.Gimbal.SetSelfVisible( false );
                else
                    this.Gimbal.UpdateBurningAndDyingStatus( true, this.SquadDyingProgress / this.SquadDeathTime );
                
                if (localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Squad " + this.myID + " " + this.RelatedTypeData.InternalName + " dying progress " + this.SquadDyingProgress, Verbosity.DoNotShow );
                if (this.SquadDyingProgress >= this.SquadDeathTime)
                {
                    if(localDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("Squad " + this.myID + " " + this.RelatedTypeData.InternalName + " dying progress " + this.SquadDyingProgress + " ready to die", Verbosity.DoNotShow );
                    this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerSquadFinishedDying );
                    return true;
                }
                return false;
            }
            else if (this.fatalDamageTaken)
            {
                //Once fatal damage is taken, all the ShipsLiving are flagged to die in HandleLODsAndShipPartAnimationsForSquad.
                //Once this is done then needsToExplode is set and we start the countdown for a burning icon.
                return false;
            }
            if ( relatedEnt == null || this.RelatedTypeData == null )
            {
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerNullRelatedStuff );
                return true;
            }

            int lastFrame = relatedEnt.LastSimFrameToldToHaveVisualObject;
            if ( lastFrame > CurrentSimFrameVisualOnly || CurrentSimFrameVisualOnly > lastFrame + 3 )
            {
                if (localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Squad " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " removal, lastFrame path. lastFrame " + lastFrame + " CurrentSimFrameVisualOnly " + CurrentSimFrameVisualOnly + " ships living " + this.ShipsLiving.Count + " ships dying " + this.ShipsDying.Count + " fatalDamageTaken: " + this.fatalDamageTaken , Verbosity.DoNotShow );
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerLastSimFrameToldToHaveVisualObjectTooFarBack );
                return true;
            }
            
            this.BaseSquadUpdateChecksPassed = true;

            GameEntity_Squad squad = relatedEnt;
            if ( squad != null )
                this.isPlayerFaction = ( squad.GetFactionTypeSafe() == FactionType.Player );
            
            //this will let us refresh the graphics of the ship in case they are different after being claimed
            if ( this.wasNotFullyClaimed != relatedEnt.HasNotYetBeenFullyClaimed )
            {
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerNotFullyClaimedMismatch );
                return true;
            }

            //Chris notes: this seems to fix the bug that we were seeing with phantom ships.  In testing this solves the problem almost all the time.
            if ( this.ShipsLiving.Count == 0 && this.ShipsDying.Count == 0 )
            {
                //if ( relatedEnt.DataForMark.CanAssist_Repair )
                    //ArcenDebugging.ArcenDebugLogSingleLine( "VisLayerNoLivingOrDyingShipsLeftInSquad: " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " +
                    //    " " + IsConsideredActive + " " + ToBeRemovedType + " DieWithoutExplosions" + DieWithoutExplosions + " fatalDamageTaken" + fatalDamageTaken, Verbosity.DoNotShow );
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerNoLivingOrDyingShipsLeftInSquad );
                return true;
            }

            //Chris notes: this is just in case, again with the phantom ships and lines.  In testing this catches the remainder
            if ( relatedEnt != null && ( !relatedEnt.IsConsideredActive || relatedEnt.HasBeenRemovedFromSim ) )
            {
                //if ( relatedEnt.DataForMark.CanAssist_Repair )
                //ArcenDebugging.ArcenDebugLogSingleLine( "VisLayerRelatedEntityNotIsConsideredActive: " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " +
                //    " " + IsConsideredActive + " " + ToBeRemovedType + " DieWithoutExplosions" + DieWithoutExplosions + " fatalDamageTaken" + fatalDamageTaken, Verbosity.DoNotShow );
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerRelatedEntityNotIsConsideredActive );
                return true;
            }

            return false;
        }

        public void UpdateSquadForcefieldsAndSuch( float deltaTime )
        {
            int debugstage = 0;
            try
            {
                debugstage = 1;
                if ( !this.BaseSquadUpdateChecksPassed )
                    return;

                debugstage = 2;
                GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
                if ( relatedEnt != null && 
                     relatedEnt.CalculatedCurrentShieldRadius > 0 )//&& this.TimeLeftUntilVisuallyAppears <= 0 )
                {
                    debugstage = 3;
                    this.HandleForcefieldIfAny();
                    debugstage = 4;
                    this.ShowOrHideForcefieldIfNeeded( true );
                    debugstage = 5;
                }
                else
                {
                    debugstage = 6;
                    this.ShowOrHideForcefieldIfNeeded( false );
                    debugstage = 7;
                }
                
                debugstage = 8;
                this.HandleOtherVisualAddonIfAny();
                debugstage = 9;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }

        public void DrawSquadRanges()
        {
            // local method so we can 'return' instead of nesting if statements
            void HandleSquadRanges()
            {
                /*
                int debugStage = 0;
                try
                */
                {
                    GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
                    if ( relatedEnt == null )
                        return;

                    if ( !this.BaseSquadUpdateChecksPassed )
                        return;

                    if ( ArcenUI.Instance.InHideGUIMode )
                        return;

                    if (!relatedEnt.GetShouldDisplayRangeRings())
                        return;

                    /*
                    if (BattlefieldVisualSingleton.Instance.NumSelected > 5 && relatedEnt != GameEntity_Base.CurrentlyHoveredOver)
                    {
                        return;
                    }
                    */

                    if (ArcenInput_AIW2.ShouldShowShipRanges_All ||
                        ArcenInput_AIW2.ShouldShowShipRanges_Selected && relatedEnt.GetIsSelected() ||
                        ArcenInput_AIW2.ShouldShowShipRanges_Hovered && relatedEnt == GameEntity_Base.CurrentlyHoveredOver)
                    {
                        for ( int i = 0; i < relatedEnt.Systems.Count; i++ )
                        {
                            var system = relatedEnt.Systems[i];
                            if ( system.TypeData.Category == EntitySystemCategory.Weapon )
                            {
                                DrawRangeCircleForSystem( this.CurrentPosition.ToUnityVector3(), system, this.rangeRings, ref this.rangeRingCurrentIndex, this, relatedEnt.CalculatedAddedRange );
                                
                                if (system.DataForMark.BaseSeekRange > system.DataForMark.BaseRange)
                                {
                                    float radius = system.DataForMark.CalculateActualSeekRange(relatedEnt) / relatedEnt.Planet.GravWellSize.CombatVisualScaleDivisor;
                                    float thickness = 0.8f / relatedEnt.Planet.GravWellSize.GeneralMultiplier;
                                    Color color = Color.yellow;
                                    DrawRangeCircle(this.CurrentPosition.ToUnityVector3(), radius, thickness, color, this.rangeRings, ref this.rangeRingCurrentIndex, this);
                                }
                            }
                            else
                            {
                                DrawRangeCircleForSystem( this.CurrentPosition.ToUnityVector3(), system, this.rangeRings, ref this.rangeRingCurrentIndex, this, 0 );
                            }
                        }

                        DrawRangeCircleForForcefield( this.CurrentPosition.ToUnityVector3(), relatedEnt.TypeData, relatedEnt.DataForMark,
                                relatedEnt, relatedEnt.Planet, this.rangeRings, ref this.rangeRingCurrentIndex, this );
                        
                        for ( int i = 0; i < relatedEnt.TypeData.IncomingDamageModifiers_AffectsRange.Count; i++ )
                        {
                            var mod = relatedEnt.TypeData.IncomingDamageModifiers_AffectsRange[i];
                            float radius = mod.ComparedToInt / relatedEnt.Planet.GravWellSize.CombatVisualScaleDivisor;
                            float thickness = 0.8f / relatedEnt.Planet.GravWellSize.GeneralMultiplier;
                            Color color = Color.magenta;
                            DrawRangeCircle(this.CurrentPosition.ToUnityVector3(), radius, thickness, color, this.rangeRings, ref this.rangeRingCurrentIndex, this);
                        }
                    }
                    else 
                    if ( Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() == false && 
                         (relatedEnt.GetHasBubbleForcefieldRightNow() || relatedEnt.TypeData.VisualOnlyForcefieldRangeCircleRadius > 0) )
                    {
                        DrawRangeCircleForForcefield( this.CurrentPosition.ToUnityVector3(), relatedEnt.TypeData, relatedEnt.DataForMark,
                                relatedEnt, relatedEnt.Planet, this.rangeRings, ref this.rangeRingCurrentIndex, this );
                    }
                }
                /*
                catch (Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine(string.Format("debugStage {0}\n{1}", debugStage, e), Verbosity.ShowAsError);
                }
                */
            }
                    
            this.rangeRingCurrentIndex = 0;
            HandleSquadRanges();

            this.HideExtraRangeRings();
        }

        public static UnityEngine.Color color_TractorRange = ColorMath.HexToColor( "2addfd" ); //cyan
        public static UnityEngine.Color color_TachyonRange = ColorMath.HexToColor( "fdddfd" ); //light purple
        public static UnityEngine.Color color_AttackRange = ColorMath.HexToColor( "fd2a2a" ); //red
        public static UnityEngine.Color color_GravityRange = ColorMath.HexToColor( "265bd4" ); //darker blue
        public static UnityEngine.Color color_ForcefieldRadius = ColorMath.HexToColor( "2a92fd" ); //blue

        public static void DrawRangeCircleForSystem( UnityEngine.Vector3 pos, EntitySystem system,
                                                     List<ArcenShape_ThicknessRingBase> rangeRings, ref int rangeRingCurrentIndex, IArcenShapeOwner ShapeOwner, int addedRange )
        {
            if ( system == null )
                return;
            var entity = system.ParentEntity;
            var planetFaction = entity.PlanetFaction;
            var systemType = system.TypeData;
            var statsForMark = system.DataForMark;
            var isDisabled = system.ComputeDisabledReason(CheckEntityStatus:false, CheckReload:false) != ArcenRejectionReason.Unknown;

            DrawRangeCircleForSystem( pos, systemType, statsForMark, planetFaction, isDisabled, rangeRings, ref rangeRingCurrentIndex, ShapeOwner, addedRange );
        }

        public static void DrawRangeCircleForSystem( UnityEngine.Vector3 placementPoint, 
                                                     EntitySystemTypeData systemData, 
                                                     EntitySystemTypeData.MarkLevelStats statsForMark, 
                                                     PlanetFaction planetFaction, 
                                                     bool isDisabled, 
                                                     List<ArcenShape_ThicknessRingBase> rangeRings, ref int rangeRingCurrentIndex, IArcenShapeOwner ShapeOwner, int addedRange )
        {
            // excessive number of null checks, because what else can i do
            if (planetFaction == null)
                return;
            Planet planet = planetFaction.Planet;
            if (planet == null)
                return;
            Faction faction = planetFaction.Faction;
            if (faction == null)
                return;
            if (systemData == null)
                return;
            if (statsForMark == null)
                return;
            if ( systemData.IsModule )
            {
                return;
            }

            var color = ColorMath.White;
            int radius = 0;

            if ( systemData.Category == EntitySystemCategory.Weapon )
            {
                radius = statsForMark.CalculateActualRange( faction, planet, addedRange );
                color = color_AttackRange;
            }
            else if ( systemData.BaseTractorCount > 0 && statsForMark.TractorRange > 0 )
            {
                radius = statsForMark.TractorRange;
                color = color_TractorRange;
            }
            else if ( statsForMark.TachyonRange > 0 )
            {
                radius = statsForMark.TachyonRange;
                color = color_TachyonRange;
            }
            else if ( statsForMark.GravityRange > 0 )
            {
                radius = statsForMark.GravityRange;
                color = color_GravityRange;
            }

            if ( radius <= 0 )
                return;

            PlanetGravWellSize gravSize = planet.GravWellSize;
            if ( gravSize == null )
                return;

            if ( radius > gravSize.DistanceScale_GravwellRadius * 2) 
                return;

            if ( isDisabled )
            {
                var hsv = color.ToHSV();
                hsv.s = 0.2f;
                hsv.v = 0.2f;
                color = hsv.ToRGB();
            }

            float rangeVis = radius / gravSize.CombatVisualScaleDivisor;

            ArcenShape_ThicknessRingBase ringDrawer;
            if ( rangeRings.Count <= rangeRingCurrentIndex )
            {
                ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                if (ringDrawer != null)
                    rangeRings.Add( ringDrawer );
            }
            else
            {
                ringDrawer = rangeRings[rangeRingCurrentIndex];
                //this case pretty much just happens if the pool was not properly used and this got returned and we  need to pull something fresh
                if ( ringDrawer == null || !ringDrawer.GetIsStillValidForUseAtOwner( ShapeOwner ) )
                {
                    ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                    if (ringDrawer != null)
                        rangeRings[rangeRingCurrentIndex] = ringDrawer;
                }
            }

            if ( ringDrawer != null )
            {
                ringDrawer.SetValues( placementPoint, color, rangeVis, 0.8f / gravSize.GeneralMultiplier );
                ringDrawer.SetEnabled(true);
            }

            //in case we have more than one system.  We'll find out!
            rangeRingCurrentIndex++;
        }

        public static void DrawRangeCircleForForcefield( UnityEngine.Vector3 placementPoint,
            GameEntityTypeData typeData, GameEntityTypeData.MarkLevelStats statsForMark, GameEntity_Squad entityOrNull, Planet planetOrNull, 
            List<ArcenShape_ThicknessRingBase> rangeRings, ref int rangeRingCurrentIndex, IArcenShapeOwner ShapeOwner )
        {
            int rangeSim;
            if ( entityOrNull != null ) {
                rangeSim = entityOrNull.GetEffectiveMaxForcefieldRadius();
            } else {
                rangeSim = statsForMark.ShieldRadius;
            }
            if ( rangeSim <= 0 ) {
                rangeSim = typeData.VisualOnlyForcefieldRangeCircleRadius;
                if ( rangeSim <= 0 ) {
                    return;
                }
            }

            UnityEngine.Color colorToUse = color_ForcefieldRadius;
            
            Planet planet = planetOrNull ?? entityOrNull?.Planet;
            if ( planet == null ) {
                return;
            }

            float rangeVis = rangeSim / planet.GravWellSize.CombatVisualScaleDivisor;

            ArcenShape_ThicknessRingBase ringDrawer;
            if ( rangeRings.Count <= rangeRingCurrentIndex )
            {
                ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                rangeRings.Add( ringDrawer );
            }
            else
            {
                ringDrawer = rangeRings[rangeRingCurrentIndex];
                //this case pretty much just happens if the pool was not properly used and this got returned and we  need to pull something fresh
                if ( ringDrawer == null || !ringDrawer.GetIsStillValidForUseAtOwner( ShapeOwner ) )
                {
                    ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                    rangeRings[rangeRingCurrentIndex] = ringDrawer;
                }
            }

            if ( ringDrawer != null )
            {
                ringDrawer.SetValues( placementPoint, colorToUse, rangeVis, 0.8f / planet.GravWellSize.GeneralMultiplier );
                ringDrawer.SetEnabled(true);
            }

            //in case we have more than one system.  We'll find out!
            rangeRingCurrentIndex++;
        }
        
        
        public static void DrawRangeCircle( 
            UnityEngine.Vector3 pos, 
            float radius,
            float thickness,
            Color color,
            List<ArcenShape_ThicknessRingBase> rangeRings, 
            ref int rangeRingCurrentIndex, 
            IArcenShapeOwner ShapeOwner )
        {
            ArcenShape_ThicknessRingBase ringDrawer;
            if ( rangeRings.Count <= rangeRingCurrentIndex )
            {
                ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                rangeRings.Add( ringDrawer );
            }
            else
            {
                ringDrawer = rangeRings[rangeRingCurrentIndex];
                
                if ( ringDrawer == null || 
                     !ringDrawer.GetIsStillValidForUseAtOwner( ShapeOwner ) )
                {
                    ringDrawer = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( ShapeOwner );
                    rangeRings[rangeRingCurrentIndex] = ringDrawer;
                }
            }

            if ( ringDrawer != null )
            {
                ringDrawer.SetValues( pos, color, radius, thickness );
                ringDrawer.SetEnabled(true);
            }
            
            rangeRingCurrentIndex++;
        }

        private Vector3 newLoc = Mat.V3N_Zero;
        private bool setPos = false;
        private bool setRot = false;
        public void UpdateSquadMovementAndRotation( float deltaTime )
        {
            if ( !this.BaseSquadUpdateChecksPassed )
                return;
            setPos = false;
            setRot = false;

            this.SetLocationToMatchSim();

            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null || relatedEnt.HasBeenRemovedFromSim || relatedEnt.ToBeRemovedAtEndOfThisFrame )
                return;
            Planet planet = relatedEnt.Planet;

            ////Engine_Universal.BeginProfilerSample( "CalcPos" );
            if ( this.movementSpeed > 0 )
            {
                if ( this.TimeLeftUntilVisuallyAppears > 0 )
                {
                    newLoc = this.targetLoc;
                    this.movementSpeed = -1;
                    //UnityEngine.Debug.Log( relatedEnt.PrimaryKeyID + " MOVE: mine:" + this.CurrentPosition + 
                    //    " underlying: " + relatedEnt.WorldLocation.ToVisualMainGameCoordinates_Numerics() + "  newLoc: " + newLoc );
                }
                else
                {
                    float totalSpeed = this.movementSpeed * deltaTime;
                    if ( this.lastShipSpeed > 0 )
                    {
                        if ( this.lastShipSpeed < 600 )
                            totalSpeed *= 1.5f;
                        if ( this.lastShipSpeed < 1000 )
                            totalSpeed *= 1.3f;
                        else if ( this.lastShipSpeed < 1500 )
                            totalSpeed *= 1.2f;
                    }
                    newLoc = Vector3.Lerp( this.CurrentPosition, this.targetLoc, totalSpeed );
                    if ( Mat.Abs( newLoc.X - this.CurrentPosition.X ) < 0.01 && Mat.Abs( newLoc.Z - this.CurrentPosition.Z ) < 0.01 )
                        this.movementSpeed = -1;
                }

                setPos = true;
                this.CurrentPosition = newLoc;
            }
            ////Engine_Universal.EndProfilerSample( "CalcPos" );

            ////Engine_Universal.BeginProfilerSample( "CalcRot" );
            //relatedEnt.DebugText = this.targetRotY + " vs " + relatedEnt.CurrentAngle.Tofloat() + " and " + this.currentRotY;
            if ( this.targetRotY >= 0 )
            {
                float newRotY = UnityEngine.Mathf.LerpAngle( this.currentRotY, this.targetRotY, RelatedTypeData.RotationalSpeedToReachTargetAngle * deltaTime );
                if ( Mat.Abs( newRotY - this.targetRotY ) < 0.01 )
                {
                    newRotY = this.targetRotY;
                    this.targetRotY = -1;
                }

                if ( this.currentRotY != newRotY )
                {
                    this.currentRotY = newRotY;
                    setRot = true;
                }
            }
            ////Engine_Universal.EndProfilerSample( "CalcRot" );

            this.TimeLeftUntilVisuallyAppears -= deltaTime;

            ////Engine_Universal.BeginProfilerSample( "SetPosOrRot" );
            if ( setRot )
            {
                this.CurrentRotation = UnityEngine.Quaternion.Euler( this.RelatedTypeData.RotationXOfShip, this.currentRotY, this.RelatedTypeData.RotationZOfShip );
                this.currentRotationIsSet = true;
            }
            if ( setPos )
                this.CurrentPosition = newLoc;
            ////Engine_Universal.EndProfilerSample( "SetPosOrRot" );

            //make sure we get the x and z in there if it has not been set ever before
            if ( !this.currentRotationIsSet )
            {
                this.currentRotY = relatedEnt.CurrentAngle.Tofloat();
                this.CurrentRotation = UnityEngine.Quaternion.Euler( this.RelatedTypeData.RotationXOfShip, this.currentRotY, this.RelatedTypeData.RotationZOfShip );
                this.currentRotationIsSet = true;
            }

            //only position matters, not rotation!
            if ( setPos )
            {
                ////Engine_Universal.BeginProfilerSample( "SyncGimbalPos" );
                this.SyncGimbalAndFFPos();
                ////Engine_Universal.EndProfilerSample( "SyncGimbalPos" );
            }
        }

        private float lastDistanceFromCamera = 0;
        public void AlterLODIfNeeded( Vector3 CameraPos )
        {
            GameEntityTypeData relatedTypeData = this.RelatedTypeData;
            if ( relatedTypeData == null )
                return;

            lastDistanceFromCamera = ( CameraPos - this.CurrentPosition ).Length();
            float lodDivisor = 1f;
            GameEntity_Squad relatedEntity = this.RelatedEntity.GetSquad();
            Planet planet = relatedEntity == null ? null : relatedEntity.Planet;
            if ( planet != null )
                lodDivisor = planet.GravWellSize.GeneralMultiplier;

            this.CurrentLOD = 0;
            float[] lodDistances = relatedTypeData.LODDistancesFromVis;
            if ( lodDistances == null )
                return;
            List<float> lodDistanceOverrides = relatedTypeData.LODDistanceOverrides;
            for ( int i = 0; i < lodDistances.Length; i++ )
            {
                if ( lodDistanceOverrides != null && i < lodDistanceOverrides.Count ) //use the override if present
                {
                    if ( lastDistanceFromCamera <= ( lodDistanceOverrides[i] * relatedTypeData.LODDistanceMultiplier ) / lodDivisor )
                        break;
                }
                else
                {
                    if ( lastDistanceFromCamera <= ( lodDistances[i] * relatedTypeData.LODDistanceMultiplier ) / lodDivisor )
                        break;
                }
                this.CurrentLOD++;
            }
        }
        public int GetCurrentLODAndLastDistanceFromCamera( out float LastDistanceFromCamera )
        {
            LastDistanceFromCamera = lastDistanceFromCamera;
            return this.CurrentLOD;
        }
        public void UpdateSquadStatusFromEntity()
        {
            if ( !this.BaseSquadUpdateChecksPassed )
                return;

            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null || !relatedEnt.IsConsideredActive )
                return;

            this.Gimbal.UpdateValuesIfDirty(this);  
        }

        #region HideExtraRangeRings
        private void HideExtraRangeRings()
        {
            try
            {
                if ( this.rangeRingCurrentIndex >= this.rangeRings.Count )
                    return; //nothing to hide, no extras
                for ( int i = this.rangeRingCurrentIndex; i < this.rangeRings.Count; i++ )
                {
                    ArcenShape_ThicknessRingBase ring = this.rangeRings[i];
                    if ( ring != null )
                        ring.SetEnabled( false );
                }
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Exception happened in SquadVisualizer.HideExtraRangeRings:\n{0}", e), Verbosity.ShowAsError);
            }
        }
        #endregion

        #region HoverAndFactionRings
        private void HideHoverRing()
        {
            try
            {
                if ( hoverRing != null )
                {
                    var isEnabled = hoverRing.enabled;
                    if (isEnabled)
                    {
                        hoverRing.SetEnabled( false );
                        CurrentRingStatus = RingStatus.NoRing;
                        if ( this.Gimbal != null )
                            Gimbal.IsDirty = true;
                    }
                }
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Exception happened in SquadVisualizer.HideHoverRing:\n{0}", e), Verbosity.ShowAsError);
            }
        }

        private ArcenShape_ThicknessRingBase GetHoverRing()
        {
            if ( hoverRing == null || !hoverRing.GetIsStillValidForUseAtOwner( this ) )
                hoverRing = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( this );
            return hoverRing;
        }

        private void HideFactionRing()
        {
            try
            {
                if ( factionRing != null )
                    factionRing.SetEnabled( false );
            }
            catch ( Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Exception happened in SquadVisualizer.HideFactionRing:\n{0}", e), Verbosity.ShowAsError);
            }
        }

        private ArcenShape_ThicknessRingBase GetFactionRing()
        {
            if ( factionRing == null || !factionRing.GetIsStillValidForUseAtOwner( this ) )
                factionRing = (ArcenShape_ThicknessRingBase)ArcenVisualOrganizer.Instance.Shape_RingThickness_Pool.GetFromPool( this );
            return factionRing;
        }
        #endregion

        public void UpdateSquadShips()
        {
            if ( !this.BaseSquadUpdateChecksPassed )
                return;

            ShipVisualizer ship;
            for ( int i = 0; i < this.ShipsLiving.Count; i++ )
            {
                ship = this.ShipsLiving[i];
                ship.DoShipUpdate( this );
            }
        }

        public void HandleSquadShipCreationRequests( bool IsInEditor )
        {
            if ( !this.BaseSquadUpdateChecksPassed )
                return;
            if ( this.fatalDamageTaken ) //don't reinforce a squad that has taken fatal damage
                return;

            GameEntityTypeData typeData = this.RelatedTypeData;
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No GameEntityTypeData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            EntityTypeFactionVisualData factionVisualData = typeData.GetVisualsForFactionType( this.RelatedFactionType );
            if ( factionVisualData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No EntityTypeFactionVisualData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }

            {
                //If for some reason we don't have the right number of ships, get us to the right number
                int desiredShips = 1 - this.ShipsLiving.Count;
                for ( int i = 0; i < desiredShips; i++ )
                    this.AddSingleShip( factionVisualData );
            }
        }
        #endregion

        public int LastUpdateSpritesCycle = 0;

        public void UpdateSprites( float BaseShipIconScale )
        {
            this.Gimbal.DoLookAtAndScaleCheck( this, BattlefieldVisualSingleton.Instance, BaseShipIconScale );
            this.Gimbal.UpdateValuesIfDirty( this );
            //this.Gimbal.UpdateBurningAndDyingStatus( this.needsToExplode, this.SquadDyingProgress / this.SquadDeathTime );

            var relatedEnt = this.RelatedEntity.GetSquad();
            if (relatedEnt == null)
                return;

            var planet = relatedEnt.Planet;
            if ( planet == null )
                return;

            var typeData = relatedEnt.TypeData;
            if ( typeData == null )
                return;
            
            var dataForMark = relatedEnt.DataForMark;
            if ( dataForMark == null )
                return;

            int debugStage = 0;
            try
            {
                void HandleHoverAndFactionRing()
                {
                    debugStage = 10;
                    if ( ArcenUI.Instance.InHideGUIMode )
                    {
                        HideFactionRing();
                        HideHoverRing();
                        return;
                    }

                    var fring = GetFactionRing();
                    var hring = GetHoverRing();
                    var pos = CurrentPosition.ToUnityVector3();
                    var radius = dataForMark.CalculateRadiusForDisplay( planet );

                    // faction ring
                    {
                        debugStage = 20;

                        if ( !GameSettings.Current.GetBoolBySetting( "ShowFactionRingAroundShip" ) )
                        {
                            HideFactionRing();
                        }
                        else
                        {
                            fring.SetValues( pos, relatedEnt.GetFactionOrNull_Safe()?.FactionCenterColor.TeamColor ?? ColorMath.White, radius + 2.0f, 1.0f / planet.GravWellSize.GeneralMultiplier ); 
                        }
                    }

                    // hover ring
                    {
                        debugStage = 30;
                        var ringColor = ExternalVisualConstants.Instance.color_radii_debug_all_times;
                    
                        var selected = RelatedEntity.GetSquad()?.GetIsSelected() ?? false;
                        var hovered = relatedEnt == GameEntity_Base.CurrentlyHoveredOver;
                        var alwaysOn = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.Debug_DrawAllRadiiAtAllTimes );

                        var ringStatus = RingStatus.NoRing;
                    
                        if (hovered && selected)
                        {
                            ringStatus = RingStatus.HoveredSelected;
                            ringColor = ExternalVisualConstants.Instance.color_radii_hovered_selected;
                        }
                        else if (selected)
                        {
                            ringStatus = RingStatus.Selected;
                            ringColor = ExternalVisualConstants.Instance.color_radii_selected;
                        }
                        else if (hovered)
                        {
                            ringStatus = RingStatus.Hovered;
                            ringColor = ExternalVisualConstants.Instance.color_radii_hovered;
                        }
                        else if (alwaysOn)
                        {
                            ringColor = ExternalVisualConstants.Instance.color_radii_debug_all_times;
                            ringStatus = RingStatus.AlwaysOn;
                        }
                        else
                        {
                            HideHoverRing();
                            return;
                        }

                        debugStage = 40;

                        hring.SetValues( pos, ringColor, radius, 0.5f / planet.GravWellSize.GeneralMultiplier );

                        debugStage = 50;
                        if ( this.CurrentRingStatus != ringStatus )
                        {
                            //LOG.Msg("CurrentRingStatus now {0} (was {1})", ringStatus, CurrentRingStatus);
                            
                            if ( this.Gimbal != null )
                                this.Gimbal.IsDirty = true;
                            this.CurrentRingStatus = ringStatus;
                        }
                        debugStage = 60;
                    }
                }

                HandleHoverAndFactionRing();
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine(string.Format("debugStage {0}\n{1}", debugStage, e), Verbosity.ShowAsError);
            }
        }
        
        public Vector3 GetCurrentPositionWithEmissionOffset()
        {
            Vector3 pos = this.CurrentPosition;
            if ( this.RelatedTypeData != null )
                pos.Y += this.RelatedTypeData.YOffsetOfShipEmissionAndHitPoint;
            return pos;
        }

        private const float MAX_SPEED = 8f; //if the move speed gets too high then things look really jerky
        private int lastShipSpeed = 0;

        #region SetLocationToMatchSim
        private void SetLocationToMatchSim()
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null || relatedEnt.HasBeenRemovedFromSim || relatedEnt.ToBeRemovedAtEndOfThisFrame )
                return;

            ArcenPoint simLoc = relatedEnt.WorldLocation;
            if ( simLoc == ArcenPoint.OutOfRange || simLoc == ArcenPoint.ZeroZeroPoint )
                return;
            Planet planet = relatedEnt.Planet;
            if ( planet == null )
                return;

            GameEntityTypeData.MarkLevelStats dataForMark = relatedEnt.DataForMark;
            if ( dataForMark == null )
                return;

            this.targetLoc = simLoc.ToVisualMainGameCoordinates_Numerics( planet );

            if ( this.TimeLeftUntilVisuallyAppears > 0 )
            {
                this.CurrentPosition = this.targetLoc;
                this.SyncGimbalAndFFPos();
                return;
            }

            float moveSpeed = -1;

            if ( relatedEnt != null )
            {
                if ( dataForMark.Speed > 0 )
                    lastShipSpeed = dataForMark.Speed;
                else
                {
                    if ( relatedEnt.TypeData.IsMobileOrbiter || relatedEnt.TypeData.IsPlanetaryOrbiter )
                    {
                        if ( relatedEnt.TypeData.AttemptsToReachOrbitalPointAtSpeed > 0 )
                            lastShipSpeed = relatedEnt.TypeData.AttemptsToReachOrbitalPointAtSpeed;
                        else
                            lastShipSpeed = 5000;
                    }
                    else
                        lastShipSpeed = 100; //quite slow, since we're moving it apparently.  It's being pushed or dragged?
                }

                float largerDist = UnityEngine.Mathf.Max( Mat.Abs( targetLoc.X - this.CurrentPosition.X ), Mat.Abs( targetLoc.Z - this.CurrentPosition.Z ) );
                if ( largerDist > 1 )
                    moveSpeed = largerDist * 0.1f;
                if ( moveSpeed > MAX_SPEED )
                    moveSpeed = MAX_SPEED;
                if ( moveSpeed < 0.01f )
                    moveSpeed = 0.01f;
                //this.DebugText = largerDist + "\n" + speed;
            }

            this.movementSpeed = moveSpeed;
            if ( this.currentRotY < 0 )
                this.currentRotY += 360f;

            if ( moveSpeed <= 0 || GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.Debug_DrawTrueLocationOfSquadsInsteadOfLerps ) )
            {
                this.CurrentPosition = this.targetLoc;
                this.SyncGimbalAndFFPos();
                //if not moving, then sync the target rotation to that of the underlying ship
                this.targetRotY = relatedEnt.CurrentAngle.Tofloat();
            }
            else
            {
                // this is causing ugly results for things that want to never rotate
                // at all, but that option doesn't exist currently
                if ( relatedEnt.NonSim_TimeCurrentAngleLastSet >= ArcenTime.TimeSinceStartF - 1 )
                {
                    //if the rotation has been set within the last second, set it here instead of the movement rotation
                    this.targetRotY = relatedEnt.CurrentAngle.Tofloat();
                }
                else
                {
                    if ( Mat.Abs( this.targetLoc.X - this.CurrentPosition.X ) >= 1f || Mat.Abs( this.targetLoc.Z - this.CurrentPosition.Z ) >= 1f )
                    {
                        UnityEngine.Vector3 direction = (this.targetLoc - this.CurrentPosition).ToUnityVector3();
                        if ( direction != UnityEngine.Vector3.zero )
                        {
                            this.targetRotY = UnityEngine.Quaternion.LookRotation( direction ).eulerAngles.y;
                            //copy the angle from here to the ship
                            relatedEnt.CurrentAngle = AngleDegrees.Create( this.targetRotY );
                        }
                    }
                }
            }
        }
        #endregion

        public void SyncGimbalAndFFPos()
        {
            UnityEngine.Vector3 pos = this.CurrentPosition.ToUnityVector3();

            if ( this.ffObjectT )
                this.ffObjectT.position = pos;

            if ( this.otherAddonObject != null )
            {
                UnityEngine.Vector3 offsetPos = pos + new UnityEngine.Vector3( 0, this.currentAddOnYOffset, 0 );
                this.otherAddonObject.SetPosIfNeeded( offsetPos );
            }

            if ( ArcenMainGameVisuals.AllowYOffsetsForGimbals )
            {
                UnityEngine.Vector3 dir = ArcenMainGameVisuals.MainViewCamera.transform.position - pos;
                dir.Normalize();

                var dist = RelatedTypeData.YOffsetOfIcon + ExternalVisualConstants.Instance.extra_y_offset_to_all_icons;
                
                pos += dir * dist;
            }

            this.GimbalT.position = pos;
        }

        public void RenderSquad( int drawingCutoffLevel, bool areHeavyModelsDisabled )
        {
            //if ( GameEntity_Base.CurrentlyHoveredOver == relatedEnt )
            //    relatedEnt.DebugText += " ShipsLiving: " + this.ShipsLiving.Count + " ShipsDying: " + ShipsDying.Count;

            GameEntityTypeData typeData = this.RelatedTypeData;
            if ( typeData == null )
                return;

            if ( drawingCutoffLevel > typeData.VisualModelDrawCutoff || areHeavyModelsDisabled && typeData.IsParticularlyHeavyVisualModel )//|| this.TimeLeftUntilVisuallyAppears > 0 )
                return;

            for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                this.ShipsLiving[i].RenderShip();
            for ( int i = 0; i < this.ShipsDying.Count; i++ )
                this.ShipsDying[i].RenderShip();
        }

        public ShipVisualizer GetCompletelyRandomShip()
        {
            if ( this.ShipsLiving.Count <= 0 )
                return null;
            return this.ShipsLiving[Engine_Universal.PermanentQualityRandom.Next( 0, this.ShipsLiving.Count )];
        }

        private static readonly List<ShipVisualizer> workingShipPool_GetRandomShipThatCanFire = List<ShipVisualizer>.Create_WillNeverBeGCed( 3000, "SquadVisualizer-workingShipPool_GetRandomShipThatCanFire" );
        public ShipVisualizer GetRandomShipThatCanFire()
        {
            if ( this.ShipsLiving.Count <= 0 )
                return null;

            if ( workingShipPool_GetRandomShipThatCanFire.Count > 0 )
                workingShipPool_GetRandomShipThatCanFire.Clear();

            ShipVisualizer shipInSquad;
            float lowestTimeUntilIdeallyFireAgain = 10000f;
            for ( int i = 0; i < this.ShipsLiving.Count; i++ )
            {
                shipInSquad = this.ShipsLiving[i];
                if ( shipInSquad == null || !shipInSquad.IsStillInGame )
                    continue;
                if ( shipInSquad.TimeUntilIIdeallyFireAgain < lowestTimeUntilIdeallyFireAgain )
                    lowestTimeUntilIdeallyFireAgain = shipInSquad.TimeUntilIIdeallyFireAgain;
                if ( shipInSquad.TimeUntilIIdeallyFireAgain < 0.5f )
                    workingShipPool_GetRandomShipThatCanFire.Add( shipInSquad );
            }

            if ( workingShipPool_GetRandomShipThatCanFire.Count <= 0 && lowestTimeUntilIdeallyFireAgain < 100 )
            {
                lowestTimeUntilIdeallyFireAgain += 0.5f;
                for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                {
                    shipInSquad = this.ShipsLiving[i];
                    if ( shipInSquad == null || !shipInSquad.IsStillInGame )
                        continue;
                    if ( shipInSquad.TimeUntilIIdeallyFireAgain <= lowestTimeUntilIdeallyFireAgain )
                        workingShipPool_GetRandomShipThatCanFire.Add( shipInSquad );
                }
            }

            if ( workingShipPool_GetRandomShipThatCanFire.Count <= 0 )
            {
                for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                {
                    shipInSquad = this.ShipsLiving[i];
                    if ( shipInSquad == null || !shipInSquad.IsStillInGame )
                        continue;
                    workingShipPool_GetRandomShipThatCanFire.Add( shipInSquad );
                }
            }

            if ( workingShipPool_GetRandomShipThatCanFire.Count <= 0 )
                return null;
            int randomShipIndex = Engine_Universal.PermanentQualityRandom.Next( 0, workingShipPool_GetRandomShipThatCanFire.Count );
            if ( randomShipIndex >= workingShipPool_GetRandomShipThatCanFire.Count )
            {
                randomShipIndex = workingShipPool_GetRandomShipThatCanFire.Count - 1;
                if ( randomShipIndex < 0 )
                    return null;
                //ArcenDebugging.ArcenDebugLogSingleLine( "BUG?!?!?!", Verbosity.DoNotShow );
            }
            shipInSquad = workingShipPool_GetRandomShipThatCanFire[randomShipIndex];
            workingShipPool_GetRandomShipThatCanFire.Clear();

            return shipInSquad;
        }

        public void GetRandomShipsThatCanBeFiredUpon(int numShipsRequested, List<ShipVisualizer> listToFill, List<ShipVisualizer> excludedShipsList, ShipVisualizer excludedShipSingle )
        {
            //Attempt to find enough ships in a squad that are able to take a hit.
            //We first check whether there are enough "Preferred" ships, and failing that we try
            //every Living ships.
            //Note that we limit the ships that can be chosen by requiring they not be on the excludedShips
            //list, and also that they cannot have any shots currently in flight to them.
            //The general case for this is "When a shot changes targets, don't change targets to a ship
            //that already has a shot coming at it"

            //This allocates a new List every time so it's not the most performant thing in the world
            //making it a ready target for optimization if desired.
            int debugValue = 0;
            try
            {
                listToFill.Clear();
                debugValue = 1;
                if ( numShipsRequested <= 0 )
                    return;
                if ( this.ShipsLiving.Count <= 0 )
                    return;
                debugValue = 2;
                float lowestTimeSinceFiredOn = 10000f;
                ShipVisualizer shipInSquad;
                for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                {
                    debugValue = 3;
                    shipInSquad = this.ShipsLiving[i];
                    if ( shipInSquad == null )
                        continue;
                    if ( !shipInSquad.IsStillInGame )
                        continue;
                    if ( shipInSquad == excludedShipSingle )
                        continue;
                    if ( excludedShipsList != null && excludedShipsList.Contains( shipInSquad ) ) //skip excluded ships
                        continue;
                    if ( shipInSquad.IncomingShotCountAfterWhichIDie > 0 )//don't overkill
                        continue;
                    debugValue = 4;
                    if ( shipInSquad.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon < lowestTimeSinceFiredOn )
                        lowestTimeSinceFiredOn = shipInSquad.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon;
                    if ( shipInSquad.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon < 0.5f )
                        listToFill.Add( shipInSquad );
                }
                debugValue = 5;
                if ( listToFill.Count <= 0 && lowestTimeSinceFiredOn < 1000 )
                {
                    debugValue = 6;
                    lowestTimeSinceFiredOn += 0.2f;
                    for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                    {
                        debugValue = 7;
                        shipInSquad = this.ShipsLiving[i];
                        if ( !shipInSquad.IsStillInGame )
                            continue;
                        if ( shipInSquad == excludedShipSingle )
                            continue;
                        if ( excludedShipsList != null && excludedShipsList.Contains( shipInSquad ) ) //skip excluded ships
                            continue;
                        if ( shipInSquad.IncomingShotCountAfterWhichIDie > 0 )
                            continue;
                        debugValue = 8;
                        if ( listToFill.Contains( shipInSquad ) )
                            continue;
                        if ( shipInSquad.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon <= lowestTimeSinceFiredOn )
                            listToFill.Add( shipInSquad );
                    }
                }
                debugValue = 9;
                //if we don't have enough ships we "want" to use and there are more ships left
                //then we use the entire ShipsLiving list instead. Note this logic could be a bit tidier
                if ( listToFill.Count <= numShipsRequested && this.ShipsLiving.Count > listToFill.Count )
                {
                    listToFill.Clear();
                    debugValue = 10;
                    for ( int i = 0; i < this.ShipsLiving.Count; i++ )
                    {
                        debugValue = 11;
                        shipInSquad = this.ShipsLiving[i];
                        if ( shipInSquad == null )
                            continue;
                        if ( !shipInSquad.IsStillInGame )
                            continue;
                        if ( shipInSquad.IncomingShotCountAfterWhichIDie > 0 ) //if there's already a fatal shot, don't hit it again
                            continue;
                        if ( shipInSquad == excludedShipSingle )
                            continue;
                        if ( excludedShipsList != null && excludedShipsList.Contains( shipInSquad ) ) //skip excluded ships
                            continue;
                        debugValue = 12;
                        listToFill.Add( shipInSquad );
                    }
                }
                debugValue = 13;
                if ( listToFill.Count <= 0 )
                    return;
                int shipsToUse = listToFill.Count - 1;
                if ( numShipsRequested < shipsToUse )
                    shipsToUse = numShipsRequested;
                debugValue = 14;
            }
            catch ( ArgumentOutOfRangeException e )
            {
                if ( debugValue == 14 )
                    return; //this basically means that somehow we were out of range while looking in our workingShipPool list, which is a mystery to me.  Just ignore it, for now.

                ArcenDebugging.ArcenDebugLog( "ReactToShotHittingSquad Error at debug number" + debugValue + "\n" + e, Verbosity.ShowAsError );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "ReactToShotHittingSquad Error at debug number" + debugValue +
                                              "\n" + e, Verbosity.ShowAsError );
            }
            
        }

        #region HandleForcefieldIfAny
        public void HandleForcefieldIfAny()
        {
            GameEntity_Squad squad = this.RelatedEntity.GetSquad();
            if ( squad == null )
                return;
            GameEntityTypeData typeData = squad.TypeData;
            if ( typeData == null )
                return;
            GameEntityTypeData.MarkLevelStats dataForMark = squad.DataForMark;
            if ( dataForMark == null )
                return;
            if ( dataForMark.ShieldRadius <= 0 && !typeData.IsForcefieldProvidedByAnyModule )
                return; //if never supposed to render an FF

            int currentRadius = squad.CalculatedCurrentShieldRadius;
            if ( this.lastFFRadius == currentRadius )
                return; //nothing to do!

            this.lastFFRadius = currentRadius;

            switch ( OriginalDisplay )
            {
                case SquadDisplayType.HasForcefield:
                    if ( !this.ffObject )
                    {
                        this.ffObject = UnityEngine.GameObject.Instantiate( ArcenVisualOrganizer.Instance.ForcefieldSpherePrefab );
                        this.ffObjectT = this.ffObject.transform;
                        this.ffObjectT.SetDefaultTRS();
                        this.ffObjectT.parent = ArcenVisualOrganizer.Instance.MainGameObjectParent;
                        this.ffRippler = this.ffObject.GetComponentInChildren<ArcenForcefieldRippler>( true );
                        this.ffObjectT.position = this.CurrentPosition.ToUnityVector3();
                        this.ffRenderers = this.ffObject.GetComponentsInChildren<UnityEngine.MeshRenderer>( true );
                    }
                    break;
            }

            if ( !ffObjectT )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No ff object for" + typeData.InternalName + " OriginalDisplay: " + OriginalDisplay, Verbosity.DoNotShow );
                return;
            }

            Planet planet = squad.Planet;
            float ffDivisor = planet == null ? 10f : planet.GravWellSize.ForcefieldVisualScaleDivisor;

            float finalRadius = currentRadius / ffDivisor;            
            this.ffObjectT.localScale = new UnityEngine.Vector3( finalRadius, finalRadius, finalRadius );
        }
        #endregion

        #region ShowOrHideForcefieldIfNeeded
        private bool wasFFLastVisible = true;
        private void ShowOrHideForcefieldIfNeeded( bool ShouldBeVisible )
        {
            if ( ShouldBeVisible == wasFFLastVisible )
                return;
            if ( this.ffRenderers == null )
                return;
            this.wasFFLastVisible = ShouldBeVisible;
            for ( int i = 0; i < this.ffRenderers.Length; i++ )
                this.ffRenderers[i].enabled = ShouldBeVisible;
        }
        #endregion

        private float currentAddOnYOffset = 0;

        #region HandleOtherVisualAddonIfAny
        public void HandleOtherVisualAddonIfAny()
        {
            int debugstage = 0;
            try
            {
                debugstage = 1;
                GameEntity_Squad squad = this.RelatedEntity.GetSquad();
                if ( squad == null )
                    return;
                
                debugstage = 2;
                GameEntityTypeData typeData = squad.TypeData;
                if ( typeData == null )
                    return;

                debugstage = 3;
                AddonObjectOrParticleField addOn = typeData.AddonObjectOrParticleField;
                if ( addOn == null ) //this is supposed to be null
                {
                    debugstage = 4;
                    if ( this.otherAddonObject != null )
                    {
                        debugstage = 5;
                        this.otherAddonObject.ReturnToPool();
                        this.otherAddonObject = null;
                        
                        debugstage = 6;
                        return;
                    }
                    
                    debugstage = 7;
                    return;
                }

                debugstage = 8;
                if ( this.otherAddonObject != null )
                {
                    debugstage = 9;
                    //this is the wrong kind of object!  Stop showing one, and move to the next.
                    if ( this.otherAddonObject.PoolID != addOn.PoolID )
                    {
                        debugstage = 10;
                        this.otherAddonObject.ReturnToPool();
                        this.otherAddonObject = null;
                    }
                }

                debugstage = 11;
                if ( this.otherAddonObject != null )
                {
                    //evidently it is the right kind of object since it is not null
                }
                else
                {
                    debugstage = 12;
                    //it was null, so initialize it
                    this.otherAddonObject = addOn.Pool.GetNewObjectOrObjectFromPool( true );
                    this.otherAddonObject.SetActiveStatusIfNeeded( true );
                }

                debugstage = 13;
                currentAddOnYOffset = typeData.AddonObjectOrParticleFieldYOffset;
                UnityEngine.Vector3 pos = this.CurrentPosition.ToUnityVector3() + new UnityEngine.Vector3( 0, this.currentAddOnYOffset, 0 );

                debugstage = 14;
                this.otherAddonObject.SetPosIfNeeded( pos );
                this.otherAddonObject.SetScale( typeData.AddonObjectOrParticleFieldScale );

                debugstage = 15;
                
                addOn.DoEverySimStep();
                debugstage = 16;
            }
            catch (Exception e)
            {
                LOG.Err("Error at debugstage {0}\n{1}", debugstage, e);
            }
        }
        #endregion

        public void DoHitAgainstMyForcefield( Vector3 hitPoint, float hitPower = 0, float hitAlpha = 1 )
        {
            if ( this.ffRippler )
                this.ffRippler.DoHit( hitPoint.ToUnityVector3(), hitPower, hitAlpha );
        }

        public Vector3 GetCloserPointOnForceFieldIfPossible( Vector3 hitPoint )
        {
            if ( this.ffRippler )
                return ffRippler.GetCloserPointOnForceFieldIfPossible( hitPoint.ToUnityVector3() ).ToNumericsVector3();
            return hitPoint;
        }

        #region AddStartingShips
        public void AddStartingShips()
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();

            GameEntityTypeData typeData = this.RelatedTypeData;
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No GameEntityTypeData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            EntityTypeFactionVisualData factionVisualData = typeData.GetVisualsForFactionType( this.RelatedFactionType );
            if ( factionVisualData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No EntityTypeFactionVisualData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            if ( factionVisualData.SubInstancedRenderer == null )
            {
                if ( !this.DealWithNullSubInstanceRenderer( factionVisualData ) )
                    return;
            }
            
            if ( relatedEnt == null )
            {
                if (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.VisMemLeak )) {
                    ArcenDebugging.ArcenDebugLog( "No RelatedEntity defined for squad of RelatedFactionData of " + this.myID + ": " +
                        typeData.InternalName + " " + this.RelatedFactionType, Verbosity.DoNotShow );
                }
                return;
            }

            int shipCountToAdd = 1 - this.ShipsLiving.Count;

            for ( int i = 0; i < shipCountToAdd; i++ )
                this.GetOrCreateNewShipVisualizer( factionVisualData );
        }
        #endregion

        private ShipVisualizer GetOrCreateNewShipVisualizer( EntityTypeFactionVisualData factionVisualData )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null )
                return null;
            ShipVisualizer newShip = (ShipVisualizer)factionVisualData.SubInstancedRenderer.GetInstancedRendererFromPersonalPool( relatedEnt );
            if ( newShip == null )
                return null;
            newShip.CurrentSquad = this;
            newShip.Activate( relatedEnt, this.RelatedTypeData );
            newShip.SetFormationPositionFromSquad( this );
            this.ShipsLiving.Add( newShip );
            return newShip;
        }

        #region DealWithNullSubInstanceRenderer
        private bool DealWithNullSubInstanceRenderer( EntityTypeFactionVisualData factionVisualData )
        {
            if ( factionVisualData.PrototypeObject == null)
            {
                //just make it invisible.  We have already errored elsewhere
                return false;
            }
            if ( factionVisualData.CorePoolableObject == null )
            {
                //just make it invisible.  We have already errored elsewhere
                return false;
            }
            factionVisualData.SubInstancedRenderer = Engine_AIW2.Instance.PresentationLayer.GetOrCreateNewInstancedRenderer( factionVisualData.TypeData, factionVisualData.CoreVisualsFilename, factionVisualData.CorePoolableObject );
            if ( factionVisualData.SubInstancedRenderer == null )
            {
                ArcenDebugging.ArcenDebugLog( "No SubInstancedRenderer was able to be initialized on object named '" + factionVisualData.PrototypeObject.name + "' from filename " +
                    (factionVisualData.CoreVisualsFilename == null ? "(NotApplicable)" : factionVisualData.CoreVisualsFilename.CombinedPath), Verbosity.ShowAsError );
                return false;
            }
            else
            {
                ArcenDebugging.ArcenDebugLog( "SubInstancedRenderer had to be fixed for RelatedFactionData of " + this.myID + ": " +
                factionVisualData.TypeData.InternalName + " fornatobject: " + factionVisualData.IsForNaturalObjectType, Verbosity.DoNotShow );
                return true;
            }
        }
        #endregion

        #region AddSingleShip
        public void AddSingleShip( EntityTypeFactionVisualData factionVisualData )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null )
                return;

            bool localDebug = false;
            if ( factionVisualData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No EntityTypeFactionVisualData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            if ( factionVisualData.SubInstancedRenderer == null )
            {
                if ( !this.DealWithNullSubInstanceRenderer( factionVisualData ) )
                    return;
            }

            int shipCountAvailableToAdd = 1 - this.ShipsLiving.Count;
            if ( shipCountAvailableToAdd <= 0 )
                return; //prevent us from adding too many ships
            if (localDebug)
                ArcenDebugging.ArcenDebugLogSingleLine("Attempting to add ship to " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " ships living: " + this.ShipsLiving.Count + " ships dying: " + this.ShipsDying.Count + " theoretical max 1", Verbosity.DoNotShow );

            this.GetOrCreateNewShipVisualizer( factionVisualData );
        }
        #endregion

        public int LastLODAndShipPartAnimationCycle = 0;
        public float ShipAnimation_AccumulatedDeltaTime = 0;

        #region HandleLODsAndShipPartAnimationsForSquad
        public void HandleLODsAndShipPartAnimationsForSquad( Vector3 CameraPos, bool IsInEditor )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();

            bool localDebug = false;
            this.AlterLODIfNeeded( CameraPos );

            float deltaTime = this.ShipAnimation_AccumulatedDeltaTime;
            ShipVisualizer ship;
            string lethalLogStr = "Lethal damage, moved to dying: ";
            string notExplodedYet = "";
            bool worthPrinting = false;
            if(this.ShipsLiving.Count == 0 && this.fatalDamageTaken && !this.needsToExplode)
            {
                if (localDebug)
                ArcenDebugging.ArcenDebugLogSingleLine("Visual: Squad " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " setting needsToExplode", Verbosity.DoNotShow );
                this.needsToExplode = true;
                return;
            }
            if(this.fatalDamageTaken && this.ShipsLiving.Count == 0 && this.ShipsDying.Count == 0)
            {
                //early escape for a squad that's in the process of dying but hasn't finished dying yet
                return;
            }
            for ( int i = this.ShipsLiving.Count - 1; i >= 0; i-- )
            {

                ship = this.ShipsLiving[i];
                if (ship.stillAntiSpawning)
                {
//                    ArcenDebugging.ArcenDebugLogSingleLine("Updating ship " + i + " in squad " + relatedEnt.PrimaryKeyID + " despawn path", Verbosity.DoNotShow );
                    bool rc = ship.UpdateAntiSpawnStatus(deltaTime);
                    if(rc)
                    {
                        //if this ship is done anti-spawning (ie it has shrunk to nothingness)
                        //then remove it entirely
                        this.ShipsLiving.Remove(ship);
                        ship.RemoveShipFromFormation(); //this ship is dying and no longer part of a formation
//                        ArcenDebugging.ArcenDebugLogSingleLine("Updating ship " + i + " in squad " + relatedEnt.PrimaryKeyID + " despawn path. Done now", Verbosity.DoNotShow );
                        continue;
                    }
                }
                if (ship.stillSpawning)
                {
                    //If this ship is being added to the squad as reinforcements
                    bool rc = ship.UpdateSpawnStatus(deltaTime);
                    if(rc)
                    {
                        //if this ship is done spawning, switch to regular material
                        ship.UnsetSpawnStatus();
                    }
                }
                
                ship.DoGeneralUpdate( deltaTime );
                if(localDebug)
                {
                    if(ship.IncomingShotCountAfterWhichIDie > 0 && ship.hasTakenLethalDamage)
                        lethalLogStr += ship.myID + " ";
                    else if(ship.IncomingShotCountAfterWhichIDie > 0)
                        notExplodedYet += ship.myID + " ";
                }
                if(ship.hasTakenLethalDamage || this.fatalDamageTaken)
                {
                    worthPrinting = true;
                    this.ShipsLiving.Remove(ship);
                    if ( !this.ShipsDying.Contains( ship ) )
                        this.ShipsDying.Add(ship);
                    continue;
                }
            }
            if(localDebug)
            {
                if(worthPrinting)
                    ArcenDebugging.ArcenDebugLogSingleLine(this.RelatedTypeData.InternalName + " " + this.myID + " " + lethalLogStr + " " + notExplodedYet, Verbosity.DoNotShow );
            }
            for ( int i = this.ShipsDying.Count - 1; i >= 0; i-- )
            {
                ship = this.ShipsDying[i];
                if(!ship.hasTakenLethalDamage)
                {
                    if (localDebug)
                        ArcenDebugging.ArcenDebugLogSingleLine("SquadVis: Squad " + relatedEnt.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + "  ordering a ship " + ship.myID + " to take lethal damage because the squad is dying", Verbosity.DoNotShow );
                    ship.IncomingShotCountAfterWhichIDie++;
                }
                ship.DoGeneralUpdate( deltaTime ); //this will remove the ship from the ShipsDying list if it is in fact dying
                //Now that the ship is dying, remove it from the formation so reinforcements can take its place.
                //Once a ship is dying it will go on the BurningAndDying list and explode promptly, so no need
                //to hold onto its formation spot
                ship.RemoveShipFromFormation(); //this ship is dying and no longer part of a formation
            }
            if(localDebug)
            {
                if(this.ShipsLiving.Count != 1)
                {
                    string logStr = "list IDs: ";
                    for( int i = this.ShipsLiving.Count - 1; i >= 0; i-- )
                    {
                        logStr += "<" + this.ShipsLiving[i].myID + " " + this.ShipsLiving[i].IncomingShotCountAfterWhichIDie + "> ";
                    }
                    //this is not really a bug because the sim and the Vis layers can be out of sync for brief intervals
                    //ArcenDebugging.ArcenDebugLogSingleLine("BUG: squad " + this.myID + " " + this.RelatedTypeData.InternalName + " update: VIS living " + this.ShipsLiving.Count + " SIM living " + this.GetSquadSize(true) + " " + logStr, Verbosity.DoNotShow );
                }
            }
        }
        #endregion

        public VisualObjectType GetObjectType()
        {
            return VisualObjectType.SquadOfShips;
        }

        public bool GetIsSquad()
        {
            return true;
        }

        public int ReactToShotHittingSquad( GameEntity_Squad TargetSquad, GameEntity_Squad ProtectingShieldThatTookTheHitOrNull, int NumberOfShipsKilled, bool WasEntireSquadKilled ) { return 0; }

        public void ReactToSquadReplacementShipJustDeployed( GameEntity_Squad EntityProvidingReplacement )
        {
            if ( this.ToBeRemovedType != SquadRemovalType.None )
                return;
            if ( this.fatalDamageTaken )
                return;
            int shipCountAvailableToAdd = 1 - this.ShipsLiving.Count;
            if ( shipCountAvailableToAdd <= 0 )
                return; //prevent us from adding too many ships

            GameEntityTypeData typeData = this.RelatedTypeData;
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No GameEntityTypeData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            EntityTypeFactionVisualData factionVisualData = typeData.GetVisualsForFactionType( this.RelatedFactionType );
            if ( factionVisualData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No EntityTypeFactionVisualData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }

            this.AddSingleShip( factionVisualData );
        }

        public void ShowBurningAndDyingEffectForShipInStackIfVisualObjectExistsForStack( GameEntity_Squad EntityProvidingReplacement )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            if ( relatedEnt == null )
                return;

            ShipVisualizer newShip = null;

            GameEntityTypeData typeData = this.RelatedTypeData;
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No GameEntityTypeData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }
            EntityTypeFactionVisualData factionVisualData = typeData.GetVisualsForFactionType( this.RelatedFactionType );
            if ( factionVisualData == null )
            {
                ArcenDebugging.ArcenDebugLog( "No EntityTypeFactionVisualData defined on squad object " + this.myID, Verbosity.DoNotShow );
                return;
            }

            try
            {
                newShip = (ShipVisualizer)factionVisualData.SubInstancedRenderer.GetInstancedRendererFromPersonalPool( relatedEnt );
            }
            catch { return; } //sometimes things don't work out, oh well.

            if ( newShip == null )
                return;

            try
            {
                newShip.CurrentSquad = this;
                newShip.Activate( relatedEnt, this.RelatedTypeData );
                newShip.SetFormationPositionFromSquad( this );
                newShip.DoDeathEffects();
            }
            catch //sometimes things don't work out, oh well.
            {
                if ( newShip != null )
                {
                    newShip.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.VisLayerNullRelatedStuff, true );
                }
            }
        }

        public System.Numerics.Vector3 GetPositionPlusAnyOffsets()
        {
            ShipVisualizer ship = null;
            if ( this.ShipsLiving.Count > 0 )
            {
                try
                {
                    ship = this.ShipsLiving[0];
                }
                catch { }
            }
            if ( ship == null && this.ShipsDying.Count > 0 )
            {
                try
                {
                    ship = this.ShipsDying[0];
                }
                catch { }
            }
            if ( ship != null )
                return ship.GetCurrentEmissionHitPoint();
            return this.CurrentPosition;
        }

        public void WriteDebugDataTo( ArcenCharacterBuffer Buffer )
        {
            GameEntity_Squad relatedEnt = this.RelatedEntity.GetSquad();
            //write debug data to the buffer that you want to go to the output log
            if ( relatedEnt != null )
            {
                Buffer.Add( "\n" ).Add( relatedEnt.TypeData.InternalName );
            }
        }

        [Obsolete]
        void IInstancedRenderer.UpdateTeamColor()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Explain if you're the same squad visualizer or not
        /// </summary>
        public bool GetIsShapeOwner( IArcenShapeOwner Other )
        {
            if ( Other is SquadVisualizer otherSquad )
                return otherSquad.myID == this.myID;
            return false;
        }
    }
}
