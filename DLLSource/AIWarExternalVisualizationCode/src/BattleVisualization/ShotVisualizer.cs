using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

//Note that we are NOT using any of the UnityEngine math here.
//Instead we're using SIMD-based System.Numerics, which is more efficient and all-C#
using System.Numerics;
//We do want this available for basic math,
//however it will fail to work with the Vector3s from System.Numerics
using Mathf = UnityEngine.Mathf;
using Color = UnityEngine.Color;

/* Badger todo:

   Instead of flying to target.CurrentPosition, fly toward LineEmissAndHitPoint
   Note that LineEmissAndHitPoint can vanish, so cache the location just in case
*/

namespace Arcen.AIW2.ExternalVisualization
{
    public class ShotVisualizer : ConcurrentPoolable<ShotVisualizer>, IInstancedRenderer
    {
        /* Notes on the lifespan of a shot */
        /* A shot is created by an EntitySystem (which represents the guns of a ship). It is a GameEntity_Shot as well.
           After being created, the Sim will make the shot A. create a visual object, and B. hit a target and do damage.
           A and B can happen in either order, but both definitely happen.
           Let's say that A happens first. So from the Vis point of view, we get a request for a new Shot object and
           call AddShot --> wireshotToTarget. The shot will now be in LerpingBetweenEquivalentPoints and head toward the target.
           Once the Sim calls DoHitLogic (to actually make the shot hit), this will eventually call ReactToQueuedShotHittingsquad.
           Once the Vis layer calls ReactToQueuedShot the shot moves from LerpingBetweenEquivalentPoints mode to
           "InterceptTarget or Specific Location", and dives right at the target.
           Once the shot Hits the target it is Deactivated.

           Now, the one caveat is "What happens when DoHitLogic is called before the shot gets its visual object?"
           In this case the GameEntity_Shot will record a QueuedHitRequest. When the visual object if finally obtained it will
           then call ReactToQueuedShotHittingSquad for all the queuedHitRequests. This means that such shots never go
           through the LerpingBetweenEquivalentPoints code, so they will look a bit different.

           Shots can also be deactivated for other reasons (maybe you've tabbed away from the planet?), but
           the above is the primary code path.

           There are lots of other fun gotchas, like "When a shot hits a squad, check all the other shots in flight
           to see if they are headed for the same squad, and if so then make sure they are hitting different ships.
           We don't want to overkill a given ship.
        */
        internal ShotRenderManagerGroup Pool = null;

        public Vector3 CurrentPosition = Mat.V3N_Zero;
        private UnityEngine.Quaternion CurrentRotation = Mat.Quater_Ident;
        private bool currentRotationIsSet = false;

        public GameEntity_Shot RelatedEntity;
        public GameEntityTypeData RelatedTypeData;
        public Planet planet;

        public float movementSpeed = 0f;
        public float finalApproachProgress = 0f;
        public float accumulatedLifespanDeltaTime = 0f;
        public Vector3 startingLocation = Mat.V3N_Zero;

        private Vector3 targetLoc = Mat.V3N_Zero;
        public ArcenPoint PriorLoc = ArcenPoint.OutOfRange;
        
        public WrapperedAddonObjectOrParticleField otherAddonObject;
        
        public ShotDisplayStyle ShotDisplay = ShotDisplayStyle.TrueLocationByDesign;
        public float TimeWaitingForShotData = 0;
        public float ShotSquadOriginalDist = 0f;
        public float ShotSquadCurrentFullDist = 0f; //if the target moved, we need to take that into account
        public ShipVisualizer ShotOriginShip = null;
        public Vector3 ShotOriginGameStylePoint = Mat.V3N_Zero;
        public ShipVisualizer ShotCurrentTargetShip = null;
        public SquadVisualizer ShotCurrentTargetSquad = null;
        public Vector3 ShotSquadTargetGameStylePoint = Mat.V3N_Zero;
        public Vector3 ShotTargetLoc = Mat.V3N_Zero;
        public float ShotPercentageBetweenEmissAndTarget = 0f;
        public Vector3 ShotShipEmissionPoint = Mat.V3N_Zero;
        public Vector3 ShotCurrentTargetPoint = Mat.V3N_Zero;
        public Vector3 ShotStartingPointForFinalApproach = Mat.V3N_Zero;
        public float DeathCounterInevitable_TimeLeft = -1f;
        public bool DeathCounterInevitable_Enabled = false;
        public ShotEOLType EOLType = ShotEOLType.Dissipating;
        public ArcenPoint Fallback_LastTargetPoint;
        public GameEntityTypeData Fallback_LastOwnType;
        public SquadVisualizer SquadWithForcefieldWeShot;
        public float AccumulatedTimeBeforeStartingDisplay; //in order to make shots not synchronous visually,
                                                   //We will wait some random number of sim steps before displaying the shot
        public int minDelay; //the min/max/multiplier are used to compute a reasonable AccumulatedTimeBeforeStartingDisplay. I'm using 3 variables since I'm not sure how to randomly generate a float. So I get a random int between min and max, then multiply it
        public int maxDelay;
        public float delayMultiplier; //setting this will introduce a delay between when a shot is fired and when it appears.
                                      //This is currently disabled.
        public readonly List<DebuggingLine> DebuggingLines = List<DebuggingLine>.Create_WillNeverBeGCed( 7000, "ShotVisualizer-DebuggingLines" );
        
        private readonly List<ShipVisualizer> potentialShipTargetsList;
        private readonly List<ShipVisualizer> shipsTakingHits; //used for shots that hit multiple subsquads in a squad

        public bool ShouldRenderThisFrame = true;
        public bool HasRenderedAOEAlready = false;

        //We use a list of potential targets in a couple places. Rather than allocate a new
        //list every time, use one list and Clear() it between uses. Note this relies on
        //the Vis code here being single threaded, which it is
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ShotVisualizers" );
        public ShotVisualizer()
        {
            RefTracker.IncrementObjectCount();
            this.RelatedTypeData = null;
            this.RelatedEntity = null;
            this.SquadWithForcefieldWeShot = null;
            this.AccumulatedTimeBeforeStartingDisplay = -1;
            this.minDelay = -1;
            this.maxDelay = -1;
            this.delayMultiplier = -1f;
            this.potentialShipTargetsList = List<ShipVisualizer>.Create_WillNeverBeGCed( 30, "ShotVisualizer-potentialShipTargetsList" );
            this.shipsTakingHits = List<ShipVisualizer>.Create_WillNeverBeGCed( 30, "ShotVisualizer-shipsTakingHits" );
        }

        public bool IsConsideredActive = false;
        public void Activate( GameEntity_Base RelatedTo, GameEntityTypeData RelatedType )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( this.IsConsideredActive )
                return;
            this.IsConsideredActive = true;
            this.PriorLoc = ArcenPoint.OutOfRange;
            this.RelatedEntity = (GameEntity_Shot)RelatedTo;
            this.planet = RelatedTo == null ? null : RelatedTo.Planet;
            this.RelatedTypeData = RelatedType;
            this.CurrentPosition = Mat.V3N_Zero;
            this.currentRotationIsSet = false;
            
            this.TimeWaitingForShotData = 0;
            this.ShotDisplay = ShotDisplayStyle.TrueLocationByFailure;
            this.ShotSquadOriginalDist = 0f;
            this.ShotSquadCurrentFullDist = 0f;
            this.ShotTargetLoc = Mat.V3N_Zero;
            this.ShotShipEmissionPoint = Mat.V3N_Zero;
            this.ShotCurrentTargetShip = null;
            this.ShotCurrentTargetSquad = null;
            this.ShotOriginShip = null;
            this.ShotOriginGameStylePoint = Mat.V3N_Zero;
            this.ShotSquadTargetGameStylePoint = Mat.V3N_Zero;
            this.ShotCurrentTargetPoint = Mat.V3N_Zero;
            this.ShotStartingPointForFinalApproach = Mat.V3N_Zero;
            this.ShotPercentageBetweenEmissAndTarget = 0;
            this.finalApproachProgress = 0;
            this.DeathCounterInevitable_TimeLeft  = -1f;
            this.DeathCounterInevitable_Enabled = false;
            this.hasHandledShotHit = false;
            this.EOLType = ShotEOLType.Dissipating;
            this.Fallback_LastTargetPoint = ArcenPoint.ZeroZeroPoint;
            this.Fallback_LastOwnType = null;
            this.ShouldRenderThisFrame = true;

            this.accumulatedLifespanDeltaTime = 0f;
            this.startingLocation = Mat.V3N_Zero;

            this.RemovalChecks_LastAnimationCycle = -1;
            this.ShotMovement_LastAnimationCycle = -1;
            this.ShotMovement_AccumulatedDeltaTime = 0;
            this.SquadWithForcefieldWeShot = null;
            this.AccumulatedTimeBeforeStartingDisplay = -1;
            //Set the min/max/multiplier are set here in case we want to do it based on how fast the shot it
            this.minDelay = 1;
            this.maxDelay = 50;
            this.delayMultiplier = 0f; //this is currently disabled
            this.isOldLocationSet = false;
            this.currentSpeed = 0f;
            this.topmostScale = 1f;
            this.trailZScale = 1;
            this.HasRenderedAOEAlready = false;

            // handle addon effect
            {
                AddonObjectOrParticleField addOn = RelatedType.AddonObjectOrParticleField;
                if ( addOn == null || this.otherAddonObject != null )
                {
                    if ( this.otherAddonObject != null )
                    {
                        this.otherAddonObject.ReturnToPool();
                        this.otherAddonObject = null;
                    }
                }
                
                if ( addOn != null )
                {
                    this.otherAddonObject = addOn.Pool.GetNewObjectOrObjectFromPool( true );
                    this.otherAddonObject.SetActiveStatusIfNeeded( true );
               
                    float yoffset = RelatedType.AddonObjectOrParticleFieldYOffset;
                    UnityEngine.Vector3 pos = this.CurrentPosition.ToUnityVector3() + new UnityEngine.Vector3( 0, yoffset, 0 );

                    this.otherAddonObject.SetPosIfNeeded( pos );
                    this.otherAddonObject.SetScale( RelatedType.AddonObjectOrParticleFieldScale );

                    addOn.DoEverySimStep();
                }
            }

            this.AddShot(null);
            this.potentialShipTargetsList.Clear();
            this.shipsTakingHits.Clear();
            BattlefieldVisualSingleton.Instance.ActiveShots.AddToActiveListNoChecks( this );
        }
        public string GetTempDebugInfo()
        {
            string retVal = string.Empty;
            return retVal;
        }

        public object GetTempDebugObject()
        {
            return null;
        }

        public void DeactivateAndReturnToPool( InstancedRendererDeactivationReason Reason, bool ForceEvenIfNotConsideredActive )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( !this.IsConsideredActive && !ForceEvenIfNotConsideredActive )
                return;
            
