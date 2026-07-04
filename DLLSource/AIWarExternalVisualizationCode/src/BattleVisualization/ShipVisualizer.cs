using System;
using Arcen.AIW2.Core;
using System.Numerics;
using Arcen.Universal;

namespace Arcen.AIW2.ExternalVisualization
{
    public class ShipVisualizer : ConcurrentPoolable<ShipVisualizer>, IInstancedRenderer, IRelatedEntity
    {
        public bool IsConsideredActive = false;

        public SquadVisualizer CurrentSquad = null;
        private GameEntity_Squad EntityForVeryLimitedUses;
        public GameEntityTypeData RelatedTypeData;
        public GameEntityTypeData.MarkLevelStats RelatedDataForMark;
        public Faction RelatedFaction;
        public float CurrentScaleMultiplier = -1;

        public StateOfMatterTypeData LastSeenStateOfMatter;
        public Planet LastSeenPlanet;

        public UnityEngine.Vector3 CurrentPositionOffset = Mat.V3_Zero;

        public ShipRenderManagerGroup RenderGroup = null;
        
        public float TimeUntilIIdeallyFireAgain = 0f;
        public float TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon = 0f;
        public bool IsStillInGame = true;

        public int IncomingShotCountAfterWhichIDie = 0;
        public float DeathCounterInevitable_TimeLeft = -1f;
        public bool DeathCounterInevitable_Enabled = false;        
        
        public bool hasTakenLethalDamage = false;

        /* Burning and Dying is for ships that have taken fatal damage,
           to give a good "On Death" effect */
        private float BurningAndDyingProgress = -1;
        
        /* Spawn is for when reinforcements are being added to a squad */
        public float SpawnProgress = -1;
        public bool stillSpawning = false;

        /* Anti-Spawn is the opposite of spawn. Units start their normal size, then take the Spawning Renderer
           and then shrink. This is used for a squad that is being upgraded */
        public float AntiSpawnProgress = -1;
        public bool stillAntiSpawning = false;
        
        //these IDs are used internally for debugging
        public int myID = 0;
        public static int id;
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ShipVisualizers" );

        public ShipVisualizer()
        {
            RefTracker.IncrementObjectCount();
            this.myID = id++;
            this.hasTakenLethalDamage = false;
        }

        public void Activate( GameEntity_Base RelatedToBase, GameEntityTypeData RelatedTypeData )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            //bool localDebug = false;
            if ( this.IsConsideredActive )
                return;
            this.IsConsideredActive = true;
            
            this.RelatedTypeData = RelatedTypeData;
            try
            {
                this.EntityForVeryLimitedUses = RelatedToBase as GameEntity_Squad;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Wrong type send to ShipVisualizer.Activate(): " + ( RelatedToBase == null ? "NULL" : RelatedToBase.GetType().ToString() ) +
                    " RelatedTypeData: " + ( RelatedTypeData == null ? "NULL" : RelatedTypeData.InternalName ) + "\n" + e, Verbosity.ShowAsError );
            }

            this.RelatedDataForMark = this.EntityForVeryLimitedUses.DataForMark;
            this.RelatedFaction = this.EntityForVeryLimitedUses.GetFactionOrNull_Safe();

            this.LastSeenStateOfMatter = null;
            this.LastSeenPlanet = null;

            this.IncomingShotCountAfterWhichIDie = 0;
            this.DeathCounterInevitable_TimeLeft = -1f;
            this.DeathCounterInevitable_Enabled = false;
            this.TimeUntilIIdeallyFireAgain = 0f;
            this.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon = 0f;
            this.IsStillInGame = true;
            this.stillSpawning = false;

            this.UnsetBurningAndDyingStatus( false );
            this.UnsetSpawnStatus();
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

            //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
            //    ArcenDebugging.ArcenDebugLogSingleLine( "DeactivateAndReturnToPool: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + " " + Reason + " " + IsConsideredActive, Verbosity.DoNotShow );
            if ( !this.IsConsideredActive && !ForceEvenIfNotConsideredActive )
                return;
            this.IsConsideredActive = false; //must happen right at the top, or bad things happen!
            
            //leave these until next activation:
            //this.RelatedTypeData = null;
            //this.RelatedDataForMark = null;

