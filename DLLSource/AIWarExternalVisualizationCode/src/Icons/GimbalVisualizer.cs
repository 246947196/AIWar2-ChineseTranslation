using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;

using Arcen.Universal.Sprites;
using Arcen.Universal.Sprites.Instanced;
using MeshSprites = Arcen.Universal.Sprites.MeshBased;
using Object = UnityEngine.Object;

namespace Arcen.AIW2.ExternalVisualization
{
    [Serializable]
    public class GimbalVisualizer : IGimbal
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "GimbalVisualizers" );
        public GimbalVisualizer()
        {
            RefTracker.IncrementObjectCount();
        }

        public ArcenGimbal UnityObject;
        
        [NonSerialized]
        public ArcenGimbalSprite Icon;

        [NonSerialized]
        public bool IsDirty = true;
        private bool HasEverSetName = false;

        public SquadVisualizer RelatedSquad;

        [NonSerialized]
        public Transform selfT;

        #region DoLookAtAndScaleCheck
        private float lastScale = -1;
        public void DoLookAtAndScaleCheck( SquadVisualizer Squad, BattlefieldVisualSingleton MainVis, float BaseShipIconScale )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            GameEntityTypeData typeData = ( RelatedSquad == null ? null : ( RelatedSquad.RelatedEntity.GetSquad()?.TypeData ?? RelatedSquad.RelatedTypeData ) );

            Vector3 globalPos = Squad.CurrentPosition.ToUnityVector3();
            float distance = Mat.MagnitudeFastestMainThread( MainVis.MainCameraPos - globalPos );
            if ( distance > ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance )
            {
                float sizeScale = 1f + ( ( distance - ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance ) * ExternalVisualConstants.Instance.gimbal_scaling_up_multiplier );
                if ( typeData != null && typeData.GimbalIconSizeMultiplier > 0 )
                    sizeScale *= typeData.GimbalIconSizeMultiplier;
                if ( Mat.Abs( this.lastScale - sizeScale ) < 0.001f )
                    return;
                //if ( sizeScale > 200 )
                //    Debug.Log( "scaleA: " + sizeScale + " distance: " + distance + " globalPos: " + globalPos + " Squad.CurrentPosition: " + Squad.CurrentPosition );
                if ( sizeScale > 200 )
                    sizeScale = 200;
                this.lastScale = sizeScale;

                sizeScale *= BaseShipIconScale;
                this.selfT.localScale = new Vector3( sizeScale, sizeScale, sizeScale );
            }
            else
            {
                float percentageIn = distance / ExternalVisualConstants.Instance.gimbal_starts_scaling_up_at_distance;
                float sizeScale = percentageIn * ExternalVisualConstants.Instance.gimbal_scaling_down_multiplier;
                if ( sizeScale < ExternalVisualConstants.Instance.gimbal_scaling_absolute_min )
                    sizeScale = ExternalVisualConstants.Instance.gimbal_scaling_absolute_min;
                if ( typeData != null && typeData.GimbalIconSizeMultiplier > 0 )
                    sizeScale *= typeData.GimbalIconSizeMultiplier;
                if ( Mat.Abs( this.lastScale - sizeScale ) < 0.001f )
                    return;
                if ( sizeScale > 200 )
                    sizeScale = 200;
                this.lastScale = sizeScale;

                sizeScale *= BaseShipIconScale;
                this.selfT.localScale = new Vector3( sizeScale, sizeScale, sizeScale );
            }
        }
        #endregion
        
        private Renderer[] rends;
        
        public void InitializeGimbal()
        {
            ArcenDebugging.ErrorIfNotMainThread();

            this.selfT = this.UnityObject.transform;
            {
                GameObject obj = Object.Instantiate( ExternalVisualConstants.Instance.GimbalSpriteObjectBase, this.UnityObject.transform, false );
                obj.transform.SetDefaultTRS();
                this.Icon = obj.GetComponent<ArcenGimbalSprite>();
                if ( this.Icon == null )
                    ArcenDebugging.ArcenDebugLog( "No ArcenGimbalSprite found on object '" + obj.name + "'", Verbosity.ShowAsError );
            }

            rends = this.UnityObject.GetComponentsInChildren<Renderer>();
            this.SetSelfVisible( false );
        }

        public bool ThinksSelfIsVisible = true;
        public void SetSelfVisible( bool ShouldBeVisible )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( ShouldBeVisible == this.ThinksSelfIsVisible )
                return;
            this.ThinksSelfIsVisible = ShouldBeVisible;
            this.lastScale = -1; //prevent them from not scaling properly when becoming visible again

            for ( int i = 0; i < this.rends.Length; i++ )
                rends[i].enabled = ShouldBeVisible;
        }

        public void UpdateValuesIfDirty( SquadVisualizer Squad )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( Squad == null || Squad.RelatedTypeData == null || Squad.RelatedEntity.GetSquad() == null )
                return;

            if ( this.UnityObject == null )
            {
                ArcenDebugging.ArcenDebugLog( "UnityObject null on GimbalVisualizer", Verbosity.ShowAsError );
                return;
            }

            this.SetSelfVisible( !Engine_AIW2.Instance.InHideGimbalMode );
            this.RelatedSquad = Squad;

            if ( !this.UnityObject.BoxColl )
            {
                this.UnityObject.gameObject.layer = PlanetViewSelectionManager.Instance.selectedLayerInt;
                this.UnityObject.BoxColl = this.UnityObject.gameObject.AddComponent<BoxCollider>();
                this.UnityObject.BoxColl.size = new Vector3( 1, 1, 0.2f );
                this.UnityObject.BoxColl.isTrigger = true;
            }

            this.IsDirty = false;

            this.Icon.StartChanges();

            this.Icon.SetFrom( Squad.RelatedEntity.GetSquad() );

            this.Icon.EndChanges( true );

            #region Name Text
            if ( !this.HasEverSetName )
            {
                this.HasEverSetName = true;
                this.UnityObject.name = Squad.RelatedTypeData.InternalName + " Gimbal";
            }
            #endregion
        }
        

        //Vector4 is used instead of Color because Color clamps to a non-HDR range
        private static Vector4 basicBlack_HDR = new Vector4( 0, 0, 0, 1 );
        private const float YEL_HDR_AMT = 14;
        private static Color basicYellow = ColorMath.HexToColor( "ffed24" );
        private static Vector4 basicYellow_HDR = new Vector4( basicYellow.r * YEL_HDR_AMT, basicYellow.g * YEL_HDR_AMT, basicYellow.b * YEL_HDR_AMT, 1 );
        private const float RDOR_HDR_AMT = 18;
        private static Color reddishOrange = ColorMath.HexToColor( "ff7624" );
        private static Vector4 reddishOrange_HDR = new Vector4( reddishOrange.r * RDOR_HDR_AMT, reddishOrange.g * RDOR_HDR_AMT, reddishOrange.b * RDOR_HDR_AMT, 1 );
        private const float DRED_HDR_AMT = 10;
        private static Color deeperRed = ColorMath.HexToColor( "9f2d00" );
        private static Vector4 deeperRed_HDR = new Vector4( deeperRed.r * DRED_HDR_AMT, deeperRed.g * DRED_HDR_AMT, deeperRed.b * DRED_HDR_AMT, 0 );
        private const float BASIC_YELLOW_CUTOFF = 0.4f;
        private const float REDDISH_ORANGE_CUTOFF = 0.7f;
        private const float REDDISH_ORANGE_CUTOFF_DIFF = REDDISH_ORANGE_CUTOFF - BASIC_YELLOW_CUTOFF;
        private const float DEEPER_RED_CUTOFF = 1f;
        private const float DEEPER_RED_CUTOFF_DIFF = DEEPER_RED_CUTOFF - REDDISH_ORANGE_CUTOFF;

        private int lastLayerSet = -1;
        private bool lastNeedsToExplode = false;
        private float lastDeathProgressPercentage = 0f;
        public void UpdateBurningAndDyingStatus( bool NeedsToExplode, float DeathProgressPercentage )
        {           
            ArcenDebugging.ErrorIfNotMainThread();


            //already in no-explosion mode, so ignore
            //if ( lastNeedsToExplode == NeedsToExplode && !NeedsToExplode )
            //    return;

            //switch the layer of this, if need be
            int desiredLayer = 0;
            if ( NeedsToExplode )
                desiredLayer = 17; //selectable ship (and thus has bloom on it)
            else
                desiredLayer = 23; //sprites (back to no bloom)

            if ( lastLayerSet != desiredLayer )
            {
                this.Icon.gameObject.layer = desiredLayer;
                lastLayerSet = desiredLayer;
            }

            //switching explosion mode
            if ( lastNeedsToExplode != NeedsToExplode )
            {
                lastNeedsToExplode = NeedsToExplode;

                //switched to no-explosion mode, so go back to white
                if ( !NeedsToExplode )
                {
                    lastDeathProgressPercentage = 0;
                    this.Icon.StartChanges();
                    this.Icon.SetAddedDiffuseAndMultipliedA( basicBlack_HDR );
                    this.Icon.EndChanges( true );
                    return;
                }
            }

            //apparently we are exploding, so do that
            //----------------

            //oops, it's the same percentage as we set last time, so don't bother changing it this frame after all
            if ( lastDeathProgressPercentage == DeathProgressPercentage )
                return;
            
            //okay, actually set the colors and such.
            Vector4 colorToSet = basicBlack_HDR;
            if ( DeathProgressPercentage <= 0 )
            { } //do nothing
            else if ( DeathProgressPercentage < BASIC_YELLOW_CUTOFF )
                colorToSet = Vector4.Lerp( colorToSet, basicYellow_HDR, DeathProgressPercentage / BASIC_YELLOW_CUTOFF );
            else if ( DeathProgressPercentage < REDDISH_ORANGE_CUTOFF )
                colorToSet = Vector4.Lerp( basicYellow_HDR, reddishOrange_HDR, ( DeathProgressPercentage - BASIC_YELLOW_CUTOFF ) / REDDISH_ORANGE_CUTOFF_DIFF );
            else if ( DeathProgressPercentage < DEEPER_RED_CUTOFF )
                colorToSet = Vector4.Lerp( reddishOrange_HDR, deeperRed_HDR, ( DeathProgressPercentage - REDDISH_ORANGE_CUTOFF ) / DEEPER_RED_CUTOFF_DIFF );
            else
                colorToSet = deeperRed_HDR;

            this.Icon.StartChanges();
            this.Icon.SetAddedDiffuseAndMultipliedA( colorToSet );
            this.Icon.EndChanges( true );
        }

        public void DoDeactivations()
        {
            ArcenDebugging.ErrorIfNotMainThread();

            this.IsDirty = true;
            this.HasEverSetName = false;
            this.SetSelfVisible( false );
        }

        #region IVisualObject
        public void Activate( ArcenGameObjectResourcePool fromPool, GameEntity_Base RelatedTo, GameEntityTypeData RelatedTypeData )
        {
            throw new NotImplementedException();
        }
        public void DetachVisObjectFromSim()
        {
            throw new NotImplementedException();
        }
        public void FlagForRemoval( bool ForceRemoval )
        {
            throw new NotImplementedException();
        }

        //public Vector3 GetPosition()
        //{
        //    throw new NotImplementedException();
        //}

        public VisualObjectType GetObjectType()
        {
            return VisualObjectType.SquadOfShips;
        }
        
        public GameEntity_Base GetEntityRelatedTo()
        {
            return this.RelatedSquad?.RelatedEntity.GetSquad();
        }

        public void WriteDebugDataTo( ArcenCharacterBuffer Buffer )
        {
            throw new NotImplementedException();
        }

        public void DoPrototypeOnlyErrorChecking()
        {
            throw new NotImplementedException();
        }

        public void DeactivateAndReturnToPool()
        {
            throw new NotImplementedException();
        }

        public GameObject GetGameObject()
        {
            throw new NotImplementedException();
        }

        public bool GetIsToUseSharedPool()
        {
            return false;
        }
        #endregion

        public IInstancedRenderer GetSquad()
        {
            return this.RelatedSquad;
        }

        public int ReactToShotHittingSquad( GameEntity_Squad TargetSquad, GameEntity_Squad ProtectingShieldThatTookTheHitOrNull, int NumberOfShipsKilled, bool WasEntireSquadKilled )
        {
            throw new NotImplementedException( "ReactToShotHittingSquad on GimbalVisualizer!" );
        }
    }
}
