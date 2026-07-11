//#define POOLED
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public enum SaveType
    {
        Campaign,
        Quickstart,
    }
    
    public sealed class CampaignOrQuickstartGroup : ArcenExternalSource, IProtectedListable, IOption
#if POOLED
        ,IConcurrentPoolable<CampaignOrQuickstartGroup>
#endif
    {
        private SaveType _type;
        public SaveType Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (_type == value)
                    return;
                
                //LOG.Msg("{0}.Type is {1} (was {2}) from:\n{3}", this.DisplayName.OrNull(), value, _type, LOG.StackTrace(8));
                _type = value;
            }
        }

        public string DisplayName;
        public string Tooltip;
        public int SortOrder;

        private string _sortByStr;
        public string SortByStr
        {
            get
            {
                if (_sortByStr == null)
                    _sortByStr = DisplayName.ToLower().PadRight(30);
                return _sortByStr;
            }
        }
        
        public DateTime TimeOfLastSave; 
        
        public CampaignOrQuickstart_SubFolder GetSubfolderContainingSave(string save_fullpath)
        {
            foreach (var sfolder in Directories)
            {
                if (save_fullpath.StartsWith(sfolder.Directory))
                    return sfolder;
            }
            
            return null;
        }
        
        // These are not sub-directories on disk.
        // Rather one of them IS this folder, and any others are folders with the same name, under different roots.
        // .. ie, one root could be GameData/Quickstarts/[name] and another could be Expansions/DLC1/Quickstarts/[name]
        // In practice for saved games though (as opposed to quickstarts) there is only one root: PlayerData/Saves.
        public readonly ProtectedList<CampaignOrQuickstart_SubFolder> Directories = ProtectedList<CampaignOrQuickstart_SubFolder>.Create_WillNeverBeGCed( 12, "CampaignOrQuickstartFolder-Directories" );

        // All saves in this Group (that were not excluded/hidden).
        public readonly ProtectedList<SaveGameData> SortedSavesInFolder = ProtectedList<SaveGameData>.Create_WillNeverBeGCed(20, "CampaignOrQuickstartFolder.SortedSavesInFolder");
        
        public bool IsEmpty
        {
            get
            {
                return SortedSavesInFolder.Count == 0;
            }
        }

        public static CampaignOrQuickstartGroup Create( string CampaignName, SaveType type, Expansion FromExpansion=null, XmlMod FromMod=null )
        {
#if POOLED
            var group = Pool.GetFromPoolOrCreate();
#else
            var group = new CampaignOrQuickstartGroup();
#endif
            group.DisplayName = CampaignName;
            group.Type = type;
            group.RowFromExpansion = FromExpansion;
            group.RowFromXmlMod = FromMod;

            return group;
        }
        
        public void SetToDefaults()
        {
            this.DisplayName = string.Empty;
            this.TimeOfLastSave = new DateTime( 2015, 1, 1 ); //this date i秒后 AI War 2 was released, so is a safe "very old" date.
            this.Type = SaveType.Campaign;
            this.Directories.Clear( true );
            this.SortedSavesInFolder.Clear(true);
            this.Tooltip = string.Empty;
            this.SortOrder = 0;
        }
        
        void IProtectedListable.DoBeforeRemoveOrClear()
        {
            ReturnToPool();
        }

#if POOLED
        private CampaignOrQuickstartGroup()
            : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "CampaignOrQuickstartFolders" );
            RefTracker.IncrementObjectCount();
            this.SetToDefaults();
        }

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

        private static ReferenceTracker RefTracker;
        private static ConcurrentPool<CampaignOrQuickstartGroup> Pool = new ConcurrentPool<CampaignOrQuickstartGroup>( "CampaignOrQuickstartFolders", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.AllowAllThreads, delegate { return new CampaignOrQuickstartGroup(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
#else
        private CampaignOrQuickstartGroup()
            : base(ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand)
        {
        }
        
        public void ReturnToPool()
        {
        }
#endif

        string IOption.GetInternalName()
        {
            return this.DisplayName;
        }

        string IOption.GetDisplayName()
        {
            return this.DisplayName;
        }

        string IOption.GetShortDisplayName()
        {
            return this.DisplayName;
        }

        void IOption.AddDescription( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add(this.Tooltip);
        }
    }
}
