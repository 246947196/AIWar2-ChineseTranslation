using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerSidekickFactionBaseInfo : NecromancerEmpireFactionBaseInfo
    {
        public override void WriteFactionIcon( ArcenCharacterBufferBase buffer )
        {
            var fac = AttachedFaction;
            
            buffer.Add( "<pos=3><size=8px><voffset=6px>" );
            buffer.AddSprite( "PlayerIcon_Necromancer", 
                              "PlayerIcon_Necromancer_Border", 
                              null,
                              fac.FactionCenterColor.ColorHex,
                              fac.FactionTrimColor.ColorHex,
                              null );
            buffer.Add( "</pos></size></voffset>" );
        }

        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            if ( World_AIW2.Instance == null )
                return;

            //in the lobby
            if ( World_AIW2.Instance.InSetupPhase ) 
            {
                buffer.Add( "Needs A Friend" );
            }
           
            //in game
            WriteControllingAccount(buffer);
        }
    }
}