            this.IsConsideredActive = false; //must happen right at the top, above ClearVisualObjIfExists, or bad things happen!

            GameEntity_Shot relatedEntOrNull = this.RelatedEntity;
            if ( relatedEntOrNull != null )
                relatedEntOrNull.ClearVisualObjIfExists( true, false, InstancedRendererDeactivationReason.VisSideDeactivateAndReturnToPoolBeingCareful );

            this.currentRotationIsSet = false;
            this.RelatedEntity = null;
            this.RelatedTypeData = null;
            this.TimeWaitingForShotData = 0;
            this.ShotDisplay = ShotDisplayStyle.TrueLocationByFailure;
            this.ShotSquadOriginalDist = 0f;
            this.ShotSquadCurrentFullDist = 0f;
            this.ShotTargetLoc = Mat.V3N_Zero;
            this.ShotShipEmissionPoint = Mat.V3N_Zero;
            this.ShotCurrentTargetShip = null;
            this.ShotCurrentTargetSquad = null;
            this.ShotOriginShip = null;
            this.ShotOriginGameStylePoint = Mat.V3N_Zero;
            this.ShotSquadTargetGameStylePoint = Mat.V3N_Zero;
            this.ShotCurrentTargetPoint = Mat.V3N_Zero;
            this.ShotStartingPointForFinalApproach = Mat.V3N_Zero;
            this.ShotPercentageBetweenEmissAndTarget = 0;
            this.finalApproachProgress = 0;
            this.DeathCounterInevitable_TimeLeft  = -1f;
            this.DeathCounterInevitable_Enabled = false;
            this.EOLType = ShotEOLType.Dissipating;
            this.Fallback_LastTargetPoint = ArcenPoint.ZeroZeroPoint;
            this.Fallback_LastOwnType = null;
            this.ShouldRenderThisFrame = false;
            this.SquadWithForcefieldWeShot = null;

            this.accumulatedLifespanDeltaTime = 0f;
            this.startingLocation = Mat.V3N_Zero;

            this.RemovalChecks_LastAnimationCycle = -1;
            this.ShotMovement_LastAnimationCycle = -1;
            this.ShotMovement_AccumulatedDeltaTime = 0;
            this.AccumulatedTimeBeforeStartingDisplay = -1;
            this.minDelay = -1;
            this.maxDelay = -1;
            this.delayMultiplier = -1;
            this.HasRenderedAOEAlready = false;

            this.isOldLocationSet = false;
            this.currentSpeed = 0f;
            this.topmostScale = 1f;
            this.trailZScale = 1;
            this.potentialShipTargetsList.Clear();
            
            if ( this.otherAddonObject != null )
            {
                this.otherAddonObject.ReturnToPool();
                this.otherAddonObject = null;
            }
            