            try
            {
                if ( BattlefieldVisualSingleton.Instance.BurningDyingShips.Contains( this ) )
                    BattlefieldVisualSingleton.Instance.BurningDyingShips.Remove( this );
            }
            catch { }//occasional threading exception

            RemoveShipFromFormation();

            if ( this.CurrentSquad != null )
            {
                try
                {
                    if ( this.CurrentSquad.ShipsLiving.Contains( this ) )
                    this.CurrentSquad.ShipsLiving.Remove( this );
                }
                catch { }//occasional threading exception
                try
                {
                    if ( this.CurrentSquad.ShipsDying.Contains( this ) )
                        this.CurrentSquad.ShipsDying.Remove( this );
                }
                catch { }//occasional threading exception
                this.CurrentSquad = null;
            }

            this.CurrentSquad = null;
            this.IncomingShotCountAfterWhichIDie = 0;
            this.DeathCounterInevitable_TimeLeft = -1f;
            this.DeathCounterInevitable_Enabled = false;
            this.TimeUntilIIdeallyFireAgain = 0f;
            this.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon = 0f;
            this.IsStillInGame = false;

            this.UnsetBurningAndDyingStatus( false );
            this.UnsetSpawnStatus();

            ShipRenderManagerGroup.PutBackInPoolRightAway( this );
            this.RenderGroup = null;
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.DeactivateAndReturnToPool( InstancedRendererDeactivationReason.IAmHeadedBackToPool, true );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public void DetachVisObjectFromSim( InstancedRendererDeactivationReason Reason )
        {
            //UnityEngine.Debug.Log( ( this.RelatedTypeData == null ? "Null ship" : this.RelatedTypeData.InternalName ) + " IsConsideredActive: " + this.IsConsideredActive + " Reason:" + Reason );
            this.DeactivateAndReturnToPool( Reason, false );
        }

        public GameEntity_Base GetEntityRelatedTo()
        {
            return this.EntityForVeryLimitedUses;
        }

        public void SetFormationPositionFromSquad( SquadVisualizer Squad )
        {
            this.CurrentSquad = Squad;

            if ( Squad == null || Squad.RelatedTypeData == null )
            {
                //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no squad actually set during SetFormationPositionFromSquad of type " +
                //        ( RelatedTypeData == null ? "??" : RelatedTypeData.InternalName ), Verbosity.DoNotShow );
                return;
            }
            GameEntityTypeData relatedTypeData = Squad.RelatedTypeData;
            if ( relatedTypeData == null )
                return;
            GameEntity_Squad relatedEnt = Squad.RelatedEntity.GetSquad();
            if ( relatedEnt == null )
                return;

            Planet planet = relatedEnt.Planet;
            if ( planet == null )
                return;
            
            var basePos = Vector3.Zero;
            basePos.Y += relatedTypeData.YOffsetOfShipInVisualSpace;
            this.CurrentPositionOffset = ( basePos * planet.GravWellSize.VisualShipScaleMultiplier ).ToUnityVector3();
        }

        public void RemoveShipFromFormation()
        {
            
        }

        #region UnsetBurningAndDyingStatus
        private void UnsetBurningAndDyingStatus( bool JustTheVisuals )
        {
            if ( this.BurningAndDyingProgress >= 0 )
            {
                this.BurningAndDyingProgress = -1;
            }
            if ( !JustTheVisuals )
                this.hasTakenLethalDamage = false;
        }

        #endregion

        public InstancedRendererDeactivationReason GetLastFlagForRemovalReason()
        {
            return InstancedRendererDeactivationReason.Unknown;
        }

        public Vector3 GetCurrentEmissionHitPoint( ArcenPoint Offset )
        {
            SquadVisualizer squad = this.CurrentSquad;
            if ( squad == null )
                return this.CurrentPositionOffset.ToNumericsVector3();
            GameEntity_Squad relatedEnt = squad.RelatedEntity.GetSquad();
            if ( relatedEnt == null )
                return this.CurrentPositionOffset.ToNumericsVector3();
            Planet planet = relatedEnt.Planet;
            if ( planet == null )
                return this.CurrentPositionOffset.ToNumericsVector3();

            Vector3 pos = ( this.CurrentPositionOffset + ( new UnityEngine.Vector3( Offset.X, 0, Offset.Y ) * planet.GravWellSize.VisualShipScaleMultiplier )).ToNumericsVector3();
            if ( this.RelatedTypeData != null )
                pos.Y = this.RelatedTypeData.YOffsetOfShipEmissionAndHitPoint;
            if ( squad != null )
                pos += squad.CurrentPosition;
            return pos;
        }

