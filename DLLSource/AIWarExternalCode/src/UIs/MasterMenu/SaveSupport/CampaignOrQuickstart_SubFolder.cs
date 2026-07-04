//#define POOLED
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class CampaignOrQuickstart_SubFolder : ArcenExternalSource, IProtectedListable, IOption
#if POOLED
        , IConcurrentPoolable<CampaignOrQuickstart_SubFolder>
#endif
    {
        public string Directory = string.Empty;
        public bool IsCommunity = false;
        
        public static CampaignOrQuickstart_SubFolder Create( string Directory, Expansion FromExpansion=null, XmlMod FromMod=null, bool IsCommunity=false )
        {
#if POOLED
            var sfolder = Pool.GetFromPoolOrCreate();
#else
            var sfolder = new CampaignOrQuickstart_SubFolder();
#endif
            sfolder.RowFromExpansion = FromExpansion;
            sfolder.RowFromXmlMod = FromMod;
            sfolder.Directory = Directory;
            sfolder.IsCommunity = IsCommunity;
            
            return sfolder;
        }

        public void SetToDefaults()
        {
            this.Directory = string.Empty;
            this.RowFromExpansion = null;
            this.RowFromXmlMod = null;
            this.IsCommunity = false;
        }
#if POOLED
        public void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        public bool GetInPoolStatus()
        {
            return this.isInPool;
        }

        private bool isInPool = false;
        public void SetInPoolStatus( bool IsInPool )
        {
            this.isInPool = IsInPool;
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool();
        }
        
        private static ReferenceTracker RefTracker;
        private CampaignOrQuickstart_SubFolder() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "CampaignOrQuickstart_SubFolders" );
            RefTracker.IncrementObjectCount();
            this.SetToDefaults();
        }

        private static ConcurrentPool<CampaignOrQuickstart_SubFolder> Pool = new ConcurrentPool<CampaignOrQuickstart_SubFolder>( "CampaignOrQuickstart_SubFolders", 30000,
            KeepTrackOfPooledItems.No, PoolBehaviorDuringShutdown.AllowAllThreads, delegate { return new CampaignOrQuickstart_SubFolder(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
#else
        private CampaignOrQuickstart_SubFolder()
            : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        { }
        
        void IProtectedListable.DoBeforeRemoveOrClear()
        {
        }
#endif

        string IOption.GetInternalName()
        {
            return this.Directory;
        }

        string IOption.GetDisplayName()
        {
            return this.Directory;
        }

        string IOption.GetShortDisplayName()
        {
            return this.Directory;
        }

        void IOption.AddDescription( ArcenCharacterBufferBase Buffer )
        {
        }
    }
}