            ShotRenderManagerGroup myPoolOrNull = this.Pool;
            if ( myPoolOrNull != null )
                myPoolOrNull.PutBackInPoolRightAway( this );
            this.Pool = null;
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.IAmHeadedBackToPool, true );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public InstancedRendererDeactivationReason GetLastFlagForRemovalReason()
        {
            return InstancedRendererDeactivationReason.Unknown;
        }

        public void DetachVisObjectFromSim( InstancedRendererDeactivationReason Reason )
        {
            //UnityEngine.Debug.Log( ( this.RelatedTypeData == null ? "Null shot" : this.RelatedTypeData.InternalName ) + " IsConsideredActive: " + this.IsConsideredActive + " Reason:" + Reason );
            this.FlagForRemoval(true, Reason );
        }
        public void FlagForRemoval( bool ForceRemoval, InstancedRendererDeactivationReason Reason )
        {
            if ( !this.IsConsideredActive )
                return;

            switch ( this.ShotDisplay )
            {
                case ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames:
                case ShotDisplayStyle.HeadingToSpecificLocationAcrossLastFewFrames:
                    {
                        if ( this.DeathCounterInevitable_Enabled && this.DeathCounterInevitable_TimeLeft > 0 )
                            return;
                    }
                    break;
            }
            this.EmitOnAOEParticles( Reason );
            this.DeactivateAndReturnToPool( Reason, false );
        }

        //public Vector3 GetPosition()
        //{
        //    return this.CurrentPosition;
        //}

        public bool GetIsSquad()
        {
            return false;
        }

        public void UpdateTeamColor() { }
        public void ReactToSquadReplacementShipJustDeployed( GameEntity_Squad EntityProvidingReplacement ) { }
        public void ShowBurningAndDyingEffectForShipInStackIfVisualObjectExistsForStack( GameEntity_Squad EntityProvidingReplacement ) { }
        public void WriteDebugDataTo( ArcenCharacterBuffer Buffer ) { }
        
        #region SetLocationToMatchSim
        private void SetLocationToMatchSim()
        {
            GameEntity_Shot relatedEnt = this.RelatedEntity;
            if ( relatedEnt == null || relatedEnt.HasBeenRemovedFromSim )
                return;

            ArcenPoint simLoc = relatedEnt.WorldLocation;
            if ( simLoc == ArcenPoint.OutOfRange || simLoc == ArcenPoint.ZeroZeroPoint || simLoc == this.PriorLoc )
                return;
            
            float speed = -1;
            if ( this.PriorLoc != ArcenPoint.OutOfRange )
            {
                float largerDist = Mathf.Max( Mat.Abs( simLoc.X - this.PriorLoc.X ), Mat.Abs( simLoc.Y - this.PriorLoc.Y ) );
                if ( largerDist > 1 )
                    speed = largerDist * 0.1f;

                //this.DebugText = largerDist + "\n" + speed;
            }

            this.PriorLoc = simLoc;
            this.movementSpeed = speed;
            this.targetLoc = simLoc.ToVisualMainGameCoordinates_Numerics( planet );

            if ( speed < 0 )
                this.CurrentPosition = this.targetLoc;
            //else
            //{
            //    Vector3 pos = this.CurrentPosition;
            //    if ( Mat.Abs( this.targetLoc.x - pos.x ) >= 1 || Mat.Abs( this.targetLoc.z - pos.z ) >= 1 )
            //        this.selfT.LookAt( this.targetLoc );
            //}
        }
        #endregion

        public GameEntity_Base GetEntityRelatedTo()
        {
            return this.RelatedEntity;
        }

        public int RemovalChecks_LastAnimationCycle = -1;

        public void DoRemovalChecks( int CurrentSimFrameVisualOnly )
        {
            if ( !this.IsConsideredActive )
                return;

            if ( this.DeathCounterInevitable_Enabled && this.DeathCounterInevitable_TimeLeft <= 0 )
            {
                this.HandleVisualsOfShotHit();
                this.FlagForRemoval( true, InstancedRendererDeactivationReason.VisLayerDeathCounterInevitableReachedZero );
                return;
            }

            GameEntity_Shot relatedShot = this.RelatedEntity;
            if ( relatedShot == null )
            {
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerNullRelatedStuff );
                return;
            }

            int lastFrame = relatedShot.LastSimFrameToldToHaveVisualObject;
            if ( lastFrame > CurrentSimFrameVisualOnly || CurrentSimFrameVisualOnly > lastFrame + 3 ) //|| relatedShot.HasBeenRemovedFromSim
            {
                this.FlagForRemoval( false, InstancedRendererDeactivationReason.VisLayerLastSimFrameToldToHaveVisualObjectTooFarBack );
                if ( !this.IsConsideredActive )
                    return;
            }
        }

        private bool hasHandledShotHit = false;
        public void HandleVisualsOfShotHit()
        {
            if ( this.hasHandledShotHit )
                return;
            this.hasHandledShotHit = true;
            if ( this.RelatedTypeData == null )
                return;

            switch ( this.EOLType )
            {
                case ShotEOLType.Dissipating: //silently we go into the night...
                    break;
                case ShotEOLType.HitShip:
                    this.PlaySFX_ShotHitting(); //bang!  got you!
                    this.EmitOnAOEParticles( InstancedRendererDeactivationReason.Unknown );
                    //this.EmitOnHitHullParticleEffects(); //this is so small that is' not worth the CPU overhead, frankly.  But this is where it would go.
                    break;
                case ShotEOLType.HitShield:
                    this.PlaySFX_ShotHittingShield(); //bang!  got you!
                    //Debug.Log( "Hit shield! " + ( SquadWithForcefieldWeShot != null ) ); This never gets called!  BADGER_TODO
                    if ( this.SquadWithForcefieldWeShot != null )
                        this.SquadWithForcefieldWeShot.DoHitAgainstMyForcefield( this.CurrentPosition );
                    this.EmitOnAOEParticles( InstancedRendererDeactivationReason.Unknown );
                    break;
            }
        }

        private void PlaySFX_ShotHitting()
        {
            int debugNum = 0;
            try{
                GameEntityTypeData typeData = this.RelatedTypeData;
                if ( typeData == null)
                    return;
                debugNum = 1;
                if ( typeData.SFX_ShotHitting == null )
                    return;
                debugNum = 2;
                if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.SFXMuteShotsHitting ) )
                    return;
                debugNum = 3;
                PresentationLayer_AIW2.Instance.PlaySoundAtUnity3DLocation( SoundPropagation.PlayLocallyOnly, typeData.SFX_ShotHitting, this.CurrentPosition.ToUnityVector3() );
            }
            catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in PlaySFX_ShotHitting. Debug number " + debugNum + "\n" + e, Verbosity.ShowAsError );
            }
        }

        private void PlaySFX_ShotHittingShield()
        {
            if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.SFXMuteShotsHitting ) )
                return;
            PresentationLayer_AIW2.Instance.PlaySoundAtUnity3DLocation( SoundPropagation.PlayLocallyOnly, ArcenSFXOrganizer.ForcefieldAbsorbsShot, this.CurrentPosition.ToUnityVector3() );
        }

        public void EmitOnAOEParticles( InstancedRendererDeactivationReason Reason )
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedEntity.TypeData.InternalName + " CALL EmitOnAOEParticles: HasRenderedAOEAlready " + HasRenderedAOEAlready, Verbosity.DoNotShow );
            if ( this.HasRenderedAOEAlready )
                return;
            this.HasRenderedAOEAlready = true;

            int debugNum = 0;
            try
            {
                bool localDebug = false;
                debugNum = 100;
                GameEntity_Shot shot = this.RelatedEntity;
                GameEntityTypeData typeData = this.RelatedTypeData;
                if ( shot == null || typeData == null )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( " RelatedEntity or TypeData null", Verbosity.DoNotShow );
                    return;
                }
                debugNum = 200;
                int aoe = shot.GetMyAreaOfEffect();
                debugNum = 300;
                if ( aoe <= 0 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( shot.TypeData.InternalName + " GetMyAreaOfEffect less than zero!", Verbosity.DoNotShow );
                    return;
                }

                debugNum = 1000;
                if ( typeData != null )
                {
                    debugNum = 2000;
                    Vector3 locationForEffects;

                    debugNum = 2100;
                    if ( shot.PostExecution_AOESimLocation != ArcenPoint.ZeroZeroPoint && shot.PostExecution_AOESimLocation != ArcenPoint.ZeroZeroPoint )
                    {
                        debugNum = 2200;
                        locationForEffects = shot.PostExecution_AOESimLocation.ToVisualMainGameCoordinates_Numerics( planet );
                    }
                    else
                    {
                        debugNum = 3000;
                        if ( shot.WorldLocation != ArcenPoint.ZeroZeroPoint && shot.WorldLocation != ArcenPoint.ZeroZeroPoint )
                        {
                            debugNum = 3100;
                            locationForEffects = shot.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                        }
                        else
                        {
                            debugNum = 3200;
                            //ArcenDebugging.ArcenDebugLogSingleLine( shot.TypeData.InternalName + "return because no location to drop found", Verbosity.DoNotShow );
                            return;
                        }
                    }

                    debugNum = 4000;
                    //ArcenDebugging.ArcenDebugLogSingleLine( shot.TypeData.InternalName + " drop at location " + locationForEffects, Verbosity.DoNotShow );
                    //UnityEngine.Debug.Log( "aoe" + aoe + "  " + shot.TypeData.InternalName + "  " + Reason + "   " +
                    //    shot.WorldLocation.ToVisualMainGameCoordinates_Numerics() +
                    //    "    " + shot.PostExecution_AOESimLocation.ToVisualMainGameCoordinates_Numerics() );

                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Dropping an effect for shot " + shot.PrimaryKeyID + " " + typeData.InternalName + " at " + locationForEffects, Verbosity.DoNotShow );

                    debugNum = 4100;
                    if ( typeData.SpecialEffectGroup_Death == null )
                    {
                        debugNum = 4200;
                        if ( typeData.IsOkayWithNullAOEEffectForAOEAttack )
                            return;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Null SpecialEffectGroup_Death on " + typeData.InternalName + " so there's no way AOE is going to show up..." +
                        //    " intended url: " + ( typeData.SpecialEffectGroup_AOEOnDeath_path == null ? "nullpath" : typeData.SpecialEffectGroup_AOEOnDeath_path.CombinedPath ), Verbosity.ShowAsError );
                        return;
                    }

                    debugNum = 5000;
                    BattlefieldVisualSingleton.Instance.DropVisualEffectAtSuperGlobal3DPositionWithTTL( typeData.SpecialEffectGroup_Death,
                        locationForEffects.ToUnityVector3(), typeData.VisualEffectTTL_AOE, aoe );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in EmitOnAOEParticles. Debug number " + debugNum + "\n" + e, Verbosity.ShowAsError );
            }
        }

        public void EmitOnHitHullParticleEffects()
        {
            //if ( this.RelatedTypeData != null )
            //    PresentationLayer_AIW2.Instance.DropVisualEffectAt3DSpaceLocationWithTTL( this.RelatedTypeData.SpecialEffectGroup_IAmAShotAndHitSomething,
            //        this.CurrentPosition, this.RelatedTypeData.VisualEffectTTL_IAmAShotAndHitSomething );
        }

        public bool DidMainUpdate = false;
        public void DoMainUpdate( BattlefieldVisualSingleton MainVis, bool DrawDebugData, bool DrawRadiiAtAllTimes )
        {
            this.DidMainUpdate = false;
            if ( !this.IsConsideredActive )
                return;

            this.DidMainUpdate = true;

            GameEntity_Shot relatedEntOrNull = this.RelatedEntity;

            if ( relatedEntOrNull != null )
            {
                try
                {
                    if (this.ShotCurrentTargetSquad?.RelatedEntity.GetSquad() != null)
                    {
                        //if these shots are going to a target on another planet, drop them
                        // if(Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() != this.ShotCurrentTargetSquad.RelatedEntity.Planet)
                        //     this.FlagForRemoval(false, InstancedRendererDeactivationReason.GoingBackToPool);
                    }
                    this.Fallback_LastTargetPoint = relatedEntOrNull.TargetInitialLocation;
                    this.Fallback_LastOwnType = this.RelatedTypeData;
                    if ( relatedEntOrNull.PostExecution_EmitOnAOECount > 0 )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine( relatedEntOrNull.TypeData.InternalName + " CALL PostExecution_EmitOnAOECount: " + RelatedEntity.PostExecution_EmitOnAOECount, Verbosity.DoNotShow );
                        this.EmitOnAOEParticles( InstancedRendererDeactivationReason.Unknown );
                        relatedEntOrNull.PostExecution_EmitOnAOECount = 0;
                    }
                }
                catch //this can hapen due to threading competition
                {
                    this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.VisLayerRemovingSilentlyDueToException, true );
                    this.DidMainUpdate = false;
                }
            }

            //if ( DrawRadiiAtAllTimes )
            //{
            //    if ( this.RelatedTypeData.RadiusForDisplay <= 0 )
            //        this.RelatedTypeData.RadiusForDisplay = this.RelatedTypeData.Radius /
            //            ExternalVisualConstants.Instance.CombatVisualScaleDivisor;

            //    FrontEndLink.Instance.IMDraw_WireDisc3D( this.CurrentPosition, Mat.Quater_Ident, this.RelatedTypeData.RadiusForDisplay, IMDrawAxis.Y, Color.yellow );
            //}

            //if ( DrawDebugData && this.DebuggingLines.Count > 0 )
            //{
            //    DebuggingLine line;
            //    for ( int i = 0; i < DebuggingLines.Count; i++ )
            //    {
            //        line = DebuggingLines[i];
            //        FrontEndLink.Instance.IMDrawLine3D( line.Source, line.Dest, line.SourceColor, line.DestColor );
            //    }
            //    this.DebuggingLines.Clear();
            //}
        }

        public int ShotMovement_LastAnimationCycle = -1;
        public float ShotMovement_AccumulatedDeltaTime = 0;

        #region DoShotMovement
        private Vector3 oldLocation;
        private bool isOldLocationSet = false;
        private float currentSpeed = 0f;
        private float currentDistanceTraveled = 0f;
        private float currentDistanceTraveledSinceLastRotationSet = 0f;
        private const float SPEED_DAMPER_UNDER_DISTANCE = 50f;
        private float topmostScale = 1f;
        private float trailZScale = 1f;

        public void DoShotMovement( bool DrawDebugData, float deltaTime )
        {
            int debugStage = 0;
            try
            {
                bool localDebug = false;
                if ( this.DeathCounterInevitable_Enabled )
                {
                    this.DeathCounterInevitable_TimeLeft -= this.ShotMovement_AccumulatedDeltaTime;
                    if ( this.DeathCounterInevitable_TimeLeft <= 0 )
                        return;
                }
                
                debugStage = 50;
                GameEntity_Shot shot = this.RelatedEntity;
                
                debugStage = 100;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + shot?.PrimaryKeyID + " is in display mode " + this.ShotDisplay + " at start of shot movement. Elapsed time: " + this.ShotMovement_AccumulatedDeltaTime, Verbosity.DoNotShow );

                ShotRenderManagerGroup myPoolOrNull = this.Pool;

                debugStage = 200;

                Vector3 newLoc = this.CurrentPosition;
                //this prevents the Invalid AABB aabb error, which happens if the shot goes way out of bounds for whatever reason these are doing that.
                if ( newLoc.X > 100000 || newLoc.X < -100000 )
                {
                    //ArcenDebugging.ArcenDebugLog( "Invalid pos of " + newLoc + " on shot!" + this.RelatedTypeData.InternalName +  " " + 
                    //    shot?.PrimaryKeyID + " " + shot?.IsConsideredActive, Verbosity.ShowAsError );
                    this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.VisAndSimEntityMismatch, true );
                    return;
                }
                
                debugStage = 300;

                if ( !this.isOldLocationSet )
                {
                    this.oldLocation = newLoc;
                    this.currentDistanceTraveled = 0f;
                }
                this.isOldLocationSet = true;
                debugStage = 400;

                float distance = ( newLoc - oldLocation ).Length();
                if ( distance > 0 ) //only do these parts if we moved!
                {
                    this.currentDistanceTraveled += distance;
                    this.currentDistanceTraveledSinceLastRotationSet += distance;
                    debugStage = 500;
                    if ( this.currentDistanceTraveledSinceLastRotationSet > (this.currentRotationIsSet ? 30f : 0.01f) && distance > 0.01f )
                    {
                        debugStage = 600;
                        UnityEngine.Vector3 direction = (newLoc - this.oldLocation).ToUnityVector3();
                        if ( direction != UnityEngine.Vector3.zero )
                        {
                            this.CurrentRotation = UnityEngine.Quaternion.LookRotation( direction );

                            this.currentRotationIsSet = true;
                            this.currentDistanceTraveledSinceLastRotationSet = 0;
                        }
                    }
                }
                debugStage = 700;
                //this needs to be done even if the game is paused
                float cameraDist = ( BattlefieldVisualSingleton.Instance.MainCameraPos_Numerics - newLoc ).LengthSquared();
                topmostScale = 1f + (float)( cameraDist < 150 ? 0 : ( cameraDist - 150 ) * 0.00003f );// ArcenVisualOrganizer.Instance.WorkingDialFloat );
                if ( topmostScale > 5 )
                    topmostScale = 5;
                debugStage = 800;

                if ( shot != null )
                {
                    GameEntityTypeData typeDataForShot = shot.TypeData;
                    if ( typeDataForShot != null && typeDataForShot.ForMark != null )
                    {
                        GameEntityTypeData.MarkLevelStats dataForMark = typeDataForShot.ForMark[0];

                        Planet planet = shot.Planet;

                        float scale = dataForMark.VisualsScaleMultiplier *
                            (planet == null ? PlanetGravWellSize.Base_VisualShipScaleMultiplier : planet.GravWellSize.VisualShipScaleMultiplier) * (myPoolOrNull == null ? 1f : myPoolOrNull.PrototypeScale );
                        //ArcenDebugging.ArcenDebugLogSingleLine( "topmostScale: " + topmostScale + " scale: " + scale, Verbosity.DoNotShow );
                        if ( topmostScale >= 0 && scale > 0 )
                            topmostScale *= scale;
                    }
                }

                debugStage = 840;

                this.oldLocation = newLoc;
                
                debugStage = 900;
                //only update the trail size if we're actually moving!
                if ( myPoolOrNull != null && myPoolOrNull.HasTrail && deltaTime > 0 )
                {
                    debugStage = 1000;
                    float newSpeed = distance / deltaTime;
                    //make engines cut on more slowly over the first bit of time.
                    if ( this.currentDistanceTraveled < SPEED_DAMPER_UNDER_DISTANCE )
                        newSpeed *= ( this.currentDistanceTraveled / SPEED_DAMPER_UNDER_DISTANCE );

                    debugStage = 1100;
                    //if ( newSpeed > this.currentSpeed ) //never let shots get smaller.  They never slow down for these purposes
                    {
                        this.currentSpeed = Mathf.Lerp( this.currentSpeed, newSpeed, 0.1f );
                        float speedModifier = currentSpeed / 90f;
                        if ( speedModifier > 20 )
                            speedModifier = 20f;

                        debugStage = 1200;
                        this.trailZScale = 1 + speedModifier;
                    }
                }

                debugStage = 2000;
                //If we are hidden now, try to update our targeting information so we can start lerping the shot
                if ( this.ShotDisplay == ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp )
                    this.TryWiringUpShotToTarget( this.ShotMovement_AccumulatedDeltaTime );

                debugStage = 3000;
                switch ( this.ShotDisplay )
                {
                    case ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp: //no change, so just make sure we're invisible
                        debugStage = 3100;
                        this.ShouldRenderThisFrame = false;
                        break;
                    case ShotDisplayStyle.LerpingBetweenEquivalentPoints:
                        debugStage = 3200;
                        this.ShouldRenderThisFrame = true;
                        this.UpdateShotViaLerpToCurrentTarget( this.ShotMovement_AccumulatedDeltaTime, DrawDebugData );
                        break;
                    case ShotDisplayStyle.HeadingToSpecificLocationAcrossLastFewFrames:
                    case ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames:
                        debugStage = 3300;
                        this.ShouldRenderThisFrame = true;
                        this.UpdateShotViaLerpToFinalLocation( this.ShotMovement_AccumulatedDeltaTime, DrawDebugData );
                        break;
                    case ShotDisplayStyle.KeepCurrentPositionWhileDying:
                        debugStage = 3400;
                        //do nothing!
                        if ( DrawDebugData && this.RelatedTypeData != null )
                            FrontEndLink.Instance.IMDraw_WireDisc3D( this.CurrentPosition.ToUnityVector3(), Mat.Quater_Ident, this.RelatedTypeData.BaseMark.CalculateRadiusForDisplay( planet ), IMDrawAxis.Y, Color.cyan );
                        break;
                    case ShotDisplayStyle.TrueLocationByDesign:
                    case ShotDisplayStyle.TrueLocationByFailure:
                        debugStage = 3500;
                        this.ShouldRenderThisFrame = true;
                        this.UpdateShotViaTrueLocationDisplay( this.ShotMovement_AccumulatedDeltaTime, DrawDebugData );
                        break;
                    case ShotDisplayStyle.HiddenByTimeout:
                        debugStage = 3600;
                        //If the shot has timed out then it never found a valid target at all (perhaps the target died
                        // before it could be visually displayed?)
                        this.ShouldRenderThisFrame = false;
                        break;
                    default:
                        debugStage = 3700;
                        ArcenDebugging.ArcenDebugLog( "No root switch entry found for " + this.ShotDisplay + " Shot Display Type!", Verbosity.ShowAsError );
                        this.ShotDisplay = ShotDisplayStyle.TrueLocationByFailure;
                        break;

                }

                debugStage = 4000;
                //if ( this.CurrentPosition.X < -20000 || this.CurrentPosition.Y < -20000 || this.CurrentPosition.Z < -20000 )
                //{
                //    UnityEngine.Debug.LogError( "Invalid CurrentPosition " + this.CurrentPosition + "  ShotDisplay: " + this.ShotDisplay + " ShotTargetLoc: " + this.ShotTargetLoc + " targetLoc: " + this.targetLoc );
                //    this.CurrentPosition = newLoc = Mat.V3N_One;
                //}

                if ( localDebug && shot != null )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + shot.PrimaryKeyID + " is in display mode " + this.ShotDisplay + " at end of shot movement", Verbosity.DoNotShow );

                debugStage = 9000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "DoShotMovement: Error at debug stage " + debugStage +
                                              "\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public void RenderShot()
        {
            if ( this.ShouldRenderThisFrame )
            {
                ShotRenderManagerGroup myPoolOrNull = this.Pool;
                if ( myPoolOrNull != null )
                    myPoolOrNull.WriteToDrawBufferForOneFrame( this.CurrentPosition, this.CurrentRotation, this.topmostScale, this.trailZScale );
            }
        }        

        private void SetLocationBasedOnComplicatedStuff()
        {
            bool newLocDebug = false;
            bool zerozeroDebug = false;
            bool localDebug = false;
            GameEntity_Shot myself = this.RelatedEntity;
            if ( myself == null || myself.HasBeenRemovedFromSim )
                return;

            ArcenPoint simLoc = myself.WorldLocation;
            if ( simLoc == ArcenPoint.OutOfRange || simLoc == ArcenPoint.ZeroZeroPoint || simLoc == this.PriorLoc )
                return;

            if ( this.PriorLoc == ArcenPoint.OutOfRange )
            {
                //this show just came into being (perhaps you tabbed to this planet while the shot was moving?)
                //So let's assume it started at the current requested location (to prevent any weirdness with the shot trails in particular)
                this.CurrentPosition = simLoc.ToVisualMainGameCoordinates_Numerics( planet );
                this.PriorLoc = simLoc;
                this.ShotShipEmissionPoint = simLoc.ToVisualMainGameCoordinates_Numerics( planet ); //we've lost track of where the shot was initially fired from, so assume it was fired from here
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "new location " + simLoc.ToVisualMainGameCoordinates_Numerics( planet ) + " requested for " + this.RelatedTypeData.InternalName   + " " + myself.PrimaryKeyID + " CurrentPosition being forcibly updated to: " + this.CurrentPosition, Verbosity.DoNotShow );
            }

            SquadVisualizer squadWithFF = this.SquadWithForcefieldWeShot;

            //A new location is requested whenever the Sim touches base with the Vis code (via SetLocation())
            //to say "Hey, you're here now, just so you know"
            if ( squadWithFF != null )
                this.ShotCurrentTargetPoint = squadWithFF.GetCloserPointOnForceFieldIfPossible( this.CurrentPosition );
            else
            {
                ShipVisualizer targetShip = this.ShotCurrentTargetShip;
                if ( targetShip != null )
                {
                    if ( targetShip.IsStillInGame )
                        this.ShotCurrentTargetPoint = targetShip.GetCurrentEmissionHitPoint();
                    else
                        this.TryRewiringShotToNewShipAtSameTargetSquad( null );
                    if ( this.ShotCurrentTargetSquad == null )
                        this.ShotCurrentTargetSquad = targetShip.CurrentSquad;
                }
                SquadVisualizer targetSquad = this.ShotCurrentTargetSquad;
                if ( targetShip == null && targetSquad != null )
                {
                    this.TryRewiringShotToNewShipAtSameTargetSquad( null );
                    if ( targetShip == null )
                        this.ShotCurrentTargetPoint = targetSquad.GetCurrentPositionWithEmissionOffset();
                }
            }
            this.TryUpdatingTargetPoint();
            this.ShotSquadCurrentFullDist = ( this.ShotOriginGameStylePoint - this.ShotSquadTargetGameStylePoint ).Length();

            if ( this.ShotSquadCurrentFullDist <= 0 )
            {
                this.ShotDisplay = ShotDisplayStyle.KeepCurrentPositionWhileDying;
                return;
            }
            //DesiredLoc is the current location of the shot
            Vector3 newLocInVisCoordinates = simLoc.ToVisualMainGameCoordinates_Numerics( planet );
            float currentDist = ( newLocInVisCoordinates - this.ShotSquadTargetGameStylePoint ).Length();

            float newPercentage = 1f - Mathf.Clamp01( currentDist / this.ShotSquadCurrentFullDist );
            if ( newPercentage > this.ShotPercentageBetweenEmissAndTarget ) //no going backwards!
                this.ShotPercentageBetweenEmissAndTarget = 1f - Mathf.Clamp01( currentDist / this.ShotSquadCurrentFullDist );
            
            this.ShotTargetLoc = Vector3.Lerp( this.ShotShipEmissionPoint, this.ShotCurrentTargetPoint, this.ShotPercentageBetweenEmissAndTarget );
            float speedModifier = -1;
            float dist = ( this.ShotTargetLoc - this.CurrentPosition ).Length();
            if ( dist > 1 )
                speedModifier = dist * 0.05f;
            float newSpeed = speedModifier;
            //to try to smoothe the movement out, update the new speed to be
            //a weighted average of the desired movement speed and the previous speed
            if ( this.movementSpeed > 0 )
                newSpeed = ( speedModifier*2 + this.movementSpeed )/3;
            if ( localDebug || newLocDebug || zerozeroDebug )
            {
                string target = "unknown";
                if ( this.ShotCurrentTargetSquad != null )
                    target = this.ShotCurrentTargetSquad.RelatedTypeData.InternalName;
                if ( this.ShotCurrentTargetShip == null )
                    target += " (unknown ship)";
                else
                    target += this.ShotCurrentTargetShip;
                if ( this.CurrentPosition == Mat.V3N_Zero && zerozeroDebug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + myself.PrimaryKeyID + " lerping to current target " + this.ShotCurrentTargetPoint + " Time remaining: " +this.DeathCounterInevitable_TimeLeft + " accumulated time " + this.accumulatedLifespanDeltaTime + " previous speed speed " + this.movementSpeed + " new speed " + newSpeed + " dist " + dist + " new location requested; the current location is " + this.CurrentPosition + " --> " + simLoc.ToVisualMainGameCoordinates_Numerics( planet ) + " target is " + target + ". Shot is at ZEROZERO", Verbosity.DoNotShow );
                }
                else if ( newLocDebug || localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + myself.PrimaryKeyID + " lerping to current target " + this.ShotCurrentTargetPoint + " Time remaining: " +this.DeathCounterInevitable_TimeLeft + " accumulated time " + this.accumulatedLifespanDeltaTime + " previous speed speed " + this.movementSpeed + " new speed " + newSpeed + " dist " + dist + " new location requested; the current location is " + this.CurrentPosition + " --> " + simLoc.ToVisualMainGameCoordinates_Numerics( planet ) + " target is " + target, Verbosity.DoNotShow );
            }

            this.movementSpeed = newSpeed;
        }

        #region UpdateShotViaLerpToCurrentTarget
        private void UpdateShotViaLerpToCurrentTarget( float DeltaTime, bool DrawDebugData )
        {
            bool localDebug = false;
            this.accumulatedLifespanDeltaTime += DeltaTime;
            GameEntity_Shot myself = this.RelatedEntity;
            //If we are in LerpingBetweenEquivalentPoints, this is while a shot is travelling
            //from the shooter to the target. Make sure enough time has passed before we start
            //showing ourselves so we get a nice staggered set of shots
            if (this.accumulatedLifespanDeltaTime < this.AccumulatedTimeBeforeStartingDisplay)
            {
                if (localDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine(this.RelatedTypeData.InternalName + " " + myself?.PrimaryKeyID + " is not going to show until " + this.AccumulatedTimeBeforeStartingDisplay + " steps have elapsed. Currently at " + this.accumulatedLifespanDeltaTime, Verbosity.DoNotShow );
                return;
            }

            this.SetLocationBasedOnComplicatedStuff();

            Vector3 previousLocation = this.CurrentPosition;
            if ( this.movementSpeed > 0 )
            {
                Vector3 globalPos = this.CurrentPosition;
                Vector3 newLoc = Vector3.Lerp( globalPos, this.ShotTargetLoc, this.movementSpeed * DeltaTime );
                if ( Mat.Abs( newLoc.X - globalPos.X ) < 0.01 && Mat.Abs( newLoc.Z - globalPos.Z ) < 0.01 && Mat.Abs( newLoc.Y - globalPos.Y ) < 0.01 )
                {
                    this.movementSpeed = -1;
                    this.CurrentPosition = newLoc;
                }
                else
                    this.CurrentPosition = newLoc;
            }
            
            if ( this.otherAddonObject != null )
            {
                float yoffset = RelatedTypeData.AddonObjectOrParticleFieldYOffset;
                UnityEngine.Vector3 offsetPos = this.CurrentPosition.ToUnityVector3() + new UnityEngine.Vector3( 0, yoffset, 0 );
                this.otherAddonObject.SetPosIfNeeded( offsetPos );
                this.otherAddonObject.AdvanceTime(DeltaTime);
            }
            
            if(localDebug)
            {
                ArcenDebugging.ArcenDebugLogSingleLine(this.RelatedTypeData.InternalName   + " " + myself?.PrimaryKeyID + " lerping to current target " + this.ShotCurrentTargetPoint + " shot location " + this.CurrentPosition + " accumulated time " + this.accumulatedLifespanDeltaTime + " speed " + this.movementSpeed + " previous location " + previousLocation, Verbosity.DoNotShow );
            }

            if ( DrawDebugData )
            {
                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), this.ShotCurrentTargetPoint.ToUnityVector3(), Color.gray, Color.green ) );
                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), this.ShotTargetLoc.ToUnityVector3(), Color.white, Color.yellow ) );
                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), this.ShotShipEmissionPoint.ToUnityVector3(), Color.gray, Color.blue ) );
                if ( myself != null )
                    this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), myself.WorldLocation.ToVisualMainGameCoordinates_Unity( planet ), Color.white, Color.magenta ) );

                FrontEndLink.Instance.IMDraw_WireDisc3D( this.CurrentPosition.ToUnityVector3(), Mat.Quater_Ident, this.RelatedTypeData.BaseMark.CalculateRadiusForDisplay( planet ), IMDrawAxis.Y, Color.yellow );
            }
        }
        #endregion

        #region UpdateShotViaTrueLocationDisplay
        private void UpdateShotViaTrueLocationDisplay( float DeltaTime, bool DrawDebugData )
        {
            this.accumulatedLifespanDeltaTime += DeltaTime;
            this.SetLocationToMatchSim();

            if ( this.movementSpeed > 0 )
            {
                Vector3 localPos = this.CurrentPosition;
                Vector3 newLoc = Vector3.Lerp( localPos, this.targetLoc, this.movementSpeed * DeltaTime );
                if ( Mat.Abs( newLoc.X - localPos.X ) < 0.01 && Mat.Abs( newLoc.Z - localPos.Z ) < 0.01 )
                    this.movementSpeed = -1;
                else
                    this.CurrentPosition = newLoc;
            }

            if ( DrawDebugData )
            {
                FrontEndLink.Instance.IMDraw_WireDisc3D( this.CurrentPosition.ToUnityVector3(), Mat.Quater_Ident, this.RelatedTypeData.BaseMark.CalculateRadiusForDisplay( planet ), IMDrawAxis.Y, Color.blue );
            }
        }
        #endregion

        #region UpdateShotViaLerpToFinalLocation
        private void UpdateShotViaLerpToFinalLocation( float DeltaTime, bool DrawDebugData )
        {
            bool localDebug = false;
            switch ( this.ShotDisplay )
            {
                case ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames:
                    if ( this.SquadWithForcefieldWeShot != null)
                        this.ShotCurrentTargetPoint = this.SquadWithForcefieldWeShot.GetCloserPointOnForceFieldIfPossible( this.CurrentPosition );
                    else if ( this.ShotCurrentTargetShip != null )
                    {
                        if ( this.ShotCurrentTargetShip.IsStillInGame )
                            this.ShotCurrentTargetPoint = this.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                        else
                            this.ShotDisplay = ShotDisplayStyle.HeadingToSpecificLocationAcrossLastFewFrames;
                    }
                    else
                        this.ShotDisplay = ShotDisplayStyle.HeadingToSpecificLocationAcrossLastFewFrames;
                    break;
            }
            this.accumulatedLifespanDeltaTime += DeltaTime;
            this.startingLocation = this.ShotShipEmissionPoint;

            if ( this.accumulatedLifespanDeltaTime > 0 )
            {
                float speed = -1;
                float dist = ( this.CurrentPosition - this.startingLocation ).Length();
                if ( dist > 1 )
                    speed = dist * 0.05f;
//                 if ( dist > 0 )
//                     speed = dist / this.accumulatedLifespanDeltaTime;
                else
                    speed = 5f;

                if ( speed > this.movementSpeed )
                    this.movementSpeed = speed;

//                this.accumulatedLifespanDeltaTime = 0f;
            }
  
            this.finalApproachProgress += ( this.movementSpeed * DeltaTime );
            if ( this.finalApproachProgress > 1 )
                this.finalApproachProgress = 1;

            GameEntity_Shot myself = this.RelatedEntity;

            Vector3 newLoc = Vector3.Lerp( this.ShotStartingPointForFinalApproach, this.ShotCurrentTargetPoint, this.finalApproachProgress );
            if (localDebug)
                ArcenDebugging.ArcenDebugLogSingleLine(this.RelatedTypeData.InternalName   + " " + myself?.PrimaryKeyID + " lerping to final destination " + this.ShotCurrentTargetPoint + " Time remaining: " +this.DeathCounterInevitable_TimeLeft + " accumulated time " + this.accumulatedLifespanDeltaTime + " speed " + this.movementSpeed + " current location: " + newLoc + " destination " + this.ShotCurrentTargetPoint, Verbosity.DoNotShow );

            if ( this.finalApproachProgress >= 1f ) //From Chris: we don't care about the Y axis because that is an approximation anyhow.
            {
                if(localDebug)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine(this.RelatedTypeData.InternalName   + " " + myself?.PrimaryKeyID + " has landed a hit!", Verbosity.DoNotShow );
                }
                this.movementSpeed = -1;
                this.CurrentPosition = newLoc;
                this.ShotDisplay = ShotDisplayStyle.KeepCurrentPositionWhileDying;
                this.HandleVisualsOfShotHit();
                this.DeathCounterInevitable_TimeLeft = 0f;
                this.FlagForRemoval( true, InstancedRendererDeactivationReason.VisLayerFinalApproachProgressComplete );
                if ( this.ShotCurrentTargetShip != null )
                {
                    this.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie--;
                    if ( this.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie <= 0 )
                        this.ShotCurrentTargetShip.DoDeathEffects();
                }
                return;
            }
            else
                this.CurrentPosition = newLoc;

            if ( DrawDebugData )
            {
                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), this.ShotCurrentTargetPoint.ToUnityVector3(), Color.gray, Color.green ) );