        public static Vector3 GetCurrentEmissionHitPointFromTypeAndLocation( GameEntityTypeData TypeData, ArcenPoint OriginalWorldPosition, ArcenPoint Offset, Planet planet )
        {
            Vector3 pos = OriginalWorldPosition.ToVisualMainGameCoordinates_Numerics( planet ) + (new Vector3( Offset.X, 0, Offset.Y ) * 
                (planet == null ? PlanetGravWellSize.Base_VisualShipScaleMultiplier : planet.GravWellSize.VisualShipScaleMultiplier ));
            if ( TypeData != null )
                pos.Y = TypeData.YOffsetOfShipEmissionAndHitPoint;
            return pos;
        }

        public Vector3 GetCurrentEmissionHitPoint()
        {
            Vector3 pos = this.CurrentPositionOffset.ToNumericsVector3();
            if ( this.RelatedTypeData != null )
                pos.Y = this.RelatedTypeData.YOffsetOfShipEmissionAndHitPoint;
            if ( this.CurrentSquad != null )
                pos += this.CurrentSquad.CurrentPosition;
            return pos;
        }

        public UnityEngine.Vector3 GetCurrentDrawPoint()
        {
            UnityEngine.Vector3 pos = this.CurrentPositionOffset;
            if ( this.CurrentSquad != null )
                pos += this.CurrentSquad.CurrentPosition.ToUnityVector3();
            return pos;
        }

