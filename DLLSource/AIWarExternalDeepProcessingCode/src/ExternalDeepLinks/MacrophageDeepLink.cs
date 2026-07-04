using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MacrophageDeepLink : MacrophageDeepLinkRoot
    {
        public MacrophageDeepLink()
        {
            MacrophageDeepLinkRoot.Instance = this;
        }

        public override void SpawnNewHarvester( MacrophageFactionBaseInfoCore MacrophageBaseInfo, ArcenHostOnlySimContext Context,
            GameEntity_Squad telium, MacrophagePerTeliumBaseInfo tData, bool debug, byte markLevel = 1 )
        {
            if ( Context == null )
                return; //client
            if (MacrophageBaseInfo== null )
            {
                ArcenDebugging.ArcenDebugLog( "Null MacrophageBaseInfo passed in to MacrophageDeepLink.SpawnNewHarvester!", Verbosity.ShowAsError );
                return;
            }
            MacrophageBaseInfo.AttachedFaction.GetExternalDeepInfoAs<MacrophageFactionDeepInfo>().SpawnNewHarvester( Context, telium, debug, markLevel );
        }
    }
}