//                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition, this.ShotTargetLoc, Color.white, Color.yellow ) );
                this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), this.ShotShipEmissionPoint.ToUnityVector3(), Color.gray, Color.blue ) );
                if ( myself != null )
                    this.DebuggingLines.Add( DebuggingLine.Create( this.CurrentPosition.ToUnityVector3(), myself.WorldLocation.ToVisualMainGameCoordinates_Unity( planet ), Color.white, Color.magenta ) );
                if(this.RelatedTypeData != null)
                    FrontEndLink.Instance.IMDraw_WireDisc3D( this.CurrentPosition.ToUnityVector3(), Mat.Quater_Ident, this.RelatedTypeData.BaseMark.CalculateRadiusForDisplay( planet ), IMDrawAxis.Y,
                                                             this.ShotDisplay == ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames ? Color.green : Color.red );
            }
        }
        #endregion

        #region AddShot
        private void AddShot( GameEntity_Squad TargetSquad )
        {
            //If TargetSquad is null then we are calling AddShot from Activate()
            this.ShotDisplay = ShotDisplayStyle.TrueLocationByDesign;
            if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.Debug_DrawTrueLocationOfShotsInsteadOfLerps ) )
                return;

            this.ShotDisplay = ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp;
            this.TryWiringUpShotToTarget( 0f, TargetSquad);
        }
        #endregion

        #region TryWiringUpShotToTarget
        private void TryWiringUpShotToTarget( float DeltaTime, GameEntity_Squad TargetSquad = null)
        {
            int debugStage = 1;
            try
            {
                bool localDebug = false;
                int debugPathID = -1;
                if ( this.ShotDisplay != ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + this.RelatedEntity?.PrimaryKeyID + " TryWiringUpShotToTarget early exit (shot already has information)", Verbosity.DoNotShow );
                    return;
                }
                debugStage = 100;

                GameEntity_Shot relatedEnt = this.RelatedEntity;
                if ( relatedEnt == null  )
                {
                    this.ShotDisplay = ShotDisplayStyle.TrueLocationByFailure;
                    //ArcenDebugging.ArcenDebugLog( "RelatedEntity null on shot during TryWiringUpShotToTarget!", Verbosity.ShowAsError );
                    return;
                }
                debugStage = 200;
                EntitySystem originSystemOrNull = relatedEnt.OriginOrNull;
                if ( originSystemOrNull == null )
                {
                    this.ShotDisplay = ShotDisplayStyle.TrueLocationByFailure;
                    return;
                }
                debugStage = 300;

                GameEntity_Squad originEntityOrNull = originSystemOrNull == null ? null : originSystemOrNull.ParentEntity;
                debugStage = 400;

                this.TimeWaitingForShotData += DeltaTime;
                if ( this.TimeWaitingForShotData > 3f )
                {
                    this.ShotDisplay = ShotDisplayStyle.HiddenByTimeout;
                    //GameEntity_Squad targetSquad = trelatedEnt.Target;
                    //if ( targetSquad == null )
                    //{
                    //    ArcenDebugging.ArcenDebugLog( "TrueLocationByTimeout triggered by targetSquad null during TryWiringUpShotToTarget!", this , Verbosity.ShowAsError );
                    //    return;
                    //}
                    //ArcenDebugging.ArcenDebugLog( "TrueLocationByTimeout triggered by ??? during TryWiringUpShotToTarget!", this , Verbosity.ShowAsError );
                    return;
                }
                debugStage = 500;

                GameEntity_Squad localTargetSquad = null;
                if ( TargetSquad != null )
                    localTargetSquad = TargetSquad;
                else if ( this.ShotCurrentTargetSquad != null )
                    localTargetSquad = this.ShotCurrentTargetSquad.RelatedEntity.GetSquad();
                else if ( relatedEnt != null )
                    localTargetSquad = relatedEnt.Target.GetSquad();
                
                debugStage = 600;
                SquadVisualizer mySquadVisOrNull = originEntityOrNull == null ? null : ( SquadVisualizer)originEntityOrNull.InstancedRenderer;
                debugStage = 700;
                SquadVisualizer targetSquadVisOrNull;
                if ( localTargetSquad == null )
                    targetSquadVisOrNull = null;
                else
                    targetSquadVisOrNull = (SquadVisualizer)localTargetSquad.InstancedRenderer;
                debugStage = 800;

                ShipVisualizer shipToFireFromOrNull = null;
                if ( mySquadVisOrNull != null )
                    shipToFireFromOrNull = mySquadVisOrNull.GetRandomShipThatCanFire();
                debugStage = 900;
                ShipVisualizer targetToFireOnOrNull = null;
                if ( targetSquadVisOrNull != null )
                {
                    potentialShipTargetsList.Clear();
                    debugStage = 1000;
                    targetSquadVisOrNull.GetRandomShipsThatCanBeFiredUpon( 1, potentialShipTargetsList, null, null );
                    if ( potentialShipTargetsList.Count != 0 )
                    {
                        debugStage = 1100;
                        targetToFireOnOrNull = potentialShipTargetsList[0];
                    }
                }

                debugStage = 1200;
                this.ShotOriginShip = shipToFireFromOrNull;
                if ( shipToFireFromOrNull != null )
                {
                    debugStage = 1300;
                    this.ShotShipEmissionPoint = shipToFireFromOrNull.GetCurrentEmissionHitPoint();
                    shipToFireFromOrNull.TimeUntilIIdeallyFireAgain = 10f;
                }
                else //null ship firing from!
                {
                    debugStage = 1400;
                    if ( mySquadVisOrNull != null )
                        this.ShotShipEmissionPoint = mySquadVisOrNull.GetCurrentPositionWithEmissionOffset();
                    else
                    {
                        debugStage = 1500;
                        if ( originEntityOrNull != null )
                            this.ShotShipEmissionPoint = originEntityOrNull.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                        else
                            this.ShotShipEmissionPoint = relatedEnt.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                    }
                }

                this.CurrentPosition = this.ShotShipEmissionPoint; //jump to this location!!

                debugStage = 1600;
                if ( originEntityOrNull != null )
                    this.ShotOriginGameStylePoint = originEntityOrNull.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                else
                    this.ShotOriginGameStylePoint = relatedEnt.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );

                debugStage = 1700;
                if ( localTargetSquad == null && this.SquadWithForcefieldWeShot == null )
                {
                    debugStage = 1800;
                    this.ShotSquadTargetGameStylePoint = this.Fallback_LastTargetPoint.ToVisualMainGameCoordinates_Numerics( planet );
                }
                else if ( localTargetSquad == null && this.SquadWithForcefieldWeShot != null && this.SquadWithForcefieldWeShot.RelatedEntity.GetSquad() != null )
                {
                    debugStage = 1900;
                    this.ShotSquadTargetGameStylePoint = this.SquadWithForcefieldWeShot.RelatedEntity.GetSquad().WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                }
                else
                {
                    debugStage = 2000;
                    this.ShotSquadTargetGameStylePoint = localTargetSquad.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                }
                debugStage = 2100;
                this.ShotSquadOriginalDist = ( this.ShotOriginGameStylePoint - this.ShotSquadTargetGameStylePoint ).Length();
                this.accumulatedLifespanDeltaTime = 0f;
                this.startingLocation = this.ShotShipEmissionPoint;

                this.ShotCurrentTargetSquad = targetSquadVisOrNull;
                this.ShotCurrentTargetShip = targetToFireOnOrNull;
                debugStage = 2200;
                if ( targetToFireOnOrNull != null )
                {
                    if ( this.SquadWithForcefieldWeShot != null )
                    {
                        debugStage = 2300;
                        //hit a force field
                        targetToFireOnOrNull.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon += 0.5f;
                        this.ShotCurrentTargetPoint = this.SquadWithForcefieldWeShot.GetCloserPointOnForceFieldIfPossible( this.CurrentPosition );
                        debugPathID = 1;
                    }
                    else
                    {
                        debugStage = 2400;
                        targetToFireOnOrNull.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon += 0.5f;
                        this.ShotCurrentTargetPoint = targetToFireOnOrNull.GetCurrentEmissionHitPoint();
                        debugPathID = 2;
                    }
                }
                else
                {
                    debugStage = 4000;
                    if ( this.SquadWithForcefieldWeShot != null )
                    {
                        debugStage = 4100;
                        this.ShotCurrentTargetPoint = this.SquadWithForcefieldWeShot.CurrentPosition;
                        debugPathID = 3;
                    }
                    else if ( targetSquadVisOrNull != null )
                    {
                        debugStage = 4200;
                        this.ShotCurrentTargetPoint = targetSquadVisOrNull.CurrentPosition;
                        debugPathID = 4;
                    }
                    else if ( localTargetSquad != null )
                    {
                        debugStage = 4300;
                        this.ShotCurrentTargetPoint = localTargetSquad.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                        debugPathID = 5;
                    }
                    else
                    {
                        debugStage = 4400;
                        this.ShotCurrentTargetPoint = this.Fallback_LastTargetPoint.ToVisualMainGameCoordinates_Numerics( planet );
                        debugPathID = 6;
                    }
                }
                debugStage = 5000;
                this.ShotTargetLoc = this.ShotCurrentTargetPoint;
                //I think "DesiredLoc" is where the shot should be, not where the target is
                //            this.DesiredLoc.X = (int)this.ShotCurrentTargetPoint.x;
                //            this.DesiredLoc.Y = (int)this.ShotCurrentTargetPoint.y;

                //if ( Mat.Abs( this.ShotCurrentTargetPoint.x - this.ShotShipEmissionPoint.x ) >= 1 || Mat.Abs( this.ShotCurrentTargetPoint.z - this.ShotShipEmissionPoint.z ) >= 1 )
                //    this.selfT.LookAt( this.ShotCurrentTargetPoint );
                //AddShot --> TryWiringUpShotToTarget is callled in Activate() with a null TargetSquad
                //if called from that path, don't display the shot yet
                debugStage = 5100;
                if ( localTargetSquad == null && this.Fallback_LastTargetPoint == ArcenPoint.ZeroZeroPoint )
                    this.ShotDisplay = ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp;
                else
                    this.ShotDisplay = ShotDisplayStyle.LerpingBetweenEquivalentPoints;

                debugStage = 5200;
                this.AccumulatedTimeBeforeStartingDisplay = this.delayMultiplier * (float)( Engine_Universal.PermanentQualityRandom.Next( this.minDelay, this.maxDelay ) );
                debugStage = 5300;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.RelatedTypeData.InternalName   + " " + relatedEnt.PrimaryKeyID + " TryWiringUpShotToTarget: Heading toward " + this.ShotCurrentTargetPoint + " code path " + debugPathID + " Resulting shotdisplay: " +this.ShotDisplay + " delay before showing: " + this.AccumulatedTimeBeforeStartingDisplay, Verbosity.DoNotShow );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "TryWiringUpShotToTarget Error at debug stage " + debugStage +
                    "\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region GetAdditionalShipsHitByMultiShot
        private void GetAdditionalShipsHitByMultiShot( GameEntity_Squad targetSquad, List<ShipVisualizer> shipsTakingHits, int shipsHit, ShipVisualizer shipToExcludeOrNull )
        {
            shipsTakingHits.Clear();
            if ( targetSquad == null )
            {
                return;
            }

            SquadVisualizer targetVis = (SquadVisualizer)targetSquad.InstancedRenderer;
            if ( targetVis == null )
            {
                return;
            }
            this.potentialShipTargetsList.Clear();
            targetVis.GetRandomShipsThatCanBeFiredUpon(shipsHit, potentialShipTargetsList, null, shipToExcludeOrNull );
            for(int i = 0; i < potentialShipTargetsList.Count; i++)
                shipsTakingHits.Add(potentialShipTargetsList[i]);
            //BADGER TODO: I may need to tweak the TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon value
            //for the hit ships
        }
        #endregion

        #region TryRewiringShotToNewShipAtSameTargetSquad
        private void TryRewiringShotToNewShipAtSameTargetSquad(List<ShipVisualizer> shipsToExcludeOrNull )
        {
            //Find another ship in this squad that can take an incoming shot (the
            //original target has died). This uses a similar logic to GetAdditionalShipsHitByMultiShot
            //and we may want to combine them eventually
            SquadVisualizer vis = this.ShotCurrentTargetSquad;
            if ( vis == null )
                return;
            GameEntity_Squad targetSquad = vis.RelatedEntity.GetSquad();
            if ( targetSquad == null )
                return;

            SquadVisualizer targetVis = (SquadVisualizer)targetSquad.InstancedRenderer;
            if ( targetVis == null )
                return;

            potentialShipTargetsList.Clear();
            targetVis.GetRandomShipsThatCanBeFiredUpon(1, potentialShipTargetsList, shipsToExcludeOrNull, null );
            if ( potentialShipTargetsList.Count == 0 || potentialShipTargetsList[0] == null)
            {
                this.ShotCurrentTargetPoint = targetVis.CurrentPosition;
                return;
            }
            ShipVisualizer targetShip = potentialShipTargetsList[0];
            if ( targetShip == null )
                this.ShotCurrentTargetPoint = targetVis.CurrentPosition;
            else
            {
                this.ShotCurrentTargetShip = targetShip;
                targetShip.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon += 0.5f;
                this.ShotCurrentTargetPoint = targetShip.GetCurrentEmissionHitPoint();
            }
        }
        #endregion

        #region TryUpdatingTargetPoint
        private void TryUpdatingTargetPoint()
        {
            if ( this.RelatedEntity == null )
                return;
            bool localDebug = false;
            SquadVisualizer curTargSquad = this.ShotCurrentTargetSquad;
            GameEntity_Squad curTargSquadEntity = null;
            try
            {
                curTargSquadEntity = curTargSquad == null ? null : curTargSquad.RelatedEntity.GetSquad();
            }
            catch { }

            if ( curTargSquad == null || curTargSquadEntity == null )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "A ", Verbosity.DoNotShow );
                this.ShotSquadTargetGameStylePoint = this.Fallback_LastTargetPoint.ToVisualMainGameCoordinates_Numerics( planet ); //target squad is already dead
            }
            else
            {
                SquadVisualizer curShotFF = this.SquadWithForcefieldWeShot;
                GameEntity_Squad curShotFFEntity = null;
                try
                {
                    curShotFFEntity = curShotFF == null ? null : curShotFF.RelatedEntity.GetSquad();
                }
                catch { }

                if ( curShotFF != null && curShotFF != null )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "B ", Verbosity.DoNotShow );
                    this.ShotSquadTargetGameStylePoint = curShotFF.GetCloserPointOnForceFieldIfPossible( this.CurrentPosition );
                }
                else
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "C", Verbosity.DoNotShow );
                    this.ShotSquadTargetGameStylePoint = curTargSquadEntity.WorldLocation.ToVisualMainGameCoordinates_Numerics( planet );
                }
            }
        }
        #endregion

        #region ReactToShotHittingSquad
        public int ReactToShotHittingSquad( GameEntity_Squad TargetSquad, GameEntity_Squad ProtectingShieldThatTookTheHitOrNull, int NumberOfShipsKilled, bool WasEntireSquadKilled )
        {
            GameEntity_Shot relatedEntOrNull = this.RelatedEntity;

            bool localDebug = false;
            bool quickDebug = false;
            int numK = 0;
            int debugValue = 0;
            try
            {

                this.ShotDisplay = ShotDisplayStyle.HeadingToSpecificLocationAcrossLastFewFrames;
                this.ShotStartingPointForFinalApproach = this.CurrentPosition;
                this.finalApproachProgress = 0;
                this.DeathCounterInevitable_TimeLeft = 3f;
                this.DeathCounterInevitable_Enabled = true;
                this.EOLType = ShotEOLType.Dissipating;
                debugValue = 1;
                if ( TargetSquad == null )
                {
                    if ( this.ShotCurrentTargetShip != null )
                        this.ShotCurrentTargetPoint = this.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                    else if ( relatedEntOrNull != null )
                        this.ShotCurrentTargetPoint = relatedEntOrNull.WorldLocation.ToVisualMainGameCoordinates_Numerics( this.ShotCurrentTargetPoint.Y, planet );
                    return numK;
                }
                debugValue = 2;
                SquadVisualizer squad = (SquadVisualizer)TargetSquad.InstancedRenderer;
                if ( squad == null )
                {
                    debugValue = 3;
                    if ( this.ShotCurrentTargetSquad != null && this.ShotCurrentTargetSquad.RelatedEntity.GetSquad() == TargetSquad )
                        squad = this.ShotCurrentTargetSquad;
                    if ( squad == null )
                    {
                        if ( relatedEntOrNull != null )
                            this.ShotCurrentTargetPoint = relatedEntOrNull.WorldLocation.ToVisualMainGameCoordinates_Numerics( this.ShotCurrentTargetPoint.Y, planet );
                        return numK;
                    }
                }
                else
                {
                    debugValue = 4;
                    //a single Shot can be queued against multiple targets, so if this Shot was used
                    //against a different target before, replace the old value (and update the Ship being hit to be part of this squad)
                    if ( this.ShotCurrentTargetSquad != squad )
                    {
                        this.ShotCurrentTargetSquad = squad;
                        this.TryRewiringShotToNewShipAtSameTargetSquad( null );
                    }
                }

                shipsTakingHits.Clear();

                if ( ProtectingShieldThatTookTheHitOrNull != null )
                {
                    debugValue = 5;
                    if ( relatedEntOrNull != null )
                        this.ShotCurrentTargetPoint = relatedEntOrNull.WorldLocation.ToVisualMainGameCoordinates_Numerics( this.ShotCurrentTargetPoint.Y, planet );
                    debugValue = 6;
                    this.SquadWithForcefieldWeShot = null;
                    SquadVisualizer squadWithForcefield = (SquadVisualizer)ProtectingShieldThatTookTheHitOrNull.InstancedRenderer;
                    if ( localDebug || quickDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "ReactToShotHittingSquad: " + this.RelatedTypeData.InternalName + " " + relatedEntOrNull?.PrimaryKeyID + " is aiming for sqaud " + TargetSquad.PrimaryKeyID + " " + TargetSquad.TypeData.InternalName + " shield path", Verbosity.DoNotShow );
                    if ( squadWithForcefield != null && squadWithForcefield is SquadVisualizer )
                        this.SquadWithForcefieldWeShot = (SquadVisualizer)squadWithForcefield;

                    this.EOLType = ShotEOLType.HitShield;
                    //                this.DeathCounterInevitable_TimeLeft  = 0.01f;
                    this.DeathCounterInevitable_TimeLeft = 3f;
                    this.DeathCounterInevitable_Enabled = true;
                    debugValue = 70;
                    if ( this.ShotDisplay == ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp )
                    {
                        debugValue = 71;
                        AddShot( TargetSquad );
                    }
                    debugValue = 72;
                    this.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                    return numK;
                }
                else //not shielded
                {
                    debugValue = 8;
                    if ( localDebug || quickDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "ReactToShotHittingSquad: " + this.RelatedTypeData.InternalName + " " + relatedEntOrNull?.PrimaryKeyID + " is aiming for sqaud " + TargetSquad.PrimaryKeyID + " " + TargetSquad.TypeData.InternalName + " non-shield. Trying to kill " + NumberOfShipsKilled + " ships. EntireSquadKiller: " + WasEntireSquadKilled, Verbosity.DoNotShow );
                    this.EOLType = ShotEOLType.HitShip;
                    if ( this.ShotDisplay == ShotDisplayStyle.HiddenWhileWaitingForDataToDoLerp )
                        AddShot( TargetSquad );
                    debugValue = 9;
                    this.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;

                    if ( localDebug )
                    {
                        string logStr = "list IDs: ";
                        for ( int k = squad.ShipsLiving.Count - 1; k >= 0; k-- )
                        {
                            logStr += "<" + squad.ShipsLiving[k].myID + " " + squad.ShipsLiving[k].IncomingShotCountAfterWhichIDie + "> ";
                        }
                        ArcenDebugging.ArcenDebugLogSingleLine( "Before hit: " + squad.ShipsLiving.Count + " ships in " + squad.RelatedTypeData.InternalName + " squad " + squad.myID + " " + logStr, Verbosity.DoNotShow );
                    }
                    if ( this.ShotCurrentTargetSquad == squad )
                    {
                        debugValue = 10;
                        //A given shot can have multiple targets. ShotCurrentTargetSquad is just the Shot.Target (aka the first target),
                        //and will sometimes be squad (the current target of this shot). A given shot can have "DoHitLogic--> enqueue shot --> shot hits" multiple times
                        if ( this.ShotCurrentTargetShip != null )
                        {
                            debugValue = 11;
                            this.ShotCurrentTargetPoint = this.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                            this.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                            if ( NumberOfShipsKilled > 1 && !WasEntireSquadKilled )
                            {
                                debugValue = 12;
                                if ( localDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Multi shot stage 1", Verbosity.DoNotShow );
                                //Handle the case where a shot is hitting multiple targets
                                //This function sets the potentialShipTargetsList
                                shipsTakingHits.Clear();
                                this.GetAdditionalShipsHitByMultiShot( TargetSquad, shipsTakingHits, NumberOfShipsKilled - 1, this.ShotCurrentTargetShip );

                                shipsTakingHits.Add( this.ShotCurrentTargetShip );
                                debugValue = 13;
                                for ( int i = 0; i < shipsTakingHits.Count; i++ )
                                {
                                    if ( shipsTakingHits[i] == null )
                                        continue;
                                    numK++;
                                    shipsTakingHits[i].IncomingShotCountAfterWhichIDie++;
                                    if ( localDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Ship " + shipsTakingHits[i].myID + " squad " + squad.myID + " has taken fatal damage (multi shot): " + shipsTakingHits[i].IncomingShotCountAfterWhichIDie, Verbosity.DoNotShow );
                                }
                            }
                            else if ( NumberOfShipsKilled > 0 && !WasEntireSquadKilled )
                            {
                                debugValue = 14;
                                numK++;
                                this.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie++;
                                if ( localDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Ship " + this.ShotCurrentTargetShip.myID + " squad " + squad.myID + " has taken fatal damage (single shot): " + this.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie, Verbosity.DoNotShow );
                                if ( !squad.ShipsLiving.Contains( this.ShotCurrentTargetShip ) )
                                {
                                    debugValue = 15;
                                    if ( this.ShotCurrentTargetShip.RelatedTypeData == null )
                                    {
                                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: shot heading for ship with null relatedTypeData", Verbosity.DoNotShow );
                                    }
                                    //TODO: these printouts were still getting triggered sporadically, but there's no bad side effects.
                                    //Since the game is hitting EA, there's no reason to frighten anyone unnecessarily. It still would be nice to fix it eventually though....
                                    // else if(squad.ShipsDying.Contains(this.ShotCurrentTargetShip))
                                    // {
                                    //     ArcenDebugging.ArcenDebugLogSingleLine("MINOR BUG: ship killed is already on Dying list. squad " + squad.RelatedEntity.PrimaryKeyID + " " + squad.RelatedTypeData.InternalName, Verbosity.DoNotShow );    
                                    // }
                                    // else
                                    //     ArcenDebugging.ArcenDebugLogSingleLine("BUG: ship killed is not part of target squad. squad " + squad.RelatedEntity.PrimaryKeyID + " " + squad.RelatedTypeData.InternalName + " shipid " +this.ShotCurrentTargetShip.myID + " in squad " + this.ShotCurrentTargetShip.CurrentSquad.RelatedTypeData.InternalName + " " + this.ShotCurrentTargetShip.CurrentSquad.RelatedEntity.PrimaryKeyID, Verbosity.DoNotShow );                       

                                }
                            }
                            else if ( WasEntireSquadKilled )
                            {
                                debugValue = 16;
                                squad.SetDeactivationReason( InstancedRendererDeactivationReason.FatalDamageTakenInVisAttack );
                                for ( int k = squad.ShipsLiving.Count - 1; k >= 0; k-- )
                                {
                                    squad.ShipsLiving[k].IncomingShotCountAfterWhichIDie++;
                                }
                            }
                        } //if these conditions aren't hit then noone died, which is fine

                        else
                        {
                            debugValue = 17;
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Shot sim ID " + relatedEntOrNull?.PrimaryKeyID + " hitting squad " + squad.myID + " ShotCurrentTargetShip is null", Verbosity.DoNotShow );
                            this.ShotCurrentTargetPoint = this.ShotCurrentTargetSquad.CurrentPosition;
                            this.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                        }
                    }
                    if ( localDebug )
                    {
                        if ( numK != NumberOfShipsKilled )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Attempted to kill " + NumberOfShipsKilled + " " + squad.RelatedTypeData.InternalName + " " + " but only got " + numK + " with a " + this.RelatedTypeData.InternalName, Verbosity.DoNotShow );
                        }
                    }
                    if ( NumberOfShipsKilled > 0 || WasEntireSquadKilled )
                    {
                        debugValue = 18;
                        //If this shot has killed its target Ship(s) then find all the other shots at those Ship(s) and reroute them to
                        //other ships in the squad (if there are other ships to hit)
                        int shotLength;
                        ShotVisualizer[] otherShots = BattlefieldVisualSingleton.Instance.ActiveShots.GetActiveList( out shotLength, false );
                        ShotVisualizer shot;
                        for ( int i = 0; i < shotLength; i++ )
                        {
                            shot = otherShots[i];
                            if ( shot == null )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( "Hit Null shot when rewiring", Verbosity.DoNotShow );
                                continue;
                            }
                            if ( shot == this || !shot.IsConsideredActive )
                                continue;
                            if ( shot.ShotCurrentTargetShip != null && shot.ShotCurrentTargetShip.CurrentSquad == squad )
                            {
                                debugValue = 19;
                                if ( WasEntireSquadKilled )
                                {
                                    debugValue = 20;
                                    shot.ShotCurrentTargetPoint = shot.ShotTargetLoc = shot.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                                    shot.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                                    shot.ShotStartingPointForFinalApproach = shot.CurrentPosition;
                                    shot.finalApproachProgress = 0;
                                    shot.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie++;
                                    shot.DeathCounterInevitable_TimeLeft = 3f;
                                    shot.DeathCounterInevitable_Enabled = true;
                                }
                                if ( NumberOfShipsKilled > 1 && !WasEntireSquadKilled )
                                {
                                    debugValue = 21;
                                    if ( shipsTakingHits == null )
                                    {
                                        if ( localDebug )
                                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG: shipsTakingHits is null", Verbosity.DoNotShow );
                                        continue;
                                    }
                                    ShipVisualizer prevTarget = shot.ShotCurrentTargetShip;
                                    for ( int k = 0; k < shipsTakingHits.Count; k++ )
                                    {
                                        if ( shipsTakingHits[k] == null )
                                            ArcenDebugging.QuickDebug( "BUG: shipsTakingHits[" + k + "] is null" );
                                        if ( shot.ShotCurrentTargetShip == shipsTakingHits[k] )
                                        {
                                            debugValue = 22;
                                            if ( localDebug )
                                                ArcenDebugging.ArcenDebugLogSingleLine( "rewire shot " + shot.RelatedEntity.PrimaryKeyID + " (multi shot case)", Verbosity.DoNotShow );
                                            shot.TryRewiringShotToNewShipAtSameTargetSquad( shipsTakingHits );
                                            debugValue = 23;
                                            //but if not possible, then back to this one and collide with it
                                            if ( shot.ShotCurrentTargetShip == null )
                                                shot.ShotCurrentTargetShip = prevTarget;
                                            if ( shot.ShotCurrentTargetShip == prevTarget )
                                            {
                                                debugValue = 24;
                                                shot.ShotCurrentTargetPoint = shot.ShotTargetLoc = shot.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                                                shot.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                                                shot.ShotStartingPointForFinalApproach = shot.CurrentPosition;
                                                shot.finalApproachProgress = 0;
                                                shot.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie++;
                                                shot.DeathCounterInevitable_TimeLeft = 3f;
                                                shot.DeathCounterInevitable_Enabled = true;
                                            }
                                        }
                                    }
                                }
                                if ( NumberOfShipsKilled == 1 && !WasEntireSquadKilled )
                                {
                                    debugValue = 25;
                                    if ( shot.ShotCurrentTargetShip == this.ShotCurrentTargetShip )
                                    {
                                        debugValue = 26;
                                        ShipVisualizer prevTarget = shot.ShotCurrentTargetShip;
                                        //try to repoint shot
                                        shot.TryRewiringShotToNewShipAtSameTargetSquad( null );
                                        //but if not possible, then back to this one and collide with it
                                        if ( localDebug )
                                            ArcenDebugging.ArcenDebugLogSingleLine( "rewire shot " + shot.RelatedEntity.PrimaryKeyID + " (single shot case). Prev " + prevTarget.myID + " --> " + shot.ShotCurrentTargetShip.myID, Verbosity.DoNotShow );
                                        if ( shot.ShotCurrentTargetShip == null )
                                            shot.ShotCurrentTargetShip = this.ShotCurrentTargetShip;
                                        if ( shot.ShotCurrentTargetShip == prevTarget )
                                        {
                                            debugValue = 27;
                                            shot.ShotCurrentTargetPoint = shot.ShotTargetLoc = shot.ShotCurrentTargetShip.GetCurrentEmissionHitPoint();
                                            shot.ShotDisplay = ShotDisplayStyle.HeadingToInterceptTargetAcrossLastFewFrames;
                                            shot.ShotStartingPointForFinalApproach = shot.CurrentPosition;
                                            shot.finalApproachProgress = 0;
                                            shot.ShotCurrentTargetShip.IncomingShotCountAfterWhichIDie++;
                                            shot.DeathCounterInevitable_TimeLeft = 3f;
                                            shot.DeathCounterInevitable_Enabled = true;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    if ( localDebug )
                    {
                        string logStr = "list IDs: ";
                        for ( int k = squad.ShipsLiving.Count - 1; k >= 0; k-- )
                        {
                            logStr += "<" + squad.ShipsLiving[k].myID + " " + squad.ShipsLiving[k].IncomingShotCountAfterWhichIDie + "> ";
                        }
                        ArcenDebugging.ArcenDebugLogSingleLine( "after hit: " + squad.ShipsLiving.Count + " ships in " + squad.RelatedTypeData.InternalName + " " + squad.myID + " " + logStr, Verbosity.DoNotShow );
                    }
                }

                return numK;
            }
            catch //( Exception e )Chris says: actually no need to tell us about this, as this is just a cross-threading thing
            {
                if ( debugValue > 0 ) { }
                //ArcenDebugging.ArcenDebugLog( "Error during ReactToShotHittingSquad debug number " + debugValue +
                //                              "\n" + e, Verbosity.ShowAsError );
            }

            return numK;
        }
        #endregion

        public int GetCurrentLODAndLastDistanceFromCamera( out float LastDistanceFromCamera )
        {
            LastDistanceFromCamera = 0;
            return 0;
        }

        public System.Numerics.Vector3 GetPositionPlusAnyOffsets()
        {
            return this.CurrentPosition;
        }
    }

    public enum ShotEOLType
    {
        /// <summary>
        /// It's just gone, I dunno.  Maybe it missed because its target went through a wormhole and escape.
        /// </summary>
        Dissipating = 0,
        /// <summary>
        /// Bam, we hit some sort of target.  Most common case
        /// </summary>
        HitShip,
        /// <summary>
        /// Blam, blocked by a forcefield.  Oh well, at least we damaged that!
        /// </summary>
        HitShield,
    }

    public enum ShotDisplayStyle
    {
        /// <summary>
        /// This is set typically by a player overriding some settings.
        /// </summary>
        TrueLocationByDesign,
        /// <summary>
        /// Typically some sort of data was missing in trying to do a lerp, so now we're doing true location instead.
        /// </summary>
        TrueLocationByFailure,
        /// <summary>
        /// Typically we were waiting for some data that is typically asynchronously updated (a visualship entry for the target may not yet exist, etc),
        /// and since that data was not forthcoming after a certain point, we're just going with the true location.
        /// </summary>
        HiddenByTimeout,
        /// <summary>
        /// We don't have enough data yet (something may be asynchronously loading, like the visualship entry of the target), but we want to lerp,
        /// and so for now we're just waiting around to see if the data arrives.  If it goes on too long, we'll just failover into this. Note that displaying this
        /// causes shots to shoot off into the corner of the screen (the default location), so it will just never show up
        /// </summary>
        HiddenWhileWaitingForDataToDoLerp,
        /// <summary>
        /// We're doing the main lerping logic.  The shot is moving from some location to another location every sim frame, and the shot exists on the sim frame
        /// both before and after this one.  The natural first thought: we just lerp between those two spots, easy peasy, right?
        /// 
        /// NO!  Sadly not.  The target and origin ships exist in 3D space and worse are moving around independent of their actual origin squad central point, so
        /// we can't do that sort of thing.  Otherwise you'd have a wicked curve to shots at the very start and end, and otherwise them heading between the center of
        /// squads, which would look super wrong.
        /// 
        /// Instead we have to calculate a percentage for how far along the track the game-sim shot is from its original location to its present target location (that changes as the target moves, potentially),
        /// and then we have to calculate the appropriate location for how far along the track the visual-sim shot should be from its "offset original location" based on 
        /// the actual location of the ship that visually fired from its squad, relative to whatever the current position is of the ship it's after, which may be moving both within its squad as well as the squad moving.
        /// 
        /// The percentage we calculate gets updated only every sim cycle, so tenth of a second or so, which means that we lerp between the percentage points as those are updated.  Even those percentages are not fluid.
        /// It's possible that the percentage points will go down if the target is fleeing faster than the shot moves, but in that instance we never let the actual calculated percentage get lower.
        /// In that instance, just keeping the same percentage and calculating the 3d-space points again will provide a further-on position, so the shot keeps moving anyway.
        /// 
        /// But it never winds up jumping backwards, which would look really bad.
        /// Badger notes: this state exists after a shot has been Activated by the Sim code, but before the shot actually hits. Once a shot
        /// actually hits (via DoHitLogic-->ReactToShotHittingSquad) then it Heads to Location/Target for the last few frames
        /// </summary>
        LerpingBetweenEquivalentPoints,
        /// <summary>
        /// The underlying shot in the game-sim has died, but we need to finish drawing our approach to wherever it died.
        /// This mostly happens because something hit a forcefield, or something caused its target to disappear.
        /// Normally we'd be proceeding all the way to a ship target via HeadingToInterceptTargetAcrossLastFewFrames instead of this,
        /// but for some reason we don't have a target to go to, so instead we're going to the last location of the shot in the sim when the shot died in sim.
        /// 
        /// Ideally we keep a speed that is consistent with our recent speed approaching the target, though, rather than getting asymptotically slower as we get closer to the target.
        /// Unfortunately, asymptotes seem to pop up now and then, mainly with forcefields.
        /// </summary>
        HeadingToSpecificLocationAcrossLastFewFrames,
        /// <summary>
        /// The underlying shot in the game-sim has died, and hit some target that is in the visual layer.
        /// We need to keep drawing this shot lerping from its current position to the current position of the target in the visual layer.
        /// All we care about is where the target is now (in 3d visual space, this frame), and where the shot is now (in 3D visual space, this frame).
        /// 
        /// Ideally we keep a speed that is consistent with our recent speed approaching the target, though, rather than getting asymptotically slower as we get closer to the target.
        /// Unfortunately, asymptotes seem to pop up now and then, mainly with forcefields.
        /// </summary>
        HeadingToInterceptTargetAcrossLastFewFrames,
        /// <summary>
        /// We dun died, and for some reason we need to stay right where we are for a bit.  So... just sit here I guess!
        /// </summary>
        KeepCurrentPositionWhileDying
    }
}