        public void RenderShip()
        {
            int debugStep = 1;
            GameEntity_Squad squadFineIfNull = this.EntityForVeryLimitedUses;
            try
            {
                int itemsInStack = 0;
                if ( squadFineIfNull != null )
                {
                    this.LastSeenStateOfMatter = squadFineIfNull.CurrentStateOfMatter;
                    this.LastSeenPlanet = squadFineIfNull.Planet;

                    if ( this.LastSeenStateOfMatter != null && !this.LastSeenStateOfMatter.ShipsRenderInThisState )
                        return; //this will let us draw the icon, but not the ship itself

                    itemsInStack = squadFineIfNull.ExtraStackedSquadsInThis;
                }
                if ( this.RelatedTypeData.ShipSkipsTraditionalRendering )
                    return; //probably we want to draw addons or something.  And of course the icon.  But not the traditional-style ship model

                GameEntityTypeData.MarkLevelStats dataForMark = this.RelatedDataForMark;
                if ( dataForMark == null )
                    return;
                ShipRenderManagerGroup renderGroup = this.RenderGroup;
                if ( renderGroup == null )
                    return;

                float relatedFloat = 0;
                ShipRenderStatus renderStatus = ShipRenderStatus.Normal;
                if ( this.BurningAndDyingProgress > 0 )
                {
                    debugStep = 10;
                    renderStatus = this.CurrentSquad.ShipsLiving.Count <= 1 && itemsInStack <= 0 ? ShipRenderStatus.BurningAndDyingLastDeath : ShipRenderStatus.BurningAndDyingNonLastDeath;
                    relatedFloat = this.BurningAndDyingProgress;
                    //UnityEngine.Debug.Log( renderStatus );
                }
                else if ( this.AntiSpawnProgress > 0 )
                    renderStatus = ShipRenderStatus.AntiSpawn;
                else if ( this.SpawnProgress > 0 )
                    renderStatus = ShipRenderStatus.Spawn;
                else if ( squadFineIfNull != null )
                {
                    if ( squadFineIfNull.SelfBuildingMetalRemaining > 0 ||
                         (squadFineIfNull.SecondsTillTransformation > 0 &&
                          squadFineIfNull.TypeData.TransformationCountdownOnlyDuringCombat == false &&
                          !string.IsNullOrEmpty( squadFineIfNull.TransformsIntoAfterTime ) &&
                          !squadFineIfNull.TransformsIntoAfterTime.StartsWith( "$" )))
                    {
                        debugStep = 40;
                        if ( squadFineIfNull.IsInHoldFireMode ||
                            (this.RelatedFaction != null && this.RelatedFaction.LastFrame_MetalFlowRequestPortionMet < FInt.One) )
                            renderStatus = ShipRenderStatus.UnderConstructionStalled;
                        else
                            renderStatus = ShipRenderStatus.UnderConstruction;
                    }
                    else if ( squadFineIfNull.SecondsSpentAsRemains > 0 )
                        renderStatus = ShipRenderStatus.ShipRemains;
                    else if ( this.RelatedTypeData.IsDrawnAsWarpingIn )
                        renderStatus = ShipRenderStatus.WarpingIn;
                    else if ( this.LastSeenStateOfMatter != null && this.LastSeenStateOfMatter.ShipsDrawAsSoloPhaseMaterialInThisState )
                    {
                        if ( this.RelatedTypeData.UsesVertexAnimatedPhasing )
                            renderStatus = ShipRenderStatus.Phase_Solo_VertexAnim;
                        else
                            renderStatus = ShipRenderStatus.Phase_Solo_Still;
                    }
                    else if ( this.LastSeenStateOfMatter != null && this.LastSeenStateOfMatter.ShipsDrawAsMultiPhaseMaterialInThisState )
                    {
                        if ( this.RelatedTypeData.UsesVertexAnimatedPhasing )
                            renderStatus = ShipRenderStatus.Phase_Multi_VertexAnim;
                        else
                            renderStatus = ShipRenderStatus.Phase_Multi_Still;
                    }
                }

                debugStep = 100;

                debugStep = 200;
                float scale = dataForMark.VisualsScaleMultiplier * 
                    (this.LastSeenPlanet == null ? PlanetGravWellSize.Base_VisualShipScaleMultiplier : this.LastSeenPlanet.GravWellSize.VisualShipScaleMultiplier ) * renderGroup.PrototypeScale;
                if ( this.CurrentScaleMultiplier >= 0 )
                    scale *= this.CurrentScaleMultiplier;

                debugStep = 300;
                renderGroup.WriteToDrawBufferForOneFrame( this.GetCurrentDrawPoint(), this.CurrentSquad.CurrentRotation,
                    scale, this.CurrentSquad.CurrentLOD, renderStatus, GameEntity_Base.CurrentlyHoveredOver == squadFineIfNull ? squadFineIfNull : null, relatedFloat );

                //if ( GameEntity_Base.CurrentlyHoveredOver == squad )
                //    squad.DebugText = " renderStatus: " + renderStatus + " scale: " + scale + " VisualsScaleMultiplier: " + dataForMark.VisualsScaleMultiplier +
                //        " dataForMarkType: " + dataForMark.TypeData.InternalName +  "\ntime: " + DateTime.Now;
            }
            catch ( Exception e )
            {
                if ( squadFineIfNull == null )
                    return; //don't tell me about the error if the RelatedEntity became null, since that's a race condition thing.

                ArcenDebugging.ArcenDebugLogSingleLine( "Error in Ship.Rendership debugStep " + debugStep + " :\n\n" + e, Verbosity.ShowAsError );
            }
        }

