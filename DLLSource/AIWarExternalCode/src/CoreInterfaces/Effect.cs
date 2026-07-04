
using System;
using System.Xml;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    #region Effect
    public enum EffectStatus
    {
        EaseIn,
        Ongoing,
        EaseOut
    }
    
    public abstract class Effect
        : 
        ExternalGameData_BaseForMultiples, 
        ITimeBasedPoolable<Effect>, 
        IExternalBaseInfo_Multiples
    {
        public abstract EffectStatus Status { get; set; }
        public abstract EffectType Type { get; set; }

        public override string GetIdentifierForErrorMessages()
        {
            return this.Type.InternalName;
        }
    }
    #endregion

    #region EffectType
    public class EffectType : ArcenDynamicTableRow, IConcurrentPoolable<EffectType>, IProtectedListable
    {
        internal string DllName = string.Empty;
        internal string TypeName = string.Empty;

        #region Junk
        
        [NotForDumping]
        private ArcenCachedExternalType _cachedTypeForImplementation;
        
        [NotForDumping]
        internal ArcenCachedExternalType CachedTypeForImplementation
        {
            get { return _cachedTypeForImplementation; }
            set
            {
                _cachedTypeForImplementation = value;
                
                var prototype = _cachedTypeForImplementation.AlwaysCreateNewInstanceOfType_AnyThread_Inner<Effect>("Effect");
                ResetPooledItem = new ArcenTypeAnalyzer(prototype);
            }
        }

        [NotForDumping]
        private TimeBasedPool<Effect> Pool;
        [NotForDumping]
        private ArcenTypeAnalyzer ResetPooledItem;

        private static ReferenceTracker RefTracker;

        private EffectType()
            : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "EffectType" );
            RefTracker.IncrementObjectCount();
            
            Pool = TimeBasedPool<Effect>.Create_WillNeverBeGCed( 
            "EffectType.Pool", 2, 10, 4096, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, 
            ()=> 
            {
                var obj = CachedTypeForImplementation.AlwaysCreateNewInstanceOfType_AnyThread_Inner<Effect>("Effect");
                obj.Type = this;
                return obj;
            });
        }

        public Effect Alloc()
        {
            var obj = Pool.GetFromPoolOrCreate();
            obj.Type = this;
            //LOG.Msg("{0}.Alloc returning {1} with .Type {2}", this.InternalName, obj.GetType().Name, obj.Type.InternalName);
            return obj;
        }

        public void Free(Effect obj)
        {
            ResetPooledItem.ApplyDefaults(obj);
            Pool.ReturnToPool(obj);
        }
        
        #endregion

        #region Pooling
        
        private static ConcurrentPool<EffectType> TypePool = new ConcurrentPool<EffectType>( "EffectType.TypePool", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new EffectType(); } );

        public static EffectType GetFromPoolOrCreate()
        {
            return TypePool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            TypePool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<EffectType> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<EffectType>( new EffectType() );
            typeAnalyzer.ApplyDefaults( this );
        }

        #endregion
    }
    #endregion

    #region EffectTypeTable
    public class EffectTypeTable : ArcenDynamicTable<EffectType>
    {
        public static readonly EffectTypeTable Instance = new EffectTypeTable();

        public EffectTypeTable() : base( "Effects", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Index, ReloadDuringRuntime.Allow ) { }

        protected override void PrepForCompleteReloadLater()
        {
        }

        public override EffectType GetNewRowFromPool()
        {
            return EffectType.GetFromPoolOrCreate();
        }

        public override void ReloadSelectData()
        {
            base.ReloadSelectData();
        }
        
        public override DelReturn NodeProcessor( ArcenXMLElement e, EffectType obj )
        {
            try
            {
                e.AllChildrenWereRequested = true;
                e.SuppressUnreqeuestedCheckOnThisNode = true;
                
                e.Fill( "dll_name", ref obj.DllName, !e.ReadingPartialRecord );
                e.Fill( "type_name", ref obj.TypeName, !e.ReadingPartialRecord );

                obj.CachedTypeForImplementation = ArcenExternalTypeManager.LoadCachedTypeFromExternalAssembly( obj.DllName, string.Empty, obj.TypeName, e.CachedDoc );
            }
            catch (Exception ex)
            {
                LOG.Err("Exception in EffectTypeTable.NodeProcessor\n{0}", ex);
            }
            
            return DelReturn.Continue;
        }

        public override DelReturn NodeSelectReProcessor( ArcenXMLElement e, EffectType obj )
        {
            return NodeProcessor(e, obj);
        }

        public override void DoPostInitializationPreSortingLogic_BackgroundThreads()
        {
        }

        public void Update()
        {
            //LOG.Line();
        }
    }
    #endregion

    #region Effect_Hooks
    public class Effect_Hooks : IArcenExternalCodeHookHandler
    {
        public void HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects, ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            if (Hook.InternalName == "LoadExternalData")
            {
                EffectTypeTable.Instance.Initialize();
            }
            
            if (Hook.InternalName == "ReloadSelectData")
            {
                EffectTypeTable.Instance.ReloadSelectData();
            }
            
            if (Hook.InternalName == "MainGameVisualsInitialized")
            {
                ArcenMainGameVisuals.Instance.BattlefieldHandler.RegisterForUpdates(EffectTypeTable.Instance.Update);
            }
        }
    }
    #endregion
}
