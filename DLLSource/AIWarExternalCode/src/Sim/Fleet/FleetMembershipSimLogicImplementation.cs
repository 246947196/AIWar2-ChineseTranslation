using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;
using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// These are just methods that were in FleetMembership that have been moved into this dll.
    /// The main reason is to make it so that this can access things like pathfinders, but it also helps make more of the game open source.
    /// </summary>
    public class FleetMembershipSimLogicImplementation : FleetMembershipSimLogic
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            
        }

        public FleetMembershipSimLogicImplementation()
        {
            FleetMembershipSimLogic.Instance = this;
        }
    }
}