        #region UpdateBurningAndDyingStatus
        /*
         * You can tweak how much the explosions seem to "strobe" by adjusting BURNING_DYING_SPEED_MULT_EARLY down to slow down the first part if you like, 
         * and then BURNING_DYING_SPEED_MULT_LATE to make the latter part go slower, too, if you want.
         * 
         * maxBurningAndDyingTime being set to 2 seconds may need to be extended if you make this a lot slower.
         * 
         * BURNING_DYING_EARLY_PART is basically the point, in the whole "explodes and then has shrapnel" at which it finishes exploding and then turns to shrapnel.  
         * That's on a 0-1 float scale that is being passed in as burningAndDyingProgress.
         * 
         * burningAndDyingProgress going from 0 to 1 is what makes it say "how exploded it is."  
         * The BURNING_DYING_SPEED_MULT_EARLY is how fast it proceeds until it hits BURNING_DYING_EARLY_PART, which is 0.3, 
         * and then BURNING_DYING_SPEED_MULT_LATE  is how fast it goes for the rest.
         * 
         * It's possible that you might want to tweak any of those, or make it go slower after a certain point, etc.  
         * It's using a custom shader that does a lot of things dynamically, etc.
         * The older logic about movement of the ship is still there, which is nice because ships drift off course, then.
         * */
        private const float BURNING_DYING_SPEED_MULT_EARLY = 0.5f;
        private const float BURNING_DYING_SPEED_MULT_LATE = 6f;
        private const int BURNING_DYING_LOCATION_MULT_LARGE = 2;
        private const int BURNING_DYING_LOCATION_MULT_LARGE_DOWN_MAX = -1;
        private const float BURNING_DYING_EARLY_PART = 0.3f;
        private const float maxBurningAndDyingTime = 2f;
        private UnityEngine.Vector3 burningDyingLocationChangeLarge = Mat.V3_Zero;
        public bool UpdateBurningAndDyingStatus( float DeltaTime )
        {
            //When a ship takes lethal damage, it is added to the BurningandDying list.
            //It changes the render to feel more like it's, well, Burning and Dying,
            //and also starts to vanish. Vanishing works as follows. For the first
            //few frames we shrink slowly, then we shrink really fast

            if ( !this.hasTakenLethalDamage )
                this.hasTakenLethalDamage = true; //Chris notes: go ahead and just mark it lethal.  There are too many ways to get here now..
                //ArcenDebugging.QuickDebug("BUG: Ship is on burningAndDying list but has not exploded");
            
            if ( this.BurningAndDyingProgress >= 0 )
            {
                if (this.BurningAndDyingProgress < BURNING_DYING_EARLY_PART ) //first go fast
                    this.BurningAndDyingProgress += ( DeltaTime * BURNING_DYING_SPEED_MULT_EARLY );
                else //then go slower
                    this.BurningAndDyingProgress += ( DeltaTime * BURNING_DYING_SPEED_MULT_LATE );
                if ( this.BurningAndDyingProgress >= maxBurningAndDyingTime )
                {
                    //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                    //    ArcenDebugging.ArcenDebugLogSingleLine( "BurningAndDyingProgress done: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName, Verbosity.DoNotShow );
                    return true;
                }
                float burningAndDyingProgress = UnityEngine.Mathf.Clamp01( this.BurningAndDyingProgress/maxBurningAndDyingTime);
                
                UnityEngine.Vector3 locationChange = burningDyingLocationChangeLarge * DeltaTime;
                this.CurrentPositionOffset += locationChange;

                if ( burningAndDyingProgress >= 1f )
                {
                    //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                    //    ArcenDebugging.ArcenDebugLogSingleLine( "BurningAndDyingProgress done: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName, Verbosity.DoNotShow );
                    return true;
                }
            }
            else
            {
                this.UnsetSpawnStatus(); //just in case -- this can happen easily
                this.BurningAndDyingProgress = 0;

                this.burningDyingLocationChangeLarge = new UnityEngine.Vector3( Engine_Universal.PermanentQualityRandom.NextFloat( -BURNING_DYING_LOCATION_MULT_LARGE, BURNING_DYING_LOCATION_MULT_LARGE ),
                    Engine_Universal.PermanentQualityRandom.NextFloat( -BURNING_DYING_LOCATION_MULT_LARGE, BURNING_DYING_LOCATION_MULT_LARGE_DOWN_MAX ),
                    Engine_Universal.PermanentQualityRandom.NextFloat( -BURNING_DYING_LOCATION_MULT_LARGE, BURNING_DYING_LOCATION_MULT_LARGE ) );
            }
            return false;
        }
        #endregion

        #region UnsetSpawnStatus
        public void UnsetSpawnStatus()
        {
            if ( this.SpawnProgress >= 0 )
            {
                if(this.SpawnProgress >= 0)
                    this.SpawnProgress = -1;
            }
            this.stillSpawning = false;
        }

        #endregion

