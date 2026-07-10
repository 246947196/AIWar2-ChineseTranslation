using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithSidekickSpecificCodeBaseInfo : ExternalFactionBaseInfoRoot
    {
        public override void WriteFactionSlotStatus(ArcenCharacterBufferBase buffer)
        {
            if ( World_AIW2.Instance == null )
                return;

            //in the lobby
            if ( World_AIW2.Instance.InSetupPhase ) 
            {
                buffer.Add("黑暗崛起");
                return;
            }

            // in game
            WriteControllingAccount( buffer );
        }
    }
}
