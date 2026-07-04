using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickSpecificCodeBaseInfo : ExternalFactionBaseInfoRoot
    {
        public void WriteFactionSlotStatus( Faction Fac, ArcenDoubleCharacterBuffer buffer )
        {
            if ( World_AIW2.Instance == null )
                return;

            //in the lobby
            if ( World_AIW2.Instance.InSetupPhase ) 
            {
                buffer.Add( "Sidekick Needs A Player Friend" );
                return;
            }

            WriteControllingAccount(buffer);
        }

        public override void WriteAddedHackingHeaderInfo( ArcenCharacterBufferBase buffer )
        {
            //Show Essence Resource
            buffer.Add( "    " );
            buffer.Add( this.PlayerType.Resource1TextColorAndIcon ).Add( AttachedFaction.StoredFactionResourceOne.ToString(), this.PlayerType.Resource1Color );
        }

        public override void WriteAddedScienceHeaderInfo( ArcenCharacterBufferBase buffer )
        {
            //Show Essence Resource
            buffer.Add( "    " );
            buffer.Add( this.PlayerType.Resource1TextColorAndIcon ).Add( AttachedFaction.StoredFactionResourceOne.ToString(), this.PlayerType.Resource1Color );
        }
    }
}