        #region UpdateSpawnStatus
        private const float SPAWN_SPEED_MULT_EARLY = 14f;
        private const float SPAWN_SPEED_MULT_LATE = 4f;
        private const float maxSpawnTime = 0.5f;
        public bool UpdateSpawnStatus( float DeltaTime )
        {
            /* Plan: when addShip is called, we set stillSpawning on
               then in HandleLODsAndShipPartAnimationsForSquad we will say "Are you stillSpawning? if so updateSpawnStatus
               if that returns true, disable stillSpawning
            */
            if(!this.stillSpawning)
                ArcenDebugging.QuickDebug("BUG: Ship is no longer spawning, so it should not have called UpdateSpawnStatus");

            if ( this.SpawnProgress >= 0 )
            {
                if (this.SpawnProgress < maxSpawnTime/4) //first grow fast
                    this.SpawnProgress += ( DeltaTime * SPAWN_SPEED_MULT_EARLY );
                else //then grow slow at the end
                    this.SpawnProgress += ( DeltaTime * SPAWN_SPEED_MULT_LATE );
                if ( this.SpawnProgress >= maxSpawnTime )
                {
                    this.CurrentScaleMultiplier = -1;
                    return true;
                }
                float spawnScale = this.SpawnProgress/maxSpawnTime;
                if ( spawnScale > 1 )
                    spawnScale = 1;
                this.CurrentScaleMultiplier = spawnScale;
            }
            else
            {
                this.UnsetBurningAndDyingStatus( true ); //just in case -- this can happen, so we don't want to bake these wrong
                this.SpawnProgress = 0;
            }
            return false;
        }
        #endregion

        //START
        #region UnsetAntiSpawnStatus
        public void UnsetAntiSpawnStatus()
        {
            if ( this.AntiSpawnProgress >= 0 )
            {
                if(this.AntiSpawnProgress >= 0)
                    this.AntiSpawnProgress = -1;
            }
            this.stillAntiSpawning = false;
        }

        #endregion

        #region UpdateAntiSpawnStatus
        private const float maxAntiSpawnTime = 1.5f;
        public bool UpdateAntiSpawnStatus( float DeltaTime )
        {
            /* Plan: when addShip is called, we set stillAntiSpawning on
               then in HandleLODsAndShipPartAnimationsForSquad we will say "Are you stillAntiSpawning? if so updateAntiSpawnStatus
               if that returns true, disable stillAntiSpawning
            */
            if(!this.stillAntiSpawning)
                ArcenDebugging.QuickDebug("BUG: Ship is no longer spawning, so it should not have called UpdateAntiSpawnStatus");
            
            if ( this.AntiSpawnProgress >= 0 )
            {
                if (this.AntiSpawnProgress < maxAntiSpawnTime/4) //first shrink slow
                    this.AntiSpawnProgress += ( DeltaTime * SPAWN_SPEED_MULT_EARLY );
                else //then shrink fast at the end
                    this.AntiSpawnProgress += ( DeltaTime * SPAWN_SPEED_MULT_LATE );
                if ( this.AntiSpawnProgress >= maxAntiSpawnTime )
                {
                    this.CurrentScaleMultiplier = 0;
                    return true;
                }
                float antiSpawnScale = 1f - this.AntiSpawnProgress/maxAntiSpawnTime;
                if ( antiSpawnScale > 1 )
                    antiSpawnScale = 1;
                this.CurrentScaleMultiplier = antiSpawnScale;
            }
            else
            {
                this.UnsetBurningAndDyingStatus( true ); //just in case -- this can happen, so we don't want to bake these wrong
                this.AntiSpawnProgress = 0;
            }
            return false;
        }
        #endregion

        #region SetAntiSpawnStatus
        public void SetAntiSpawnStatus()
        {
        }
        #endregion
//END
        
        public bool GetIsToUseSharedPool()
        {
            return false;
        }

