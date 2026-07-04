using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class MacrophageDeepLinkRoot : IExternalDeepLink
    {
        public static MacrophageDeepLinkRoot Instance;

        public abstract void SpawnNewHarvester( MacrophageFactionBaseInfoCore MacrophageBaseInfo, ArcenHostOnlySimContext Context, 
            GameEntity_Squad telium, MacrophagePerTeliumBaseInfo tData, bool debug, byte markLevel = 1 );
    }
}
