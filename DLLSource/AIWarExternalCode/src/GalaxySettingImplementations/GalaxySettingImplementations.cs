using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class BaseGalaxySettingImplementation : IAIWar2GalaxySettingImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseGalaxySettingImplementations" );
        public BaseGalaxySettingImplementation()
        {
            RefTracker.IncrementObjectCount();
        }
        
        public virtual bool ShouldShow
        {
            get
            {
                return true;
            }
        }
        
        public virtual void DoGameStartLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue ) {}

        public virtual void DoAfterSavegameLoadLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue ) { }
    }
}