        public void DoDeathEffects()
        {
            if(this.hasTakenLethalDamage)
            {
                //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "hasTakenLethalDamage so skip: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName + 
                //        " BurningDyingShips.Contains: " + BattlefieldVisualSingleton.Instance.BurningDyingShips.Contains( this ), Verbosity.DoNotShow );
                return;
            }
            this.hasTakenLethalDamage = true;
            SquadVisualizer squadVis = this.CurrentSquad;
            GameEntity_Squad squad = squadVis == null ? null : squadVis.RelatedEntity.GetSquad();

            if ( squad != null )
            {
                switch ( squad.despawnVis )
                {
                    case DespawnVisualization.Transformation:
                    case DespawnVisualization.WarpOut:
                        //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                        //    ArcenDebugging.ArcenDebugLogSingleLine( squad.despawnVis + " add: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName, Verbosity.DoNotShow );
                        return;
                }
                //ArcenDebugging.ArcenDebugLogSingleLine( "Ship death: " + squad.TypeData.InternalName + " " +
                //    squad.despawnVis, Verbosity.DoNotShow );
            }
            //else
            //    ArcenDebugging.ArcenDebugLogSingleLine( "Ship death: unknowns!", Verbosity.DoNotShow );

            if ( this.RelatedTypeData != null && this.CurrentSquad != null )
            {
                if ( this.RelatedTypeData.SFX_ShipOrStructure_Explosion != null && !GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.SFXMuteShipExplosions ) && this.BurningAndDyingProgress == -1 )
                {
                    bool noSound = false;
                    if( squad != null && squad.suppressDeathAudioCue)
                        noSound = true;

                    if(!noSound)
                    {
                        PresentationLayer_AIW2.Instance.PlaySoundAtUnity3DLocation( SoundPropagation.PlayLocallyOnly, this.RelatedTypeData.SFX_ShipOrStructure_Explosion, this.CurrentSquad.CurrentPosition.ToUnityVector3() );
                    }
                }
            }

            if ( !BattlefieldVisualSingleton.Instance.BurningDyingShips.Contains( this ) )
            {
                //if ( this.RelatedEntity.DataForMark.CanAssist_Repair )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "BurningAndDyingProgress add: " + this.RelatedEntity.PrimaryKeyID + " " + this.RelatedTypeData.InternalName, Verbosity.DoNotShow );
                BattlefieldVisualSingleton.Instance.BurningDyingShips.Add( this );
                //once a ship has hit BurningAndDying, it's no longer managed by the squad,
                //it is updated by the Singleton
                if ( this.CurrentSquad.ShipsDying.Contains( this ) )
                    this.CurrentSquad.ShipsDying.Remove( this );
            }
        }

        public void DoShipUpdate( SquadVisualizer Squad )
        {
            this.CurrentSquad = Squad;
        }
                        
        public void DoGeneralUpdate( float DeltaTime )
        {
            if(this.hasTakenLethalDamage)
            {
                //nothing to do here, we are waiting to be cleaned up
                //and returned to the pool
                return;
            }

            if ( this.IncomingShotCountAfterWhichIDie > 0)
            {
                this.DoDeathEffects(); //this sets "hasTakenLethalDamage"
                return;
            }

            if ( this.TimeUntilIIdeallyFireAgain > 0 )
            {
                this.TimeUntilIIdeallyFireAgain -= DeltaTime;
                if ( this.TimeUntilIIdeallyFireAgain < 0 )
                    this.TimeUntilIIdeallyFireAgain = 0;
            }
            if ( this.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon > 0 )
                this.TimeAccumlationUntilIdeallyVisuallyGetsFiredUpon -= DeltaTime;
        }

        public bool GetIsSquad()
        {
            return false;
        }

        public void UpdateTeamColor() { }

        public void ReactToSquadReplacementShipJustDeployed( GameEntity_Squad EntityProvidingReplacement ) { }

        public void ShowBurningAndDyingEffectForShipInStackIfVisualObjectExistsForStack( GameEntity_Squad EntityProvidingReplacement ) { }

        public int ReactToShotHittingSquad( GameEntity_Squad TargetSquad, GameEntity_Squad ProtectingShieldThatTookTheHitOrNull, int NumberOfShipsKilled, bool WasEntireSquadKilled ) { return 0; }

        public void WriteDebugDataTo( ArcenCharacterBuffer Buffer ) { }

        public int GetCurrentLODAndLastDistanceFromCamera( out float LastDistanceFromCamera )
        {
            LastDistanceFromCamera = 0;
            return 0;
        }

        public System.Numerics.Vector3 GetPositionPlusAnyOffsets()
        {
            return this.GetCurrentEmissionHitPoint();
        }
    }

    public enum ShipRenderStatus
    {
        Normal = 0,
        AntiSpawn,
        Spawn,
        BurningAndDyingLastDeath,
        BurningAndDyingNonLastDeath,
        UnderConstruction,
        UnderConstructionStalled,
        ShipRemains,
        WarpingIn,
        Phase_Solo_Still,
        Phase_Solo_VertexAnim,
        Phase_Multi_Still,
        Phase_Multi_VertexAnim,
        Length
    }
}
