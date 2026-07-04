using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;


namespace Arcen.AIW2.ExternalVisualization
{
    public class SpecialEffect : IArcenSpecialEffectVisualizer
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "SpecialEffects" );
        public SpecialEffect()
        {
            RefTracker.IncrementObjectCount();
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            this.DeactivateAndReturnToPool();
        }

        public ArcenSpecialEffect UnityObject;
        public ArcenGameObjectResourcePool Pool = null;        
        public float RemainingTimeToLive = 0f;
        public Transform selfT;
        private Vector3 baseScale;

        private bool didInit = false;
        public void Init( MonoBehaviour unityObj )
        {
            if ( !this.UnityObject || (ArcenSpecialEffect)unityObj )
                this.UnityObject = (ArcenSpecialEffect)unityObj;

            if ( this.didInit )
                return;
            this.didInit = true;
            this.selfT = this.UnityObject.transform;
            this.baseScale = this.selfT.localScale;
        }

        public bool IsConsideredActive = false;
        public void Activate( ArcenGameObjectResourcePool fromPool )
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( this.IsConsideredActive )
                return;
            this.IsConsideredActive = true;

            //do NOT activate this yet, or it will place it the wrong place!!
            //this.UnityObject.gameObject.SetActive( true );
            this.RemainingTimeToLive = 5f;
            BattlefieldVisualSingleton.Instance.ActiveSpecialEffects.Add( this );

            this.Pool = fromPool;
        }

        public void DeactivateAndReturnToPool()
        {
            ArcenDebugging.ErrorIfNotMainThread();

            if ( !this.IsConsideredActive )
                return;
            this.IsConsideredActive = false;

            this.UnityObject.gameObject.SetActive( false );
            this.Pool.PutBackInPoolRightAway( this.UnityObject );
            this.Pool = null;
        }

        public void SetSuperGlobal3DPosition( Vector3 newLoc, float timeToLive, int AOESize )
        {
            this.RemainingTimeToLive = timeToLive;
            this.selfT.position = newLoc;
            //ArcenDebugging.ArcenDebugLog( "position set: " + newLoc , Verbosity.DoNotShow );
            this.UnityObject.gameObject.SetActive( true );

            if ( this.UnityObject.BaseAOESize > 0 && AOESize > 0 )
            {
                float scale = (float)AOESize / (float)this.UnityObject.BaseAOESize;
                this.selfT.localScale = ( this.baseScale * scale );
            }
        }

        public bool GetIsEffectComplete( float deltaTime )
        {
            this.RemainingTimeToLive -= deltaTime;
            if ( this.RemainingTimeToLive <= 0 )
            {
                this.DeactivateAndReturnToPool();
                return true;
            }
            return false;
        }
    }
}
